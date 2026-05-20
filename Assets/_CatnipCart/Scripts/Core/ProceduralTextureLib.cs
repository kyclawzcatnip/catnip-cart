using UnityEngine;

namespace CatnipCart.Core
{
    /// <summary>
    /// Generates high-quality procedural Texture2D assets at runtime for all game surfaces.
    /// Uses layered noise, domain warping, and cellular patterns for rich, convincing textures.
    /// Every method returns a ready-to-use texture with Apply() already called.
    /// </summary>
    public static class ProceduralTextureLib
    {
        // ------------------------------------------------------------------ 
        //  NOISE HELPERS
        // ------------------------------------------------------------------ 

        /// <summary>Simple hash for deterministic noise.</summary>
        static float Hash(float x, float y)
        {
            float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        /// <summary>Second independent hash for Voronoi cell jitter.</summary>
        static float Hash2(float x, float y)
        {
            float h = Mathf.Sin(x * 269.5f + y * 183.3f) * 28786.2351f;
            return h - Mathf.Floor(h);
        }

        /// <summary>Smooth interpolated value noise with quintic interpolation.</summary>
        static float SmoothNoise(float x, float y)
        {
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            float fx = x - ix;
            float fy = y - iy;
            // Quintic smoothstep for C2 continuity
            fx = fx * fx * fx * (fx * (fx * 6f - 15f) + 10f);
            fy = fy * fy * fy * (fy * (fy * 6f - 15f) + 10f);

            float a = Hash(ix, iy);
            float b = Hash(ix + 1, iy);
            float c = Hash(ix, iy + 1);
            float d = Hash(ix + 1, iy + 1);

            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>Fractal Brownian Motion with configurable octaves.</summary>
        static float FBM(float x, float y, int octaves = 4)
        {
            float value = 0f, amp = 0.5f, freq = 1f;
            for (int i = 0; i < octaves; i++)
            {
                value += SmoothNoise(x * freq, y * freq) * amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            return value;
        }

        /// <summary>Ridge noise — inverted abs of noise creates sharp ridge lines.</summary>
        static float RidgeNoise(float x, float y, int octaves = 4)
        {
            float value = 0f, amp = 0.5f, freq = 1f;
            for (int i = 0; i < octaves; i++)
            {
                float n = SmoothNoise(x * freq, y * freq);
                n = 1f - Mathf.Abs(n * 2f - 1f);
                n = n * n;
                value += n * amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            return value;
        }

        /// <summary>Domain-warped FBM for organic, flowing distortion.</summary>
        static float WarpedFBM(float x, float y, float warpStrength = 2f, int octaves = 4)
        {
            float wx = FBM(x + 5.2f, y + 1.3f, octaves) * warpStrength;
            float wy = FBM(x + 1.7f, y + 9.2f, octaves) * warpStrength;
            return FBM(x + wx, y + wy, octaves);
        }

        /// <summary>Voronoi / cellular noise — returns distance to nearest cell center.</summary>
        static float Voronoi(float x, float y, out float cellID)
        {
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            float minDist = 10f;
            cellID = 0f;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = ix + dx, cy = iy + dy;
                    float px = cx + Hash(cx, cy);
                    float py = cy + Hash2(cx, cy);
                    float d = (x - px) * (x - px) + (y - py) * (y - py);
                    if (d < minDist)
                    {
                        minDist = d;
                        cellID = Hash(cx * 13.7f, cy * 17.3f);
                    }
                }
            }
            return Mathf.Sqrt(minDist);
        }

        // ------------------------------------------------------------------ 
        //  MATERIAL HELPER
        // ------------------------------------------------------------------ 

        public static Material MakeLitMaterial(Texture2D tex, float smoothness = 0.3f,
            float metallic = 0f, Color? emission = null, Vector2? tiling = null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetTexture("_BaseMap", tex);
            mat.color = Color.white; // tint white so texture shows true color
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);

            if (tiling.HasValue)
            {
                mat.SetTextureScale("_BaseMap", tiling.Value);
            }

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                // Also set emission map to the base texture for glow
                mat.SetTexture("_EmissionMap", tex);
            }

            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            return mat;
        }

        // ------------------------------------------------------------------ 
        //  ASPHALT  (road surface)
        // ------------------------------------------------------------------ 

