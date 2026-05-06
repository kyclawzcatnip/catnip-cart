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
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        [Header("Race Config")]
        public int totalLaps = 3;

        void Awake()
        {
            BuildScene();
        }

        void BuildScene()
        {
            // === LIGHTING ===
            SetupLighting();

            // === TRACK ===
            var trackGO = new GameObject("Track");
            var spline = trackGO.AddComponent<TrackSpline>();
            spline.waypoints = CreateCatnipGardensLayout();
            spline.isClosed = true;

            var trackGen = trackGO.AddComponent<TrackGenerator>();
            trackGen.roadWidth = 14f;
            trackGen.resolution = 200;

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
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.5f, 0.75f, 1f);
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
            PlaceDecorations(spline);

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

        System.Collections.Generic.List<Vector3> CreateCatnipGardensLayout()
        {
            // Fun circuit with variety of turns
            return new System.Collections.Generic.List<Vector3>
            {
                new Vector3(0, 0, 0),
                new Vector3(40, 0, 10),
                new Vector3(80, 0, 5),
                new Vector3(110, 0, 30),    // Gentle right
                new Vector3(120, 0, 70),
                new Vector3(100, 0, 110),   // Hairpin left
                new Vector3(60, 0, 120),
                new Vector3(30, 2, 130),    // Slight uphill
                new Vector3(-10, 3, 120),
                new Vector3(-40, 2, 100),   // Downhill
                new Vector3(-60, 0, 70),
                new Vector3(-70, 0, 40),    // Tight right
                new Vector3(-50, 0, 10),
                new Vector3(-20, 0, -10),   // S-curve back to start
            };
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

        void PlaceDecorations(TrackSpline spline)
        {
            float totalLen = spline.TotalLength;
            Material treeMat = MakeMat(new Color(0.15f, 0.5f, 0.1f));
            Material trunkMat = MakeMat(new Color(0.45f, 0.3f, 0.15f));
            Material yarnMat = MakeMat(new Color(0.9f, 0.2f, 0.3f));

            // Trees along the track
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

            // Giant yarn balls as obstacles
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
                // Keep collider for bouncing
            }

            // Sun / directional light
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.95f, 0.85f);
            sun.transform.rotation = Quaternion.Euler(45, -30, 0);
        }

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

        void SetupLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.65f);
        }

        Material MakeMat(Color c)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = c;
            return mat;
        }
    }

    /// <summary>Simple restart handler.</summary>
    public class RestartHandler : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
