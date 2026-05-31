using UnityEngine;
using CatnipCart.Kart;
using CatnipCart.Track;
using CatnipCart.Items;
using CatnipCart.AI;
using CatnipCart.UI;

namespace CatnipCart.Core
{
    /// <summary>
    /// Master scene initializer. Creates the entire race scene procedurally:
    /// track, karts, cats, camera, UI, lighting, skybox.
    /// Attach to an empty GameObject in the scene and press Play.
    /// 
    /// Now data-driven: uses TrackData to configure all visual/layout parameters.
    /// Shows a track selection menu if no track is selected yet.
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        [Header("Race Config")]
        public int totalLaps = 3;

        /// <summary>
        /// Static index so it persists across scene reloads.
        /// -1 = show track selection menu, 0+ = load that track.
        /// </summary>
        public static int SelectedTrackIndex = -1;

        void Awake()
        {
            if (SelectedTrackIndex < 0)
            {
                // Show track selection menu
                ShowTrackSelect();
            }
            else
            {
                var allTracks = TrackData.GetAllTracks();
                int idx = Mathf.Clamp(SelectedTrackIndex, 0, allTracks.Length - 1);
                BuildScene(allTracks[idx]);
            }
        }

        void ShowTrackSelect()
        {
            // Remove default camera for our UI camera
            var defaultCam = Camera.main;

            // Create a UI camera
            var camGO = new GameObject("MenuCamera");
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.06f, 0.12f);
            cam.orthographic = true;

            if (defaultCam != null && defaultCam != cam)
                Destroy(defaultCam.gameObject);