        public static Texture2D Asphalt(int size = 512)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color darkBase = new Color(0.18f, 0.18f, 0.21f);
            Color lightBase = new Color(0.28f, 0.27f, 0.30f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Multi-scale surface variation via domain warping
                    float surface = WarpedFBM(u * 20f, v * 20f, 1.5f, 5);
                    Color c = Color.Lerp(darkBase, lightBase, surface);

                    // Coarse aggregate — scattered light specks
                    float aggregate = Hash(x * 3.7f, y * 3.7f);
                    if (aggregate > 0.88f)
                    {
                        float brightness = (aggregate - 0.88f) * 5f;
                        float grainTone = Hash(x * 7.1f, y * 7.1f);
                        Color grain = Color.Lerp(
                            new Color(0.35f, 0.33f, 0.30f),
                            new Color(0.45f, 0.42f, 0.38f),
                            grainTone);
                        c = Color.Lerp(c, grain, brightness * 0.6f);
                    }

                    // Fine noise texture
                    float fine = Hash(x * 11.3f, y * 11.3f) * 0.06f - 0.03f;
                    c.r += fine; c.g += fine; c.b += fine;

                    // Tar patch splotches (dark, smooth areas)
                    float tarNoise = WarpedFBM(u * 6f + 50f, v * 6f + 50f, 3f, 3);
                    if (tarNoise > 0.62f)
                    {
                        float tarAmt = Mathf.Clamp01((tarNoise - 0.62f) * 4f);
                        c = Color.Lerp(c, new Color(0.12f, 0.12f, 0.14f), tarAmt * 0.5f);
                    }

                    // Micro-crack network using ridge noise
                    float cracks = RidgeNoise(u * 30f + 100f, v * 30f + 100f, 3);
                    if (cracks > 0.72f)
                    {
                        float crackAmt = (cracks - 0.72f) * 3f;
                        c = Color.Lerp(c, new Color(0.08f, 0.08f, 0.10f), Mathf.Clamp01(crackAmt) * 0.4f);
                    }

                    // Oil stain spots (subtle dark patches with iridescent tint)
                    float oilNoise = SmoothNoise(u * 4f + 200f, v * 4f + 200f);
                    if (oilNoise > 0.78f)
                    {
                        float oilAmt = (oilNoise - 0.78f) * 4f;
                        float hueShift = SmoothNoise(u * 12f, v * 12f);
                        Color oilColor = Color.Lerp(
                            new Color(0.15f, 0.10f, 0.18f),
                            new Color(0.10f, 0.15f, 0.12f),
                            hueShift);
                        c = Color.Lerp(c, oilColor, oilAmt * 0.25f);
                    }

                    // Center line dashes (white, slightly worn)
                    float centerDist = Mathf.Abs(u - 0.5f);
                    float lineWobble = SmoothNoise(v * 40f, 0f) * 0.005f;
                    if (centerDist + lineWobble < 0.018f)
                    {
                        float dashPhase = (v * 8f) % 1f;
                        if (dashPhase < 0.35f)
                        {
                            float wear = Hash(x * 0.5f, y * 0.5f) * 0.3f;
                            Color lineColor = new Color(
                                0.88f - wear * 0.2f,
                                0.88f - wear * 0.2f,
                                0.82f - wear * 0.15f);
                            float edgeSoft = 1f - Mathf.Clamp01((centerDist + lineWobble) / 0.018f);
                            c = Color.Lerp(c, lineColor, edgeSoft * (0.8f - wear * 0.3f));
                        }
                    }

                    // Edge shoulder lines (slightly wider, worn)
                    for (int side = 0; side < 2; side++)
                    {
                        float edgePos = side == 0 ? u : 1f - u;
                        if (edgePos < 0.035f)
                        {
                            float edgeFade = Mathf.Clamp01(edgePos / 0.035f);
                            float wearE = FBM(u * 20f, v * 20f, 2) * 0.4f;
                            c = Color.Lerp(c, new Color(0.82f, 0.82f, 0.76f),
                                (1f - edgeFade) * Mathf.Max(0f, 0.35f - wearE));
                        }
                    }

                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ 
        //  RACING STRIPES  (curbs)
        // ------------------------------------------------------------------ 

        public static Texture2D RacingStripes(int size = 256)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color red = new Color(0.82f, 0.12f, 0.10f);
            Color white = new Color(0.93f, 0.93f, 0.90f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Diagonal stripes
                    float stripe = ((v * 16f) + (u * 2f)) % 2f;

                    // Anti-aliased stripe transition at boundaries
                    float t1 = Mathf.Clamp01((stripe - 0.96f) / 0.08f);
                    float t2 = Mathf.Clamp01((stripe - 1.96f) / 0.08f);
                    Color c = Color.Lerp(red, white, t1);
                    c = Color.Lerp(c, red, t2);

                    // Paint surface texture
                    float paintNoise = SmoothNoise(u * 40f, v * 40f) * 0.06f - 0.03f;
                    c.r += paintNoise; c.g += paintNoise; c.b += paintNoise;

                    // Bevel effect — darken near stripe boundaries
                    float stripeEdge = Mathf.Abs(stripe % 1f - 0.5f) * 2f;
                    float bevel = 1f - Mathf.Pow(stripeEdge, 6f) * 0.25f;
                    c *= bevel;

                    // Rubber scuff marks (dark smudges from tires)
                    float scuff = WarpedFBM(u * 8f + 30f, v * 15f + 30f, 2f, 3);
                    if (scuff > 0.65f)
                    {
                        float scuffAmt = (scuff - 0.65f) * 2f;
                        c = Color.Lerp(c, new Color(0.2f, 0.18f, 0.18f),
                            Mathf.Clamp01(scuffAmt) * 0.2f);
                    }

                    // Worn/chipped paint near stripe edges
                    float wear = Hash(x * 5.3f, y * 5.3f);
                    if (wear > 0.96f && stripeEdge > 0.85f)
                    {
                        float chipAmt = (wear - 0.96f) * 10f;
                        c = Color.Lerp(c, new Color(0.5f, 0.5f, 0.48f), chipAmt * 0.3f);
                    }

                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ 
        //  GRASS
        // ------------------------------------------------------------------ 

        public static Texture2D Grass(int size = 512)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Multi-scale base color variation
                    float n1 = WarpedFBM(u * 10f, v * 10f, 1.5f, 4);
                    float n2 = FBM(u * 25f, v * 25f, 3);
                    float n3 = SmoothNoise(u * 50f, v * 50f);

                    // Rich green palette
                    float g = 0.38f + n1 * 0.22f + n2 * 0.08f + n3 * 0.04f;
                    float r = 0.10f + n1 * 0.10f + n2 * 0.03f;
                    float b = 0.04f + n1 * 0.05f;

                    // Grass blade streaks (vertical with slight wind angle)
                    float windAngle = SmoothNoise(u * 3f, v * 2f) * 0.3f;
                    float bladeU = u + windAngle * v;
                    float blade1 = SmoothNoise(bladeU * 5f, v * 60f);
                    float blade2 = SmoothNoise(bladeU * 8f + 3f, v * 45f + 5f);
                    float bladePattern = Mathf.Pow(blade1, 3f) * 0.12f
                                       + Mathf.Pow(blade2, 4f) * 0.08f;
                    g -= bladePattern;
                    r -= bladePattern * 0.3f;

                    // Lighter blade tips
                    float tips = SmoothNoise(bladeU * 6f + 7f, v * 80f);
                    if (tips > 0.75f)
                    {
                        float tipAmt = (tips - 0.75f) * 3f;
                        g += tipAmt * 0.08f;
                        r += tipAmt * 0.06f;
                    }

                    // Clover patches (darker, rounder spots via Voronoi)
                    float cellID;
                    float clover = Voronoi(u * 12f, v * 12f, out cellID);
                    if (cellID > 0.85f && clover < 0.15f)
                    {
                        float cloverAmt = 1f - clover / 0.15f;
                        g += cloverAmt * 0.06f;
                        r -= cloverAmt * 0.02f;
                        b += cloverAmt * 0.01f;
                    }

                    // Dirt/bare patches
                    float dirtNoise = WarpedFBM(u * 5f + 100f, v * 5f + 100f, 3f, 3);
                    if (dirtNoise > 0.72f)
                    {
                        float dirtAmt = Mathf.Clamp01((dirtNoise - 0.72f) * 5f);
                        r = Mathf.Lerp(r, 0.32f, dirtAmt * 0.4f);
                        g = Mathf.Lerp(g, 0.22f, dirtAmt * 0.4f);
                        b = Mathf.Lerp(b, 0.10f, dirtAmt * 0.4f);
                    }

                    // Wildflower dots (clustered in patches)
                    float flower = Hash(x * 7.1f, y * 7.1f);
                    float flowerArea = SmoothNoise(u * 6f + 50f, v * 6f + 50f);
                    if (flowerArea > 0.6f)
                    {
                        if (flower > 0.993f)
                        {
                            r = 0.95f; g = 0.88f; b = 0.15f; // Yellow dandelion
                        }
                        else if (flower > 0.988f)
                        {
                            r = 0.92f; g = 0.90f; b = 0.88f; // White daisy
                        }
                        else if (flower > 0.984f)
                        {
                            r = 0.65f; g = 0.30f; b = 0.70f; // Purple clover flower
                        }
                    }

                    // Subtle shadow variation
                    float shadow = FBM(u * 4f + 200f, v * 4f + 200f, 2);
                    float shadowMul = 0.9f + shadow * 0.2f;
                    r *= shadowMul; g *= shadowMul; b *= shadowMul;

                    tex.SetPixel(x, y, new Color(
                        Mathf.Clamp01(r),
                        Mathf.Clamp01(g),
                        Mathf.Clamp01(b), 1f));
                }
            }
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ 
        //  BARRIER  (hazard chevrons)
        // ------------------------------------------------------------------ 

