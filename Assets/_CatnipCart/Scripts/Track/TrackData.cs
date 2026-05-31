using UnityEngine;
using System.Collections.Generic;

namespace CatnipCart.Track
{
    /// <summary>
    /// Defines all data for a single race track: layout, colors, lighting, sky.
    /// Use the static factory methods to create built-in tracks.
    /// </summary>
    public class TrackData
    {
        // Identity
        public string trackName;
        public string trackEmoji;
        public string trackDescription;

        // Layout
        public List<Vector3> waypoints;
        public float roadWidth = 14f;
        public int resolution = 200;

        // Surface colors
        public Color roadColor;
        public Color curbColor;
        public Color grassColor;
        public Color barrierColor;

        // Lighting
        public Color ambientColor;
        public Color sunColor;
        public Vector3 sunRotation;
        public float sunIntensity = 1.2f;

        // Sky gradient
        public Color skyZenith;
        public Color skyHorizon;
        public Color skySunGlow;

        // Camera
        public Color cameraBgColor;

        // Fog
        public float fogDensity = 0f; // 0 = no fog, >0 = fog enabled
        public Color fogColor = Color.white;

        // ---------------------------------------------------------------
        //  CATNIP GARDENS (original track)
        // ---------------------------------------------------------------
        public static TrackData CreateCatnipGardens()
        {
            return new TrackData
            {
                trackName = "Catnip Gardens",
                trackEmoji = "🌿",
                trackDescription = "A sunny garden circuit with gentle hills and yarn ball obstacles",

                waypoints = new List<Vector3>
                {
                    new Vector3(0, 0, 0),
                    new Vector3(40, 0, 10),
                    new Vector3(80, 0, 5),
                    new Vector3(110, 0, 30),
                    new Vector3(120, 0, 70),
                    new Vector3(100, 0, 110),
                    new Vector3(60, 0, 120),
                    new Vector3(30, 2, 130),
                    new Vector3(-10, 3, 120),
                    new Vector3(-40, 2, 100),
                    new Vector3(-60, 0, 70),
                    new Vector3(-70, 0, 40),
                    new Vector3(-50, 0, 10),
                    new Vector3(-20, 0, -10),
                },

                roadWidth = 14f,
                resolution = 200,

                roadColor = new Color(0.3f, 0.3f, 0.35f),
                curbColor = new Color(0.9f, 0.2f, 0.2f),
                grassColor = new Color(0.2f, 0.65f, 0.15f),
                barrierColor = new Color(0.85f, 0.85f, 0.9f),

                ambientColor = new Color(0.5f, 0.55f, 0.65f),
                sunColor = new Color(1f, 0.95f, 0.85f),
                sunRotation = new Vector3(45, -30, 0),
                sunIntensity = 1.2f,

                skyZenith = new Color(0.28f, 0.5f, 0.85f),
                skyHorizon = new Color(0.7f, 0.82f, 0.95f),
                skySunGlow = new Color(1f, 0.92f, 0.7f),

                cameraBgColor = new Color(0.5f, 0.75f, 1f),
            };
        }