            // Add the track selection UI
            var menuGO = new GameObject("TrackSelectUI");
            menuGO.AddComponent<TrackSelectUI>();
        }

        void BuildScene(TrackData trackData)
        {
            // === LIGHTING ===
            SetupLighting(trackData);

            // === FOG ===
            SetupFog(trackData);

            // === TRACK ===
            var trackGO = new GameObject("Track");
            var spline = trackGO.AddComponent<TrackSpline>();
            spline.waypoints = trackData.waypoints;
            spline.isClosed = true;
            spline.CalculateLengths(); // Must recalculate after setting waypoints

            var trackGen = trackGO.AddComponent<TrackGenerator>();
            trackGen.roadWidth = trackData.roadWidth;
            trackGen.resolution = trackData.resolution;

            // Apply track-specific colors to the generator
            trackGen.trackRoadColor = trackData.roadColor;
            trackGen.trackCurbColor = trackData.curbColor;
            trackGen.trackGrassColor = trackData.grassColor;
            trackGen.trackBarrierColor = trackData.barrierColor;

            var checkpoints = trackGO.AddComponent<CheckpointSystem>();
            checkpoints.spline = spline;
            checkpoints.totalLaps = totalLaps;
            checkpoints.checkpointCount = 20;

            // === PLAYER KART ===
            var playerKart = CreateKart("PlayerKart", CatColorData.CreateGinger(), true,
                spline, GetStartPosition(spline, 0));

            // === AI KARTS ===
            var aiKart1 = CreateKart("AI_Shadow", CatColorData.CreateShadow(), false,
                spline, GetStartPosition(spline, 1));
            var aiKart2 = CreateKart("AI_Midnight", CatColorData.CreateMidnight(), false,
                spline, GetStartPosition(spline, 2));
            var aiKart3 = CreateKart("AI_Snow", CatColorData.CreateSnow(), false,
                spline, GetStartPosition(spline, 3));

            // === CAMERA ===
            var camGO = new GameObject("RaceCamera");
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = trackData.cameraBgColor;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 500f;

            var camCtrl = camGO.AddComponent<CameraController>();
            camCtrl.target = playerKart.transform;
            camCtrl.kart = playerKart.GetComponent<KartController>();

            // Remove default camera
            var defaultCam = Camera.main;
            if (defaultCam != null && defaultCam != cam)
                Destroy(defaultCam.gameObject);

            // === RACE MANAGER ===
            var rmGO = new GameObject("RaceManager");
            var rm = rmGO.AddComponent<RaceManager>();
            rm.spline = spline;
            rm.checkpointSystem = checkpoints;
            rm.totalLaps = totalLaps;

            // === UI ===
            var uiGO = new GameObject("RaceUI");
            var ui = uiGO.AddComponent<RaceUI>();
            ui.raceManager = rm;
            ui.checkpointSystem = checkpoints;
            ui.playerKart = playerKart.GetComponent<KartController>();

            // === ITEM BOXES ===
            PlaceItemBoxes(spline);

            // === BOOST PADS ===
            PlaceBoostPads(spline);

            // === DECORATION ===
            PlaceDecorations(spline, trackData);

            // === RESTART HANDLER ===
            gameObject.AddComponent<RestartHandler>();
        }

        GameObject CreateKart(string name, CatColorData colors, bool isPlayer,
            TrackSpline spline, Vector3 position)
        {
            var kartGO = new GameObject(name);
            kartGO.transform.position = position;
            kartGO.transform.rotation = Quaternion.LookRotation(spline.GetDirectionAtDistance(0));

            // Rigidbody
            var rb = kartGO.AddComponent<Rigidbody>();
            rb.mass = 1000f;

            // Box collider for kart body
            var col = kartGO.AddComponent<BoxCollider>();
            col.size = new Vector3(1f, 0.8f, 1.8f);
            col.center = new Vector3(0, 0.4f, 0);

            // Ground raycast origin
            var rayOrigin = new GameObject("GroundRay").transform;
            rayOrigin.SetParent(kartGO.transform, false);
            rayOrigin.localPosition = new Vector3(0, 0.5f, 0);

            // Kart stats
            var stats = ScriptableObject.CreateInstance<KartStats>();
            if (!isPlayer)
            {
                // Slight variation for AI
                stats.maxSpeed += Random.Range(-2f, 2f);
                stats.acceleration += Random.Range(-5f, 5f);
            }

            // Kart controller
            var kc = kartGO.AddComponent<KartController>();
            kc.stats = stats;
            kc.groundRayOrigin = rayOrigin;

            // Input (player or AI)
            if (isPlayer)
            {
                kartGO.AddComponent<KartInput>();
            }
            else
            {
                var ai = kartGO.AddComponent<AIInput>();
                ai.spline = spline;
                ai.kart = kc;
                ai.lookAheadDistance = Random.Range(12f, 20f);
                ai.maxSpeedMultiplier = Random.Range(0.8f, 0.95f);
                kartGO.AddComponent<AIItemUser>();
            }

            // Item holder
            kartGO.AddComponent<ItemHolder>();

            // Visuals — kart body
            var kartVisualGO = new GameObject("KartModel");
            kartVisualGO.transform.SetParent(kartGO.transform, false);
            var kartBuilder = kartVisualGO.AddComponent<KartBuilder>();
            kartBuilder.primaryColor = colors.kartPrimary;
            kartBuilder.secondaryColor = colors.kartSecondary;
            kartBuilder.accentColor = colors.kartAccent;

            // Visuals — cat driver
            var catGO = new GameObject("CatDriver");
            catGO.transform.SetParent(kartGO.transform, false);
            catGO.transform.localPosition = new Vector3(0, 0.25f, -0.05f);
            catGO.transform.localScale = Vector3.one * 0.8f;
            var catBuilder = catGO.AddComponent<CatBuilder>();
            catBuilder.colorData = colors;
            catBuilder.wearHat = isPlayer; // Only player gets the hat
            catBuilder.kart = kc;

            // Kart visuals effects
            var kv = kartGO.AddComponent<KartVisuals>();
            kv.kart = kc;
            kv.kartBody = kartVisualGO.transform;

            return kartGO;
        }

        Vector3 GetStartPosition(TrackSpline spline, int index)
        {
            // Stagger karts at the start line
            float dist = index * 4f; // 4m apart
            Vector3 pos = spline.GetPointAtDistance(spline.TotalLength - dist);
            Vector3 right = Vector3.Cross(Vector3.up, spline.GetDirectionAtDistance(spline.TotalLength - dist));
            float lateralOff = (index % 2 == 0 ? -1 : 1) * 2.5f;
            pos += right * lateralOff;
            pos.y += 1f; // Slight lift so they settle onto the road
            return pos;
        }

        void PlaceItemBoxes(TrackSpline spline)
        {
            // Place item box rows at 4 locations around the track
            float totalLen = spline.TotalLength;
            float[] placements = { 0.15f, 0.4f, 0.65f, 0.85f };

            foreach (float t in placements)
            {
                float dist = t * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                // Row of 3 item boxes
                for (int i = -1; i <= 1; i++)
                {
                    var boxGO = new GameObject($"ItemBox_{t}_{i}");
                    boxGO.transform.position = center + right * (i * 3.5f) + Vector3.up * 1.5f;
                    boxGO.AddComponent<BoxCollider>().size = new Vector3(1.5f, 1.5f, 1.5f);
                    boxGO.AddComponent<ItemBox>();
                }
            }
        }

        void PlaceBoostPads(TrackSpline spline)
        {
            float totalLen = spline.TotalLength;
            float[] placements = { 0.25f, 0.55f, 0.78f };

            foreach (float t in placements)
            {
                float dist = t * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);

                var padGO = new GameObject($"BoostPad_{t}");
                padGO.transform.position = center + Vector3.up * 0.05f;
                padGO.transform.rotation = Quaternion.LookRotation(fwd);
                padGO.AddComponent<BoxCollider>().size = new Vector3(4f, 1f, 6f);
                padGO.AddComponent<BoostPad>();
            }
        }

        // ---------------------------------------------------------------
        //  THEMED DECORATIONS
        // ---------------------------------------------------------------

        void PlaceDecorations(TrackSpline spline, TrackData trackData)
        {
            float totalLen = spline.TotalLength;

            // Sun / directional light — themed per track
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = trackData.sunIntensity;
            light.color = trackData.sunColor;
            sun.transform.rotation = Quaternion.Euler(trackData.sunRotation);

            // Spawn theme-specific decorations
            string trackName = trackData.trackName;

            if (trackName == "Catnip Gardens")
                PlaceGardensDecor(spline, totalLen);
            else if (trackName == "Catnip City")
                PlaceCityDecor(spline, totalLen);
            else if (trackName == "Meowz'es Mansion")
                PlaceMansionDecor(spline, totalLen);
            else if (trackName == "Catnip Sky Lands")
                PlaceSkyLandsDecor(spline, totalLen);
            else if (trackName == "Whisker Beach")
                PlaceBeachDecor(spline, totalLen);
            else if (trackName == "Purrfrost Peaks")
                PlacePeaksDecor(spline, totalLen);
            else if (trackName == "Tabby Toybox")
                PlaceToyboxDecor(spline, totalLen);
            else if (trackName == "Neko Nethervoid")
                PlaceNethervoidDecor(spline, totalLen);
            else
                PlaceGardensDecor(spline, totalLen); // Fallback
        }

        // --- CATNIP GARDENS: Trees + yarn balls (original) ---
        void PlaceGardensDecor(TrackSpline spline, float totalLen)
        {
            Material treeMat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.TreeLeaves(), 0.15f);
            Material trunkMat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.TreeBark(), 0.2f);
            Material yarnMat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.Yarn(new Color(0.9f, 0.2f, 0.3f)), 0.3f);

            for (int i = 0; i < 30; i++)
            {
                float dist = (i / 30f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float side = (i % 2 == 0) ? 1 : -1;
                float offset = 12f + Random.Range(3f, 10f);
                Vector3 treePos = center + right * side * offset;
                treePos.y = 0;
                CreateTree(treePos, treeMat, trunkMat);
            }

            for (int i = 0; i < 5; i++)
            {
                float dist = (i / 5f + 0.1f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float yarnOffset = Random.Range(-6f, 6f);
                Vector3 pos = center + right * yarnOffset;
                pos.y = 1.5f;

                var yarnGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                yarnGO.name = $"YarnDecor_{i}";
                yarnGO.transform.position = pos;
                yarnGO.transform.localScale = Vector3.one * 3f;
                yarnGO.GetComponent<Renderer>().material = yarnMat;
            }
        }

        // --- CATNIP CITY: Street lamps + neon signs ---
        void PlaceCityDecor(TrackSpline spline, float totalLen)
        {
            Material metalMat = MakeMat(new Color(0.3f, 0.3f, 0.35f));
            Material neonPink = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.KartPaint(new Color(0.9f, 0.1f, 0.5f)), 0.6f, 0.3f,
                new Color(0.9f, 0.1f, 0.5f) * 2f);
            Material neonBlue = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.KartPaint(new Color(0.1f, 0.4f, 0.9f)), 0.6f, 0.3f,
                new Color(0.1f, 0.4f, 0.9f) * 2f);

            // Street lamps
            for (int i = 0; i < 24; i++)
            {
                float dist = (i / 24f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float side = (i % 2 == 0) ? 1 : -1;
                Vector3 lampPos = center + right * side * 10f;
                lampPos.y = 0;

                CreateStreetLamp(lampPos, metalMat);
            }

            // Neon signs scattered along the sides
            for (int i = 0; i < 8; i++)
            {
                float dist = (i / 8f + 0.05f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float side = (i % 2 == 0) ? 1 : -1;
                Vector3 signPos = center + right * side * 12f;
                signPos.y = 4f;

                var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sign.name = $"NeonSign_{i}";
                sign.transform.position = signPos;
                sign.transform.rotation = Quaternion.LookRotation(-right * side);
                sign.transform.localScale = new Vector3(4f, 2f, 0.3f);
                sign.GetComponent<Renderer>().material = (i % 2 == 0) ? neonPink : neonBlue;
                Destroy(sign.GetComponent<Collider>());
            }
        }

        // --- MEOWZ'ES MANSION: Tombstones + dead trees ---
        void PlaceMansionDecor(TrackSpline spline, float totalLen)
        {
            Material stoneMat = MakeMat(new Color(0.35f, 0.33f, 0.3f));
            Material deadWoodMat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.TreeBark(), 0.1f);

            // Tombstones
            for (int i = 0; i < 20; i++)
            {
                float dist = (i / 20f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float side = (i % 2 == 0) ? 1 : -1;
                float offset = 10f + Random.Range(2f, 8f);
                Vector3 pos = center + right * side * offset;
                pos.y = 0;

                var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stone.name = $"Tombstone_{i}";
                stone.transform.position = pos + Vector3.up * 0.75f;
                stone.transform.localScale = new Vector3(0.6f, 1.5f, 0.15f);
                stone.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), Random.Range(-5f, 5f));
                stone.GetComponent<Renderer>().material = stoneMat;
                Destroy(stone.GetComponent<Collider>());
            }

            // Dead twisted trees (no leaves, just trunks)
            for (int i = 0; i < 15; i++)
            {
                float dist = (i / 15f + 0.03f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float side = (i % 2 == 0) ? 1 : -1;
                float offset = 14f + Random.Range(2f, 8f);
                Vector3 pos = center + right * side * offset;
                pos.y = 0;

                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = $"DeadTree_{i}";
                trunk.transform.position = pos;
                float height = Random.Range(4f, 7f);
                trunk.transform.localScale = new Vector3(0.3f, height * 0.5f, 0.3f);
                trunk.transform.position += Vector3.up * height * 0.5f;
                trunk.transform.rotation = Quaternion.Euler(Random.Range(-8f, 8f), 0, Random.Range(-8f, 8f));
                trunk.GetComponent<Renderer>().material = deadWoodMat;
                Destroy(trunk.GetComponent<Collider>());

                // Bare branches
                for (int b = 0; b < 3; b++)
                {
                    var branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    branch.transform.SetParent(trunk.transform, false);
                    branch.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
                    branch.transform.localPosition = new Vector3(
                        Random.Range(-0.5f, 0.5f), 0.6f + b * 0.2f, Random.Range(-0.5f, 0.5f));
                    branch.transform.localRotation = Quaternion.Euler(
                        Random.Range(30f, 70f), Random.Range(0, 360), 0);
                    branch.GetComponent<Renderer>().material = deadWoodMat;
                    Destroy(branch.GetComponent<Collider>());
                }
            }
        }

        // --- CATNIP SKY LANDS: Clouds + rainbow arcs ---
        void PlaceSkyLandsDecor(TrackSpline spline, float totalLen)
        {
            Material cloudMat = MakeMat(new Color(0.95f, 0.95f, 1f));
            Material rainbowMat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.KartPaint(new Color(0.9f, 0.5f, 0.3f)), 0.4f, 0f,
                new Color(0.9f, 0.6f, 0.3f) * 0.5f);

            // Floating cloud puffs below the track
            for (int i = 0; i < 25; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-80f, 130f),
                    Random.Range(-10f, -2f),
                    Random.Range(-20f, 140f));

                var cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cloud.name = $"Cloud_{i}";
                cloud.transform.position = pos;
                float scale = Random.Range(6f, 15f);
                cloud.transform.localScale = new Vector3(scale * 2f, scale * 0.6f, scale);
                cloud.GetComponent<Renderer>().material = cloudMat;
                Destroy(cloud.GetComponent<Collider>());
            }

            // Rainbow arc decorations
            for (int i = 0; i < 3; i++)
            {
                float dist = (i / 3f + 0.15f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);

                for (int a = 0; a < 8; a++)
                {
                    float angle = (a / 8f) * Mathf.PI;
                    float radius = 20f + i * 5f;
                    Vector3 arcPos = center + new Vector3(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius * 0.5f + 10f,
                        0);

                    var arcSeg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    arcSeg.name = $"Rainbow_{i}_{a}";
                    arcSeg.transform.position = arcPos;
                    arcSeg.transform.localScale = Vector3.one * 1.5f;
                    // Cycle rainbow colors
                    float hue = a / 8f;
                    Color rainbowCol = Color.HSVToRGB(hue, 0.7f, 0.9f);
                    var mat = MakeMat(rainbowCol);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", rainbowCol * 0.5f);
                    arcSeg.GetComponent<Renderer>().material = mat;
                    Destroy(arcSeg.GetComponent<Collider>());
                }
            }
        }

        // --- WHISKER BEACH: Palm trees + beach umbrellas ---
        void PlaceBeachDecor(TrackSpline spline, float totalLen)
        {
            Material palmTrunkMat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.TreeBark(), 0.15f);
            Material palmLeafMat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.TreeLeaves(), 0.1f);

            // Palm trees
            for (int i = 0; i < 20; i++)
            {
                float dist = (i / 20f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float side = (i % 2 == 0) ? 1 : -1;
                float offset = 12f + Random.Range(3f, 10f);
                Vector3 pos = center + right * side * offset;
                pos.y = 0;

                CreatePalmTree(pos, palmTrunkMat, palmLeafMat);
            }

            // Beach umbrellas
            for (int i = 0; i < 8; i++)
            {
                float dist = (i / 8f + 0.06f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float side = (i % 2 == 0) ? 1 : -1;
                Vector3 pos = center + right * side * 14f;
                pos.y = 0;

                CreateBeachUmbrella(pos, i);
            }
        }

        // --- PURRFROST PEAKS: Snow-covered pines + ice crystals ---
        void PlacePeaksDecor(TrackSpline spline, float totalLen)
        {
            Material snowPineMat = MakeMat(new Color(0.15f, 0.28f, 0.15f));
            Material snowMat = MakeMat(new Color(0.9f, 0.92f, 0.98f));
            Material trunkMat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.TreeBark(), 0.15f);
            Material iceMat = MakeMat(new Color(0.7f, 0.85f, 0.95f));
            iceMat.SetFloat("_Smoothness", 0.9f);

            // Snow-covered pine trees
            for (int i = 0; i < 25; i++)
            {
                float dist = (i / 25f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float side = (i % 2 == 0) ? 1 : -1;
                float offset = 11f + Random.Range(2f, 10f);
                Vector3 pos = center + right * side * offset;

                CreateSnowPine(pos, snowPineMat, snowMat, trunkMat);
            }

            // Ice crystal formations
            for (int i = 0; i < 6; i++)
            {
                float dist = (i / 6f + 0.08f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float side = (i % 2 == 0) ? 1 : -1;
                Vector3 pos = center + right * side * 9f;

                CreateIceCrystal(pos, iceMat);
            }
        }

        // --- TABBY TOYBOX: Toy blocks + bouncy balls ---
        void PlaceToyboxDecor(TrackSpline spline, float totalLen)
        {
            Color[] toyColors = {
                new Color(0.95f, 0.2f, 0.2f),  // Red
                new Color(0.2f, 0.6f, 0.95f),  // Blue
                new Color(0.95f, 0.85f, 0.1f), // Yellow
                new Color(0.2f, 0.85f, 0.3f),  // Green
                new Color(0.9f, 0.4f, 0.9f),   // Pink
                new Color(0.95f, 0.5f, 0.1f),  // Orange
            };

            // Scattered toy blocks
            for (int i = 0; i < 20; i++)
            {
                float dist = (i / 20f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float side = (i % 2 == 0) ? 1 : -1;
                float offset = 11f + Random.Range(2f, 8f);
                Vector3 pos = center + right * side * offset;
                pos.y = 0;

                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = $"ToyBlock_{i}";
                float blockSize = Random.Range(2f, 5f);
                block.transform.position = pos + Vector3.up * blockSize * 0.5f;
                block.transform.localScale = Vector3.one * blockSize;
                block.transform.rotation = Quaternion.Euler(0, Random.Range(0, 90), 0);

                Color blockColor = toyColors[i % toyColors.Length];
                var mat = MakeMat(blockColor);
                mat.SetFloat("_Smoothness", 0.7f);
                block.GetComponent<Renderer>().material = mat;
                Destroy(block.GetComponent<Collider>());
            }

            // Bouncy balls
            for (int i = 0; i < 10; i++)
            {
                float dist = (i / 10f + 0.05f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float side = (i % 2 == 0) ? 1 : -1;
                Vector3 pos = center + right * side * Random.Range(10f, 16f);
                float ballSize = Random.Range(1.5f, 4f);
                pos.y = ballSize * 0.5f;

                var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = $"BouncyBall_{i}";
                ball.transform.position = pos;
                ball.transform.localScale = Vector3.one * ballSize;

                Color ballColor = toyColors[(i + 3) % toyColors.Length];
                var mat = MakeMat(ballColor);
                mat.SetFloat("_Smoothness", 0.85f);
                ball.GetComponent<Renderer>().material = mat;
                Destroy(ball.GetComponent<Collider>());
            }
        }

        // --- NEKO NETHERVOID: Floating asteroids + star particles ---
        void PlaceNethervoidDecor(TrackSpline spline, float totalLen)
        {
            Material asteroidMat = MakeMat(new Color(0.15f, 0.12f, 0.2f));
            Material glowMat = MakeMat(new Color(0.1f, 0.8f, 0.9f));
            glowMat.EnableKeyword("_EMISSION");
            glowMat.SetColor("_EmissionColor", new Color(0.1f, 0.8f, 0.9f) * 3f);

            // Floating asteroid rocks
            for (int i = 0; i < 30; i++)
            {
                float dist = (i / 30f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float side = (i % 2 == 0) ? 1 : -1;
                float offset = 14f + Random.Range(3f, 15f);
                Vector3 pos = center + right * side * offset;
                pos.y += Random.Range(-5f, 10f);

                var asteroid = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                asteroid.name = $"Asteroid_{i}";
                asteroid.transform.position = pos;
                float scale = Random.Range(1.5f, 5f);
                asteroid.transform.localScale = new Vector3(
                    scale * Random.Range(0.7f, 1.3f),
                    scale * Random.Range(0.6f, 1f),
                    scale * Random.Range(0.8f, 1.2f));
                asteroid.transform.rotation = Random.rotation;
                asteroid.GetComponent<Renderer>().material = asteroidMat;
                Destroy(asteroid.GetComponent<Collider>());
            }

            // Glowing energy orbs / star particles
            for (int i = 0; i < 15; i++)
            {
                float dist = (i / 15f + 0.03f) * totalLen;
                Vector3 center = spline.GetPointAtDistance(dist);
                Vector3 pos = center + new Vector3(
                    Random.Range(-20f, 20f),
                    Random.Range(3f, 15f),
                    Random.Range(-20f, 20f));

                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = $"StarOrb_{i}";
                orb.transform.position = pos;
                orb.transform.localScale = Vector3.one * Random.Range(0.3f, 1.2f);

                // Alternate cyan/magenta/purple glow
                Color orbColor;
                switch (i % 3)
                {
                    case 0: orbColor = new Color(0.1f, 0.9f, 1f); break;
                    case 1: orbColor = new Color(0.8f, 0.1f, 0.9f); break;
                    default: orbColor = new Color(0.4f, 0.2f, 1f); break;
                }
                var orbMat = MakeMat(orbColor);
                orbMat.EnableKeyword("_EMISSION");
                orbMat.SetColor("_EmissionColor", orbColor * 4f);
                orb.GetComponent<Renderer>().material = orbMat;
                Destroy(orb.GetComponent<Collider>());
            }
        }

        // ---------------------------------------------------------------
        //  DECORATION HELPERS
        // ---------------------------------------------------------------

        void CreateTree(Vector3 pos, Material leafMat, Material trunkMat)
        {
            var tree = new GameObject("Tree");
            tree.transform.position = pos;

            float height = Random.Range(4f, 8f);
            float radius = Random.Range(2f, 3.5f);

            // Trunk
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0, height * 0.4f, 0);
            trunk.transform.localScale = new Vector3(0.4f, height * 0.4f, 0.4f);
            trunk.GetComponent<Renderer>().material = trunkMat;

            // Canopy (stacked spheres)
            for (int i = 0; i < 3; i++)
            {
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.transform.SetParent(tree.transform, false);
                float y = height * 0.6f + i * radius * 0.5f;
                float r = radius * (1f - i * 0.25f);
                leaf.transform.localPosition = new Vector3(
                    Random.Range(-0.3f, 0.3f), y, Random.Range(-0.3f, 0.3f));
                leaf.transform.localScale = Vector3.one * r;
                leaf.GetComponent<Renderer>().material = leafMat;
                Destroy(leaf.GetComponent<Collider>());
            }
        }

        void CreatePalmTree(Vector3 pos, Material trunkMat, Material leafMat)
        {
            var tree = new GameObject("PalmTree");
            tree.transform.position = pos;

            float height = Random.Range(6f, 10f);

            // Curved trunk (slightly leaning)
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0, height * 0.45f, 0);
            trunk.transform.localScale = new Vector3(0.3f, height * 0.45f, 0.3f);
            trunk.transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), 0, Random.Range(-8f, 8f));
            trunk.GetComponent<Renderer>().material = trunkMat;

            // Palm fronds (elongated spheres fanning out from top)
            for (int i = 0; i < 6; i++)
            {
                var frond = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                frond.transform.SetParent(tree.transform, false);
                float angle = (i / 6f) * 360f;
                float rad = angle * Mathf.Deg2Rad;
                frond.transform.localPosition = new Vector3(
                    Mathf.Cos(rad) * 2f,
                    height * 0.85f + Random.Range(-0.3f, 0.3f),
                    Mathf.Sin(rad) * 2f);
                frond.transform.localScale = new Vector3(1f, 0.3f, 3f);
                frond.transform.localRotation = Quaternion.Euler(
                    Random.Range(20f, 40f), angle, 0);
                frond.GetComponent<Renderer>().material = leafMat;
                Destroy(frond.GetComponent<Collider>());
            }
        }

        void CreateBeachUmbrella(Vector3 pos, int index)
        {
            Color[] umbrellaColors = {
                new Color(0.95f, 0.3f, 0.2f),
                new Color(0.2f, 0.7f, 0.95f),
                new Color(0.95f, 0.85f, 0.2f),
                new Color(0.3f, 0.9f, 0.4f),
            };

            var umbrella = new GameObject($"Umbrella_{index}");
            umbrella.transform.position = pos;

            // Pole
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(umbrella.transform, false);
            pole.transform.localPosition = Vector3.up * 1.5f;
            pole.transform.localScale = new Vector3(0.08f, 1.5f, 0.08f);
            pole.GetComponent<Renderer>().material = MakeMat(new Color(0.8f, 0.75f, 0.6f));
            Destroy(pole.GetComponent<Collider>());

            // Canopy
            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.transform.SetParent(umbrella.transform, false);
            canopy.transform.localPosition = Vector3.up * 3f;
            canopy.transform.localScale = new Vector3(3f, 0.4f, 3f);
            Color uColor = umbrellaColors[index % umbrellaColors.Length];
            canopy.GetComponent<Renderer>().material = MakeMat(uColor);
            Destroy(canopy.GetComponent<Collider>());
        }

        void CreateSnowPine(Vector3 pos, Material pineMat, Material snowMat, Material trunkMat)
        {
            var tree = new GameObject("SnowPine");
            tree.transform.position = pos;

            float height = Random.Range(5f, 9f);

            // Trunk
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = Vector3.up * height * 0.3f;
            trunk.transform.localScale = new Vector3(0.25f, height * 0.3f, 0.25f);
            trunk.GetComponent<Renderer>().material = trunkMat;

            // Pine cone shapes (stacked, getting smaller)
            for (int i = 0; i < 4; i++)
            {
                var layer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                layer.transform.SetParent(tree.transform, false);
                float y = height * 0.4f + i * height * 0.15f;
                float r = (2.5f - i * 0.5f);
                layer.transform.localPosition = new Vector3(0, y, 0);
                layer.transform.localScale = new Vector3(r, r * 0.6f, r);
                layer.GetComponent<Renderer>().material = pineMat;
                Destroy(layer.GetComponent<Collider>());
            }

            // Snow cap on top
            var snowCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            snowCap.transform.SetParent(tree.transform, false);
            snowCap.transform.localPosition = new Vector3(0, height * 0.85f, 0);
            snowCap.transform.localScale = new Vector3(1.5f, 0.5f, 1.5f);
            snowCap.GetComponent<Renderer>().material = snowMat;
            Destroy(snowCap.GetComponent<Collider>());
        }

        void CreateIceCrystal(Vector3 pos, Material iceMat)
        {
            var crystal = new GameObject("IceCrystal");
            crystal.transform.position = pos;

            // Main crystal shard (tall narrow cube)
            var main = GameObject.CreatePrimitive(PrimitiveType.Cube);
            main.transform.SetParent(crystal.transform, false);
            float mainHeight = Random.Range(3f, 6f);
            main.transform.localPosition = Vector3.up * mainHeight * 0.5f;
            main.transform.localScale = new Vector3(0.5f, mainHeight, 0.5f);
            main.transform.localRotation = Quaternion.Euler(
                Random.Range(-10f, 10f), Random.Range(0, 360), Random.Range(-10f, 10f));
            main.GetComponent<Renderer>().material = iceMat;
            Destroy(main.GetComponent<Collider>());

            // Smaller shards around the base
            for (int i = 0; i < 3; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.transform.SetParent(crystal.transform, false);
                float h = mainHeight * Random.Range(0.3f, 0.6f);
                float angle = (i / 3f) * 360f;
                float rad = angle * Mathf.Deg2Rad;
                shard.transform.localPosition = new Vector3(
                    Mathf.Cos(rad) * 0.8f,
                    h * 0.5f,
                    Mathf.Sin(rad) * 0.8f);
                shard.transform.localScale = new Vector3(0.3f, h, 0.3f);
                shard.transform.localRotation = Quaternion.Euler(
                    Random.Range(-15f, 15f), angle, Random.Range(-15f, 15f));
                shard.GetComponent<Renderer>().material = iceMat;
                Destroy(shard.GetComponent<Collider>());
            }
        }

        void CreateStreetLamp(Vector3 pos, Material metalMat)
        {
            var lamp = new GameObject("StreetLamp");
            lamp.transform.position = pos;

            // Pole
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(lamp.transform, false);
            pole.transform.localPosition = Vector3.up * 3f;
            pole.transform.localScale = new Vector3(0.12f, 3f, 0.12f);
            pole.GetComponent<Renderer>().material = metalMat;
            Destroy(pole.GetComponent<Collider>());

            // Light fixture
            var fixture = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fixture.transform.SetParent(lamp.transform, false);
            fixture.transform.localPosition = Vector3.up * 6.2f;
            fixture.transform.localScale = Vector3.one * 0.6f;

            var lightMat = MakeMat(new Color(1f, 0.9f, 0.6f));
            lightMat.EnableKeyword("_EMISSION");
            lightMat.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.6f) * 3f);
            fixture.GetComponent<Renderer>().material = lightMat;
            Destroy(fixture.GetComponent<Collider>());

            // Actual point light
            var pointLight = new GameObject("LampLight");
            pointLight.transform.SetParent(lamp.transform, false);
            pointLight.transform.localPosition = Vector3.up * 6f;
            var lt = pointLight.AddComponent<Light>();
            lt.type = LightType.Point;
            lt.range = 15f;
            lt.intensity = 1.5f;
            lt.color = new Color(1f, 0.85f, 0.5f);
        }

        // ---------------------------------------------------------------
        //  LIGHTING + FOG
        // ---------------------------------------------------------------

        void SetupLighting(TrackData trackData)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = trackData.ambientColor;

            // Procedural skybox with track-specific colors
            var skyTex = ProceduralTextureLib.SkyGradient(
                trackData.skyZenith, trackData.skyHorizon, trackData.skySunGlow);
            skyTex.wrapMode = TextureWrapMode.Clamp;
            var skyShader = Shader.Find("Skybox/Panoramic");
            if (skyShader == null) skyShader = Shader.Find("Skybox/6 Sided");
            if (skyShader == null) skyShader = ProceduralTextureLib.FindLitShader();
            var skyMat = new Material(skyShader);
            if (skyMat != null)
            {
                skyMat.SetTexture("_MainTex", skyTex);
                RenderSettings.skybox = skyMat;
            }
        }

        void SetupFog(TrackData trackData)
        {
            if (trackData.fogDensity > 0f)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = trackData.fogDensity;
                RenderSettings.fogColor = trackData.fogColor;
            }
            else
            {
                RenderSettings.fog = false;
            }
        }

        Material MakeMat(Color c)
        {
            var mat = new Material(ProceduralTextureLib.FindLitShader());
            mat.color = c;
            return mat;
        }
    }

    /// <summary>Simple restart handler. Also returns to track select on Escape.</summary>
    public class RestartHandler : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Return to track selection
                SceneSetup.SelectedTrackIndex = -1;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