        public static Texture2D Barrier(int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color red = new Color(0.82f, 0.10f, 0.08f);
            Color white = new Color(0.92f, 0.92f, 0.88f);
            Color metal = new Color(0.55f, 0.55f, 0.58f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Diagonal chevron pattern (45° stripes, not horizontal)
                    float diag = (u + v) * 4f;
                    float stripe = diag % 2f;

                    // Anti-aliased stripe transition
                    float t1 = Mathf.Clamp01((stripe - 0.92f) / 0.16f);
                    float t2 = Mathf.Clamp01((stripe - 1.92f) / 0.16f);
                    Color c = Color.Lerp(red, white, t1);
                    c = Color.Lerp(c, red, t2);

                    // Paint surface texture
                    float paintTex = SmoothNoise(u * 50f, v * 50f) * 0.05f - 0.025f;
                    c.r += paintTex; c.g += paintTex; c.b += paintTex;

                    // Metal base showing through scratches
                    float scratch = RidgeNoise(u * 25f + 50f, v * 25f + 50f, 3);
                    if (scratch > 0.78f)
                    {
                        float scratchAmt = (scratch - 0.78f) * 3f;
                        c = Color.Lerp(c, metal, Mathf.Clamp01(scratchAmt) * 0.35f);
                    }

                    // Reflective tape highlight band at top/bottom edges
                    float reflective = Mathf.Abs(v - 0.5f);
                    if (reflective > 0.42f)
                    {
                        float refAmt = (reflective - 0.42f) * 8f;
                        float shimmer = SmoothNoise(u * 20f, v * 5f) * 0.5f + 0.5f;
                        c = Color.Lerp(c, new Color(0.95f, 0.95f, 0.9f),
                            Mathf.Clamp01(refAmt) * shimmer * 0.3f);
                    }

                    // Fine metallic speckle
                    float spec = Hash(x * 2.1f, y * 2.1f) * 0.06f;
                    c = new Color(c.r + spec, c.g + spec, c.b + spec, 1f);

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ 
        //  YARN BALL
        // ------------------------------------------------------------------ 

        public static Texture2D Yarn(Color baseColor, int size = 256)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float br = baseColor.r, bg = baseColor.g, bb = baseColor.b;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Multiple wound fiber layers at different angles
                    float angle = Mathf.Atan2(v - 0.5f, u - 0.5f);
                    float dist = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f));