        // ---------------------------------------------------------------
        //  CATNIP CITY (neon night race)
        // ---------------------------------------------------------------
        public static TrackData CreateCatnipCity()
        {
            return new TrackData
            {
                trackName = "Catnip City",
                trackEmoji = "🏙️",
                trackDescription = "A neon-lit night race through the cat metropolis streets",

                // Tight rectangular street circuit with sharp corners
                waypoints = new List<Vector3>
                {
                    // Start / main straight (boulevard)
                    new Vector3(0, 0, 0),
                    new Vector3(50, 0, 0),
                    new Vector3(100, 0, 0),
                    // Right turn into side street
                    new Vector3(120, 0, 10),
                    new Vector3(125, 0, 40),
                    new Vector3(125, 0, 70),
                    // Chicane alley
                    new Vector3(115, 0, 90),
                    new Vector3(100, 0, 100),
                    new Vector3(85, 0, 95),
                    // Back straight
                    new Vector3(70, 0, 105),
                    new Vector3(40, 0, 110),
                    new Vector3(10, 0, 105),
                    // Sharp left into avenue
                    new Vector3(-10, 0, 90),
                    new Vector3(-15, 0, 60),
                    new Vector3(-10, 0, 35),
                    // Final curve back to start
                    new Vector3(-5, 0, 15),
                },

                roadWidth = 13f,
                resolution = 220,

                // Dark asphalt, yellow curbs (city style)
                roadColor = new Color(0.15f, 0.15f, 0.18f),
                curbColor = new Color(0.95f, 0.75f, 0.1f),
                grassColor = new Color(0.12f, 0.12f, 0.14f), // Sidewalk gray
                barrierColor = new Color(0.4f, 0.4f, 0.45f),

                // Night lighting
                ambientColor = new Color(0.12f, 0.1f, 0.2f),
                sunColor = new Color(0.4f, 0.45f, 0.7f), // Moonlight blue
                sunRotation = new Vector3(25, 60, 0),
                sunIntensity = 0.5f,

                // Night sky
                skyZenith = new Color(0.02f, 0.02f, 0.08f),
                skyHorizon = new Color(0.1f, 0.06f, 0.18f),
                skySunGlow = new Color(0.3f, 0.15f, 0.5f), // Neon purple glow

                cameraBgColor = new Color(0.05f, 0.03f, 0.1f),
            };
        }

        // ---------------------------------------------------------------
        //  MEOWZ'ES MANSION (spooky haunted)
        // ---------------------------------------------------------------
        public static TrackData CreateMeowzesMansion()
        {
            return new TrackData
            {
                trackName = "Meowz'es Mansion",
                trackEmoji = "🏚️",
                trackDescription = "A spooky race around the haunted mansion grounds",

                // Figure-8 inspired with elevation changes
                waypoints = new List<Vector3>
                {
                    // Start at mansion entrance
                    new Vector3(0, 0, 0),
                    new Vector3(30, 0, 15),
                    new Vector3(60, 1, 35),
                    // Climb up the hill
                    new Vector3(80, 4, 60),
                    new Vector3(85, 6, 90),
                    // Hairpin around tower
                    new Vector3(70, 5, 110),
                    new Vector3(45, 3, 115),
                    // Cross-over bridge section (elevated)
                    new Vector3(20, 6, 100),
                    new Vector3(0, 6, 80),
                    // Descend through graveyard
                    new Vector3(-20, 3, 60),
                    new Vector3(-35, 1, 40),
                    // Tight curves through garden maze
                    new Vector3(-45, 0, 20),
                    new Vector3(-40, -1, 0),
                    new Vector3(-25, -2, -15),
                    // Underground dip
                    new Vector3(-5, -1, -10),
                },

                roadWidth = 13f,
                resolution = 210,

                // Dark cobblestone, gothic colors
                roadColor = new Color(0.25f, 0.22f, 0.2f),
                curbColor = new Color(0.3f, 0.15f, 0.35f), // Dark purple
                grassColor = new Color(0.12f, 0.2f, 0.08f), // Dead dark grass
                barrierColor = new Color(0.2f, 0.2f, 0.22f), // Iron fence dark

                // Eerie moonlit atmosphere
                ambientColor = new Color(0.15f, 0.2f, 0.18f),
                sunColor = new Color(0.6f, 0.65f, 0.8f), // Pale moonlight
                sunRotation = new Vector3(15, -60, 0), // Low moon
                sunIntensity = 0.6f,

                // Spooky purple-green sky
                skyZenith = new Color(0.05f, 0.02f, 0.1f),
                skyHorizon = new Color(0.15f, 0.22f, 0.12f),
                skySunGlow = new Color(0.2f, 0.35f, 0.15f), // Sickly green glow

                cameraBgColor = new Color(0.08f, 0.06f, 0.12f),
            };
        }