                    // Primary wound direction
                    float wound1 = Mathf.Sin(dist * 45f + angle * 3f);
                    // Secondary cross-wound
                    float wound2 = Mathf.Sin(dist * 30f - angle * 5f + 1.7f);
                    // Tertiary chaotic wound
                    float wound3 = Mathf.Sin(dist * 55f + angle * 7f + 3.2f);

                    // Strand-like pattern (sharp peaks = strand centers)
                    float strand1 = Mathf.Pow(Mathf.Abs(Mathf.Sin(u * 35f + v * 18f)), 0.3f);
                    float strand2 = Mathf.Pow(Mathf.Abs(Mathf.Sin(u * 18f - v * 35f + 1.5f)), 0.3f);
                    float strands = Mathf.Min(strand1, strand2);

                    // Combine wound patterns
                    float pattern = wound1 * 0.25f + wound2 * 0.20f + wound3 * 0.10f + strands * 0.25f;
                    float brightness = 0.65f + pattern * 0.35f;

                    // Fiber depth shadows (gaps between strands)
                    float gap = SmoothNoise(u * 20f + 10f, v * 20f + 10f);
                    if (gap > 0.7f)
                    {
                        brightness -= (gap - 0.7f) * 0.8f;
                    }

                    // Fuzzy fiber noise (fine-scale)
                    float fuzz = Hash(x * 5f, y * 5f) * 0.12f - 0.06f;

                    // Color depth variation (slight hue shift in shadows)
                    float shadow = Mathf.Clamp01(brightness);
                    float hueShift = (1f - shadow) * 0.08f;

                    // Loose thread wisps
                    float wisp = SmoothNoise(u * 3f + 20f, v * 60f + 20f);
                    float wispHighlight = wisp > 0.85f ? (wisp - 0.85f) * 5f * 0.1f : 0f;

                    Color c = new Color(
                        Mathf.Clamp01(br * brightness + fuzz + hueShift + wispHighlight),
                        Mathf.Clamp01(bg * brightness + fuzz - hueShift * 0.5f + wispHighlight),
                        Mathf.Clamp01(bb * brightness + fuzz + wispHighlight), 1f);

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ 
        //  TREE BARK
        // ------------------------------------------------------------------ 

        public static Texture2D TreeBark(int size = 256)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color darkBark = new Color(0.30f, 0.18f, 0.09f);
            Color lightBark = new Color(0.48f, 0.33f, 0.18f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Vertical grain (dominant bark direction)
                    float grain = SmoothNoise(u * 4f, v * 25f);
                    float grain2 = SmoothNoise(u * 6f + 5f, v * 35f + 3f);

                    // Base color from grain
                    Color c = Color.Lerp(darkBark, lightBark, grain * 0.6f + grain2 * 0.3f);

                    // Deep vertical fissures (ridge noise for sharp cracks)
                    float fissure = RidgeNoise(u * 6f + 10f, v * 30f + 10f, 3);
                    if (fissure > 0.65f)
                    {
                        float fissureAmt = (fissure - 0.65f) * 2.5f;
                        c = Color.Lerp(c, new Color(0.12f, 0.07f, 0.03f),
                            Mathf.Clamp01(fissureAmt) * 0.6f);
                    }

                    // Knot holes via Voronoi
                    float cellID;
                    float knotDist = Voronoi(u * 3f + 20f, v * 3f + 20f, out cellID);
                    if (cellID > 0.92f && knotDist < 0.15f)
                    {
                        float knotAmt = 1f - knotDist / 0.15f;
                        // Concentric rings around knot
                        float rings = Mathf.Sin(knotDist * 80f) * 0.5f + 0.5f;
                        c = Color.Lerp(c, new Color(0.22f, 0.13f, 0.06f), knotAmt * 0.5f);
                        c *= 0.85f + rings * 0.15f;
                    }

                    // Horizontal lenticel marks (small breathing pores)
                    float lenticel = Hash(Mathf.Floor(u * 15f), Mathf.Floor(v * 40f));
                    if (lenticel > 0.92f)
                    {
                        float lenWidth = Hash(Mathf.Floor(u * 15f) + 100f,
                            Mathf.Floor(v * 40f)) * 0.5f + 0.5f;
                        float lenU = (u * 15f) % 1f;
                        if (Mathf.Abs(lenU - 0.5f) < lenWidth * 0.3f)
                        {
                            c = Color.Lerp(c, new Color(0.42f, 0.30f, 0.18f), 0.3f);
                        }
                    }

                    // Lichen/moss patches
                    float moss = WarpedFBM(u * 8f + 50f, v * 8f + 50f, 2f, 3);
                    if (moss > 0.65f)
                    {
                        float mossAmt = Mathf.Clamp01((moss - 0.65f) * 3f);
                        float mossType = SmoothNoise(u * 12f + 70f, v * 12f + 70f);
                        Color mossCol = Color.Lerp(
                            new Color(0.22f, 0.38f, 0.12f),  // Green moss
                            new Color(0.55f, 0.55f, 0.42f),  // Gray lichen
                            mossType);
                        c = Color.Lerp(c, mossCol, mossAmt * 0.35f);
                    }

                    // Fine bark texture noise
                    float barkNoise = Hash(x * 3f, y * 3f) * 0.08f - 0.04f;
                    c.r += barkNoise;
                    c.g += barkNoise * 0.8f;
                    c.b += barkNoise * 0.6f;

                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ 
        //  TREE LEAVES
        // ------------------------------------------------------------------ 

        public static Texture2D TreeLeaves(int size = 256)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Multi-scale green variation
                    float n1 = WarpedFBM(u * 8f, v * 8f, 1.5f, 4);
                    float n2 = FBM(u * 15f, v * 15f, 3);

                    // Rich green base
                    float g = 0.35f + n1 * 0.25f + n2 * 0.08f;
                    float r = 0.06f + n1 * 0.12f;
                    float b = 0.02f + n1 * 0.04f;

                    // Leaf cluster shapes using Voronoi cells
                    float cellID;
                    float leafDist = Voronoi(u * 12f, v * 12f, out cellID);

                    // Each cell is a leaf cluster with its own shade
                    float clusterShade = cellID * 0.3f;
                    g += clusterShade * 0.12f;
                    r += clusterShade * 0.04f;

                    // Leaf vein-like edges (darker at cell borders)
                    if (leafDist > 0.12f && leafDist < 0.18f)
                    {
                        float veinAmt = 1f - Mathf.Abs(leafDist - 0.15f) / 0.03f;
                        g -= veinAmt * 0.06f;
                        r -= veinAmt * 0.02f;
                    }

                    // Dark gaps/shadows between leaf clusters
                    float gapNoise = SmoothNoise(u * 18f + 5f, v * 18f + 5f);
                    if (gapNoise > 0.72f)
                    {
                        float gapAmt = (gapNoise - 0.72f) * 3.5f;
                        r -= Mathf.Clamp01(gapAmt) * 0.05f;
                        g -= Mathf.Clamp01(gapAmt) * 0.15f;
                        b -= Mathf.Clamp01(gapAmt) * 0.03f;
                    }

                    // Light-dappled highlights (sun through canopy)
                    float sunDapple = FBM(u * 6f + 30f, v * 6f + 30f, 3);
                    if (sunDapple > 0.6f)
                    {
                        float sunAmt = (sunDapple - 0.6f) * 2f;
                        g += sunAmt * 0.12f;
                        r += sunAmt * 0.08f;
                        b += sunAmt * 0.02f;
                    }

                    // Autumn hints at cluster borders
                    if (leafDist > 0.08f)
                    {
                        float autumnChance = cellID;
                        if (autumnChance > 0.8f)
                        {
                            float autumnAmt = (leafDist - 0.08f) * 3f
                                * (autumnChance - 0.8f) * 5f;
                            autumnAmt = Mathf.Clamp01(autumnAmt);
                            r += autumnAmt * 0.25f;
                            g += autumnAmt * 0.05f;
                            b -= autumnAmt * 0.02f;
                        }
                    }

                    // Fine leaf texture
                    float leafTex = Hash(x * 4f, y * 4f) * 0.06f - 0.03f;
                    r += leafTex * 0.5f; g += leafTex; b += leafTex * 0.3f;

                    tex.SetPixel(x, y, new Color(
                        Mathf.Clamp01(r),
                        Mathf.Clamp01(g),
                        Mathf.Clamp01(b), 1f));
                }
            }
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ 
        //  KART PAINT  (metallic flake)
        // ------------------------------------------------------------------ 

        public static Texture2D KartPaint(Color baseColor, int size = 256)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Metallic flake sparkle (multi-scale)
                    float flake1 = Hash(x * 11.3f, y * 11.3f);
                    float flake2 = Hash(x * 23.7f, y * 23.7f);
                    float highlight = 0f;
                    if (flake1 > 0.88f) highlight += (flake1 - 0.88f) * 4f;
                    if (flake2 > 0.92f) highlight += (flake2 - 0.92f) * 6f;
                    highlight = Mathf.Clamp01(highlight);

                    // Clear-coat reflection gradient (lighter at top)
                    float reflectionGrad = v * 0.12f;

                    // Orange peel micro-texture
                    float orangePeel = SmoothNoise(u * 60f, v * 60f) * 0.03f;