        // ---------------------------------------------------------------
        //  CATNIP SKY LANDS (floating islands dream)
        // ---------------------------------------------------------------
        public static TrackData CreateCatnipSkyLands()
        {
            return new TrackData
            {
                trackName = "Catnip Sky Lands",
                trackEmoji = "☁️",
                trackDescription = "Race across floating islands high above the clouds",

                // Large sweeping curves with dramatic elevation
                waypoints = new List<Vector3>
                {
                    // Starting island
                    new Vector3(0, 0, 0),
                    new Vector3(35, 2, 20),
                    // Bridge to second island
                    new Vector3(70, 5, 30),
                    // Spiral climb
                    new Vector3(100, 10, 50),
                    new Vector3(110, 16, 80),
                    new Vector3(95, 22, 110),
                    // Summit peak
                    new Vector3(70, 25, 120),
                    // Thrilling descent
                    new Vector3(40, 18, 125),
                    new Vector3(10, 12, 115),
                    // Floating bridge (stays high)
                    new Vector3(-15, 10, 95),
                    new Vector3(-25, 8, 65),
                    // Steep drop
                    new Vector3(-30, 3, 40),
                    // Rainbow curve back
                    new Vector3(-20, 1, 15),
                },

                roadWidth = 15f, // Wider for floating feel
                resolution = 200,

                // Ethereal white/gold road
                roadColor = new Color(0.75f, 0.72f, 0.65f),
                curbColor = new Color(0.4f, 0.6f, 0.9f), // Sky blue curbs
                grassColor = new Color(0.5f, 0.75f, 0.4f), // Lush bright green
                barrierColor = new Color(0.8f, 0.85f, 0.95f), // Cloud white

                // Bright dreamy atmosphere
                ambientColor = new Color(0.65f, 0.6f, 0.7f),
                sunColor = new Color(1f, 0.9f, 0.6f), // Golden sun
                sunRotation = new Vector3(35, 45, 0),
                sunIntensity = 1.5f,

                // Dreamy pink-orange-blue sky
                skyZenith = new Color(0.35f, 0.45f, 0.85f),
                skyHorizon = new Color(0.95f, 0.7f, 0.55f),
                skySunGlow = new Color(1f, 0.8f, 0.5f),

                cameraBgColor = new Color(0.7f, 0.8f, 1f),
            };
        }

        // ---------------------------------------------------------------
        //  WHISKER BEACH (tropical sunset coastal race)
        // ---------------------------------------------------------------
        public static TrackData CreateWhiskerBeach()
        {
            return new TrackData
            {
                trackName = "Whisker Beach",
                trackEmoji = "🏖️",
                trackDescription = "A tropical sunset cruise along the coast with wide sweeping turns",

                // Wide sweeping coastal curves, gentle cliffside elevation
                waypoints = new List<Vector3>
                {
                    // Start on sandy straightaway
                    new Vector3(0, 0, 0),
                    new Vector3(50, 0, 10),
                    // Gentle right along the shore
                    new Vector3(95, 0, 30),
                    new Vector3(130, 1, 60),
                    // Clifftop section with slight elevation
                    new Vector3(145, 4, 100),
                    new Vector3(135, 6, 140),
                    // Wide left sweeper along bluff
                    new Vector3(105, 5, 170),
                    new Vector3(65, 3, 180),
                    // Descend back toward beach
                    new Vector3(25, 1, 170),
                    new Vector3(-10, 0, 145),
                    // Beachside curve
                    new Vector3(-30, 0, 110),
                    new Vector3(-35, 0, 75),
                    // Gentle right back inland
                    new Vector3(-25, 0, 45),
                    new Vector3(-10, 0, 15),
                },

                roadWidth = 15f,
                resolution = 200,

                // Sandy tan road, coral curbs, warm palette
                roadColor = new Color(0.72f, 0.62f, 0.45f),
                curbColor = new Color(0.92f, 0.45f, 0.3f),
                grassColor = new Color(0.82f, 0.72f, 0.5f),
                barrierColor = new Color(0.9f, 0.8f, 0.65f),

                // Golden hour sunset lighting
                ambientColor = new Color(0.6f, 0.45f, 0.35f),
                sunColor = new Color(1f, 0.75f, 0.4f),
                sunRotation = new Vector3(12, -45, 0), // Low sun near horizon
                sunIntensity = 1.3f,

                // Sunset sky
                skyZenith = new Color(0.25f, 0.35f, 0.7f),
                skyHorizon = new Color(1f, 0.55f, 0.25f),
                skySunGlow = new Color(1f, 0.7f, 0.2f),

                cameraBgColor = new Color(0.9f, 0.6f, 0.35f),
            };
        }