                    // Large-scale color depth shift
                    float depth = WarpedFBM(u * 3f, v * 3f, 1f, 3) * 0.08f;

                    // Subtle warm/cool color shift
                    float colorShift = SmoothNoise(u * 8f, v * 8f);
                    float warmShift = colorShift * 0.03f;

                    Color c = new Color(
                        Mathf.Clamp01(baseColor.r + highlight * 0.25f + depth
                            + reflectionGrad + orangePeel + warmShift),
                        Mathf.Clamp01(baseColor.g + highlight * 0.25f + depth
                            + reflectionGrad * 0.8f + orangePeel),
                        Mathf.Clamp01(baseColor.b + highlight * 0.25f + depth
                            + reflectionGrad * 0.6f + orangePeel - warmShift),
                        1f);

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ 
        //  RUBBER  (tire)
        // ------------------------------------------------------------------ 

        public static Texture2D Rubber(int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color baseCol = new Color(0.10f, 0.10f, 0.11f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Base rubber noise
                    float n = SmoothNoise(u * 20f, v * 20f) * 0.06f;

                    // Cross-hatch tread block pattern
                    float blockU = (u * 8f) % 1f;
                    float blockV = (v * 12f) % 1f;

                    // Groove channels between blocks
                    float grooveH = Mathf.Abs(blockV - 0.5f) > 0.42f ? -0.08f : 0f;
                    float grooveV = Mathf.Abs(blockU - 0.5f) > 0.44f ? -0.06f : 0f;
                    float groove = Mathf.Min(grooveH, grooveV);

                    // Angled sipes within each block
                    float sipe = Mathf.Abs(Mathf.Sin((blockU + blockV) * 12f));
                    sipe = sipe < 0.08f ? -0.04f : 0f;

                    // Sidewall texture area (along edges)
                    float sidewall = 0f;
                    if (u < 0.12f || u > 0.88f)
                    {
                        sidewall = SmoothNoise(u * 5f, v * 30f) * 0.04f;
                        groove = 0f;
                        sipe = 0f;
                    }

                    // Rubber surface micro-texture
                    float micro = Hash(x * 7f, y * 7f) * 0.04f - 0.02f;

                    float brightness = 1f + n + groove + sipe + sidewall + micro;

                    // Slight brown undertone
                    Color c = new Color(
                        baseCol.r * brightness + 0.01f,
                        baseCol.g * brightness,
                        baseCol.b * brightness,
                        1f);

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ 
        //  ITEM BOX GOLD
        // ------------------------------------------------------------------ 

        public static Texture2D ItemBoxGold(int size = 256)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color warmGold = new Color(1f, 0.82f, 0.08f);
            Color coolGold = new Color(0.95f, 0.75f, 0.20f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Gold base with warm/cool variation
                    float goldVar = SmoothNoise(u * 6f, v * 6f);
                    Color gold = Color.Lerp(warmGold, coolGold, goldVar);

                    // Anisotropic brushed metal shimmer
                    float brushed = SmoothNoise(u * 3f, v * 40f) * 0.15f;
                    gold.r += brushed;
                    gold.g += brushed * 0.8f;
                    gold.b += brushed * 0.3f;

                    // Sparkle highlights
                    float sparkle = Hash(x * 9.1f, y * 9.1f);
                    if (sparkle > 0.82f)
                    {
                        float sparkAmt = (sparkle - 0.82f) * 4f;
                        gold = Color.Lerp(gold, new Color(1f, 0.98f, 0.85f), sparkAmt * 0.4f);
                    }

                    // Beveled edge darkening with highlight
                    float edgeDist = Mathf.Min(
                        Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    float bevel = Mathf.Clamp01(edgeDist * 12f);
                    float bevelHighlight = Mathf.Clamp01((edgeDist - 0.04f) * 15f)
                        * (1f - Mathf.Clamp01((edgeDist - 0.08f) * 15f));

                    // "?" question mark — SDF for smooth anti-aliased rendering
                    float cx = u - 0.5f, cy = v - 0.5f;

                    // Composite distance field for ?
                    float qDist = 10f;

                    // Top arc
                    float arcRadius = 0.18f;
                    float arcCenterY = 0.04f;
                    float arcDx = cx;
                    float arcDy = cy - arcCenterY;
                    float arcDist = Mathf.Abs(
                        Mathf.Sqrt(arcDx * arcDx + arcDy * arcDy) - arcRadius);
                    float arcAngle = Mathf.Atan2(arcDy, arcDx);
                    if (arcAngle > -0.3f && arcAngle < 3.2f)
                        qDist = Mathf.Min(qDist, arcDist);

                    // Descending stem
                    if (Mathf.Abs(cx) < 0.06f && cy > -0.18f && cy < -0.02f)
                    {
                        float stemDist = Mathf.Abs(cx);
                        qDist = Mathf.Min(qDist, stemDist);
                    }

                    // Dot
                    float dotCy = cy + 0.28f;
                    float dotDist = Mathf.Sqrt(cx * cx + dotCy * dotCy);
                    qDist = Mathf.Min(qDist, dotDist - 0.035f);

                    // Apply question mark with anti-aliased edge
                    float lineWidth = 0.035f;
                    float aaWidth = 0.015f;
                    Color c;
                    if (qDist < lineWidth)
                    {
                        float blend = Mathf.Clamp01((lineWidth - qDist) / aaWidth);
                        Color qColor = new Color(0.85f, 0.12f, 0.08f);
                        c = Color.Lerp(gold * bevel, qColor, blend);
                    }
                    else
                    {
                        c = gold * bevel;
                    }

                    // Bevel edge highlight
                    c = Color.Lerp(c, new Color(1f, 0.95f, 0.7f), bevelHighlight * 0.3f);

                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ 
        //  BOOST ARROW
        // ------------------------------------------------------------------ 

        public static Texture2D BoostArrow(int size = 256)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color cyan = new Color(0.05f, 0.75f, 0.95f);
            Color brightCyan = new Color(0.3f, 0.9f, 1f);
            Color dark = new Color(0.01f, 0.08f, 0.18f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Repeating chevron/arrow pattern
                    float chevronPhase = (v * 4f) % 1f;
                    float centerDist = Mathf.Abs(u - 0.5f);

                    // Arrow shape with smoother edges
                    float arrowWidth = 0.42f - chevronPhase * 0.35f;
                    float arrowInner = arrowWidth - 0.07f;

                    // Distance to arrow edge
                    float outerDist = Mathf.Abs(centerDist - arrowWidth);
                    bool isOuterEdge = outerDist < 0.025f
                        && centerDist < arrowWidth + 0.025f;
                    bool isInnerFill = centerDist < arrowInner;

                    // Energy glow along the center
                    float centerGlow = Mathf.Exp(-centerDist * centerDist * 30f);

                    Color c;
                    if (isOuterEdge)
                    {
                        // Glowing edge line
                        float edgeBright = 1f - outerDist / 0.025f;
                        c = Color.Lerp(cyan, Color.white, edgeBright * 0.6f);
                    }
                    else if (isInnerFill)
                    {
                        // Inner fill with energy pulse
                        float pulse = 0.6f + Mathf.Sin(chevronPhase * Mathf.PI) * 0.4f;
                        float energy = centerGlow * 0.4f + pulse * 0.6f;
                        c = Color.Lerp(dark, brightCyan, energy);

                        // Scanner sweep effect
                        float sweep = Mathf.Sin(v * 12f) * 0.5f + 0.5f;
                        c = Color.Lerp(c, brightCyan, sweep * 0.15f);
                    }
                    else
                    {
                        // Background with subtle grid
                        float gridU = Mathf.Abs(Mathf.Sin(u * 40f));
                        float gridV = Mathf.Abs(Mathf.Sin(v * 40f));
                        float grid = (gridU < 0.05f || gridV < 0.05f) ? 0.15f : 0f;
                        c = dark;
                        c.r += grid * 0.03f;
                        c.g += grid * 0.08f;
                        c.b += grid * 0.1f;
                    }

                    // Energy particle dots
                    float particle = Hash(x * 13f, y * 13f);
                    if (particle > 0.97f && isInnerFill)
                    {
                        float particleBright = (particle - 0.97f) * 20f;
                        c = Color.Lerp(c, Color.white, particleBright * 0.5f);
                    }

                    // Overall glow haze
                    float haze = Hash(x * 1.5f, y * 1.5f) * 0.02f;
                    c.r += haze * 0.2f; c.g += haze; c.b += haze;

                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ 
        //  SKYBOX  (gradient + clouds on a panoramic texture)
        // ------------------------------------------------------------------ 

        public static Texture2D SkyGradient(int width = 1024, int height = 512)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color zenith = new Color(0.18f, 0.35f, 0.82f);
            Color midSky = new Color(0.40f, 0.60f, 0.92f);
            Color horizon = new Color(0.72f, 0.82f, 0.95f);
            Color sunGlow = new Color(1f, 0.90f, 0.65f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)width;
                    float v = y / (float)height;

                    // Atmospheric scattering gradient (non-linear)
                    Color sky;
                    if (v > 0.5f)
                    {
                        float t = (v - 0.5f) * 2f;
                        t = t * t; // Quadratic for deeper blue at zenith
                        sky = Color.Lerp(midSky, zenith, t);
                    }
                    else
                    {
                        float t = v * 2f;
                        sky = Color.Lerp(horizon, midSky, t * t);
                    }

                    // Sun disc and bloom
                    float sunU = 0.25f, sunV = 0.15f;
                    float sunDx = u - sunU, sunDy = v - sunV;
                    float sunDist = Mathf.Sqrt(sunDx * sunDx + sunDy * sunDy);

                    // Sun disc
                    if (sunDist < 0.03f)
                    {
                        float discFade = Mathf.Clamp01(1f - sunDist / 0.03f);
                        sky = Color.Lerp(sky, new Color(1f, 0.98f, 0.90f), discFade);
                    }
                    // Sun bloom (wider glow)
                    float bloom = Mathf.Exp(-sunDist * sunDist * 20f);
                    sky = Color.Lerp(sky, sunGlow, bloom * 0.5f);
                    // Extended glow
                    float extGlow = Mathf.Clamp01(1f - sunDist * 2f);
                    sky = Color.Lerp(sky, sunGlow, extGlow * 0.2f);

                    // Atmospheric haze near horizon
                    if (v < 0.15f)
                    {
                        float hazeFade = 1f - v / 0.15f;
                        hazeFade = hazeFade * hazeFade;
                        sky = Color.Lerp(sky, new Color(0.80f, 0.85f, 0.92f),
                            hazeFade * 0.4f);
                    }

                    // Cumulus clouds (main layer)
                    if (v < 0.55f)
                    {
                        float cloudU = u * 8f + 0.5f;
                        float cloudV = v * 10f + 0.5f;
                        float cloudNoise = WarpedFBM(cloudU, cloudV, 2f, 5);
                        float cloudThreshold = 0.40f + v * 0.35f;

                        if (cloudNoise > cloudThreshold)
                        {
                            float cloudAmt = (cloudNoise - cloudThreshold) * 3.5f;
                            cloudAmt = Mathf.Clamp01(cloudAmt);

                            // Cloud shading (darker bottoms, brighter tops)
                            float cloudShade = 0.85f
                                + FBM(cloudU + 2f, cloudV - 1f, 3) * 0.2f;
                            float bottomDark = FBM(cloudU, cloudV + 0.5f, 2);
                            cloudShade -= bottomDark * 0.15f;

                            Color cloudCol = new Color(
                                cloudShade, cloudShade, cloudShade * 1.02f, 1f);

                            // Sun-lit edges
                            float edgeLight = FBM(cloudU + 3f, cloudV + 3f, 2);
                            if (edgeLight > 0.5f)
                            {
                                cloudCol = Color.Lerp(cloudCol,
                                    new Color(1f, 0.97f, 0.90f),
                                    (edgeLight - 0.5f) * 0.4f);
                            }

                            sky = Color.Lerp(sky, cloudCol, cloudAmt * 0.75f);
                        }
                    }

                    // Wispy cirrus clouds (high altitude)
                    if (v > 0.4f)
                    {
                        float cirrusU = u * 12f + 100f;
                        float cirrusV = v * 3f + 100f;
                        float cirrus = SmoothNoise(cirrusU, cirrusV);
                        float cirrus2 = SmoothNoise(
                            cirrusU * 2f + 5f, cirrusV * 0.5f + 5f);
                        float cirrusPattern = cirrus * 0.6f + cirrus2 * 0.4f;

                        if (cirrusPattern > 0.55f)
                        {
                            float cirrusAmt = (cirrusPattern - 0.55f) * 3f;
                            cirrusAmt = Mathf.Clamp01(cirrusAmt) * 0.25f;
                            sky = Color.Lerp(sky,
                                new Color(0.9f, 0.92f, 0.96f), cirrusAmt);
                        }
                    }

                    sky.a = 1f;
                    tex.SetPixel(x, y, sky);
                }
            }
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ 
        //  HAIRBALL  (matted fur trap texture)
        // ------------------------------------------------------------------ 

        public static Texture2D Hairball(int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color darkFur = new Color(0.22f, 0.15f, 0.12f);
            Color lightFur = new Color(0.42f, 0.30f, 0.24f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // Tangled hair strands (multiple chaotic directions)
                    float warp1 = SmoothNoise(u * 5f, v * 5f) * 3f;
                    float warp2 = SmoothNoise(u * 7f + 3f, v * 7f + 3f) * 2f;
                    float warp3 = SmoothNoise(u * 4f + 6f, v * 4f + 6f) * 4f;
                    float strand1 = Mathf.Abs(Mathf.Sin(u * 25f + v * 40f + warp1));
                    float strand2 = Mathf.Abs(Mathf.Sin(u * 40f - v * 20f + warp2));
                    float strand3 = Mathf.Abs(Mathf.Sin((u + v) * 30f + warp3));
                    float strandPattern = Mathf.Min(
                        Mathf.Min(strand1, strand2), strand3);

                    // Base color from strand pattern
                    float brightness = 0.5f + strandPattern * 0.5f;
                    Color c = Color.Lerp(darkFur, lightFur, brightness);

                    // Matted clumps (domain-warped noise)
                    float clump = WarpedFBM(u * 8f, v * 8f, 3f, 3);
                    c = Color.Lerp(c, darkFur, clump * 0.3f);

                    // Oily sheen spots
                    float sheen = SmoothNoise(u * 10f + 20f, v * 10f + 20f);
                    if (sheen > 0.7f)
                    {
                        float sheenAmt = (sheen - 0.7f) * 2.5f;
                        c = Color.Lerp(c, new Color(0.35f, 0.28f, 0.22f),
                            Mathf.Clamp01(sheenAmt) * 0.3f);
                    }

                    // Dust/debris particles
                    float debris = Hash(x * 8f, y * 8f);
                    if (debris > 0.95f)
                    {
                        float debrisAmt = (debris - 0.95f) * 10f;
                        c = Color.Lerp(c, new Color(0.5f, 0.45f, 0.38f),
                            debrisAmt * 0.4f);
                    }

                    // Fine fur texture noise
                    float furNoise = Hash(x * 6f, y * 6f) * 0.08f - 0.04f;
                    c.r += furNoise;
                    c.g += furNoise * 0.8f;
                    c.b += furNoise * 0.6f;

                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