        // ---------------------------------------------------------------
        //  PURRFROST PEAKS (icy mountain alpine circuit)
        // ---------------------------------------------------------------
        public static TrackData CreatePurrfrostPeaks()
        {
            return new TrackData
            {
                trackName = "Purrfrost Peaks",
                trackEmoji = "🏔️",
                trackDescription = "A treacherous alpine circuit with steep switchbacks and icy hairpins",

                // Steep elevation, tight switchbacks, hairpin turns
                waypoints = new List<Vector3>
                {
                    // Base camp start
                    new Vector3(0, 0, 0),
                    new Vector3(35, 3, 20),
                    // Begin steep climb
                    new Vector3(60, 8, 50),
                    new Vector3(75, 14, 80),
                    // First switchback right
                    new Vector3(55, 18, 100),
                    new Vector3(30, 22, 95),
                    // Hairpin left up the ridge
                    new Vector3(15, 26, 75),
                    new Vector3(30, 30, 55),
                    // Summit pass
                    new Vector3(55, 28, 40),
                    // Steep descent switchback
                    new Vector3(70, 22, 20),
                    new Vector3(80, 16, -5),
                    // Icy valley bottom
                    new Vector3(65, 8, -25),
                    new Vector3(40, 4, -30),
                    // Final curve through glacier
                    new Vector3(15, 2, -25),
                    new Vector3(-5, 0, -15),
                    new Vector3(-10, 0, -5),
                },

                roadWidth = 12f,
                resolution = 220,

                // Icy palette
                roadColor = new Color(0.6f, 0.65f, 0.72f),
                curbColor = new Color(0.5f, 0.7f, 0.9f),
                grassColor = new Color(0.88f, 0.9f, 0.95f), // Snow white
                barrierColor = new Color(0.65f, 0.8f, 0.92f), // Glacier blue

                // Cold dim lighting
                ambientColor = new Color(0.35f, 0.4f, 0.55f),
                sunColor = new Color(0.7f, 0.78f, 0.95f), // Pale blue
                sunRotation = new Vector3(20, 30, 0),
                sunIntensity = 0.7f,

                // Overcast icy sky
                skyZenith = new Color(0.45f, 0.55f, 0.75f),
                skyHorizon = new Color(0.78f, 0.82f, 0.9f),
                skySunGlow = new Color(0.7f, 0.75f, 0.85f),

                cameraBgColor = new Color(0.6f, 0.7f, 0.85f),

                fogDensity = 0.015f,
                fogColor = new Color(0.75f, 0.82f, 0.92f), // White-blue fog
            };
        }

        // ---------------------------------------------------------------
        //  TABBY TOYBOX (giant oversized playroom)
        // ---------------------------------------------------------------
        public static TrackData CreateTabbyToybox()
        {
            return new TrackData
            {
                trackName = "Tabby Toybox",
                trackEmoji = "🧸",
                trackDescription = "A playful race through a giant toybox full of colorful surprises",

                // Tight playful turns, figure-8 feel, mostly flat with bumps
                waypoints = new List<Vector3>
                {
                    // Start on the building block straight
                    new Vector3(0, 0, 0),
                    new Vector3(40, 0, 5),
                    // Quick right into loop
                    new Vector3(70, 1, 25),
                    new Vector3(80, 2, 55),
                    // Sharp left (figure-8 crossover zone)
                    new Vector3(65, 3, 80),
                    new Vector3(35, 2, 90),
                    // Tight S-curve through toy blocks
                    new Vector3(10, 1, 80),
                    new Vector3(-5, 0, 55),
                    // Loop back right
                    new Vector3(-15, 1, 30),
                    new Vector3(-25, 2, 5),
                    // Wide left sweeper around dollhouse
                    new Vector3(-35, 1, -20),
                    new Vector3(-20, 0, -40),
                    // Return straight through marble run
                    new Vector3(10, 0, -35),
                    new Vector3(30, 0, -20),
                },

                roadWidth = 13f,
                resolution = 200,

                // Bright primary colors
                roadColor = new Color(0.95f, 0.85f, 0.2f),  // Bright yellow
                curbColor = new Color(0.9f, 0.15f, 0.15f),  // Red
                grassColor = new Color(0.15f, 0.85f, 0.2f),  // Vivid green
                barrierColor = new Color(0.2f, 0.45f, 0.95f), // Blue

                // Very bright indoor lighting
                ambientColor = new Color(0.7f, 0.7f, 0.72f),
                sunColor = new Color(1f, 1f, 1f),
                sunRotation = new Vector3(60, 0, 0), // Overhead
                sunIntensity = 1.6f,

                // Bright cheerful sky (indoor ceiling glow)
                skyZenith = new Color(0.5f, 0.75f, 1f),
                skyHorizon = new Color(0.85f, 0.9f, 1f),
                skySunGlow = new Color(1f, 1f, 0.9f),

                cameraBgColor = new Color(0.7f, 0.85f, 1f),
            };
        }

        // ---------------------------------------------------------------
        //  NEKO NETHERVOID (cosmic void / space track)
        // ---------------------------------------------------------------
        public static TrackData CreateNekoNethervoid()
        {
            return new TrackData
            {
                trackName = "Neko Nethervoid",
                trackEmoji = "🌌",
                trackDescription = "A cosmic race through the swirling void between galaxies",

                // Large sweeping galactic arcs, moderate floating elevation
                waypoints = new List<Vector3>
                {
                    // Launch from void platform
                    new Vector3(0, 0, 0),
                    new Vector3(55, 3, 15),
                    // Grand right arc through nebula
                    new Vector3(110, 8, 45),
                    new Vector3(140, 14, 90),
                    // Apex of cosmic loop
                    new Vector3(130, 18, 140),
                    new Vector3(95, 15, 175),
                    // Sweeping left into star field
                    new Vector3(45, 10, 185),
                    new Vector3(0, 6, 170),
                    // Galactic descent
                    new Vector3(-35, 3, 140),
                    new Vector3(-55, 0, 100),
                    // Dark matter chicane
                    new Vector3(-50, -2, 60),
                    new Vector3(-35, -1, 30),
                    // Re-entry sweep back
                    new Vector3(-15, 0, 10),
                },

                roadWidth = 16f,
                resolution = 200,

                // Dark cosmic palette
                roadColor = new Color(0.08f, 0.05f, 0.12f),   // Very dark purple-black
                curbColor = new Color(0.7f, 0.15f, 0.6f),     // Magenta
                grassColor = new Color(0.04f, 0.02f, 0.08f),   // Dark void
                barrierColor = new Color(0.1f, 0.9f, 0.85f),   // Glowing cyan

                // Dim cosmic lighting
                ambientColor = new Color(0.1f, 0.06f, 0.18f),
                sunColor = new Color(0.5f, 0.3f, 0.7f),       // Purple-tinted
                sunRotation = new Vector3(30, -90, 0),
                sunIntensity = 0.4f,

                // Deep void sky
                skyZenith = new Color(0.02f, 0.01f, 0.06f),    // Near-black purple
                skyHorizon = new Color(0.25f, 0.05f, 0.2f),    // Magenta horizon
                skySunGlow = new Color(0.4f, 0.1f, 0.5f),      // Purple glow

                cameraBgColor = new Color(0.03f, 0.01f, 0.08f),
            };
        }

        /// <summary>
        /// Returns all available tracks.
        /// </summary>
        public static TrackData[] GetAllTracks()
        {
            return new TrackData[]
            {
                CreateCatnipGardens(),
                CreateCatnipCity(),
                CreateMeowzesMansion(),
                CreateCatnipSkyLands(),
                CreateWhiskerBeach(),
                CreatePurrfrostPeaks(),
                CreateTabbyToybox(),
                CreateNekoNethervoid(),
            };
        }
    }
}
