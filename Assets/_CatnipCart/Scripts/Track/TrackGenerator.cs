using UnityEngine;
using System.Collections.Generic;
using CatnipCart.Core;

namespace CatnipCart.Track
{
    /// <summary>
    /// Generates the track mesh along the TrackSpline.
    /// Creates road surface, curbs, grass borders, and barrier colliders.
    /// Supports per-track color theming via the trackXxxColor fields.
    /// </summary>
    [RequireComponent(typeof(TrackSpline))]
    public class TrackGenerator : MonoBehaviour
    {
        [Header("Track Dimensions")]
        public float roadWidth = 12f;
        public float curbWidth = 1.5f;
        public float grassWidth = 30f;

        [Header("Generation")]
        public int resolution = 200;
        public Material roadMaterial;
        public Material curbMaterial;
        public Material grassMaterial;
        public Material barrierMaterial;

        [Header("Barriers")]
        public float barrierHeight = 2f;

        [Header("Track Theme Colors (set by SceneSetup)")]
        public Color trackRoadColor = Color.clear;
        public Color trackCurbColor = Color.clear;
        public Color trackGrassColor = Color.clear;
        public Color trackBarrierColor = Color.clear;

        private TrackSpline spline;

        void Start()
        {
            spline = GetComponent<TrackSpline>();
            if (spline.waypoints.Count < 2) return;

            GenerateRoad();
            GenerateBarriers();
        }

        void GenerateRoad()
        {
            // Create road mesh
            CreateExtrudedMesh("Road", roadWidth, 0f, GetRoadMat());
            // Curbs
            CreateExtrudedMesh("CurbLeft", curbWidth, roadWidth / 2f, GetCurbMat(), true);
            CreateExtrudedMesh("CurbRight", curbWidth, -(roadWidth / 2f + curbWidth), GetCurbMat(), true);
            // Grass
            CreateExtrudedMesh("GrassLeft", grassWidth, roadWidth / 2f + curbWidth, GetGrassMat());
            CreateExtrudedMesh("GrassRight", grassWidth, -(roadWidth / 2f + curbWidth + grassWidth), GetGrassMat());
        }

        void CreateExtrudedMesh(string name, float width, float offset, Material mat, bool isCurb = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = mat;
            var mc = go.AddComponent<MeshCollider>();

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            float totalLen = spline.TotalLength;
            for (int i = 0; i <= resolution; i++)
            {
                float t = (i / (float)resolution) * totalLen;
                Vector3 center = spline.GetPointAtDistance(t);
                Vector3 fwd = spline.GetDirectionAtDistance(t);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                Vector3 leftEdge = center + right * (offset + width);
                Vector3 rightEdge = center + right * offset;

                // Slight Y offset for curbs
                if (isCurb) { leftEdge.y += 0.15f; rightEdge.y += 0.15f; }

                verts.Add(leftEdge);
                verts.Add(rightEdge);
                uvs.Add(new Vector2(0, i / (float)resolution * 10f));
                uvs.Add(new Vector2(1, i / (float)resolution * 10f));

                if (i > 0)
                {
                    int vi = (i - 1) * 2;
                    tris.Add(vi); tris.Add(vi + 2); tris.Add(vi + 1);
                    tris.Add(vi + 1); tris.Add(vi + 2); tris.Add(vi + 3);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = verts.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.mesh = mesh;
            mc.sharedMesh = mesh;

            go.layer = LayerMask.NameToLayer("Default");
        }

        void GenerateBarriers()
        {
            float totalLen = spline.TotalLength;
            float spacing = totalLen / resolution;

            for (int i = 0; i < resolution; i++)
            {
                float t = (i / (float)resolution) * totalLen;
                Vector3 center = spline.GetPointAtDistance(t);
                Vector3 fwd = spline.GetDirectionAtDistance(t);
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                float barrierOffset = roadWidth / 2f + curbWidth + 0.5f;

                // Left barrier
                CreateBarrierSegment($"BarrierL_{i}", center + right * barrierOffset, fwd, spacing);
                // Right barrier
                CreateBarrierSegment($"BarrierR_{i}", center - right * barrierOffset, fwd, spacing);
            }
        }

        void CreateBarrierSegment(string name, Vector3 pos, Vector3 fwd, float length)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos + Vector3.up * barrierHeight * 0.5f;
            go.transform.rotation = Quaternion.LookRotation(fwd);

            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(0.5f, barrierHeight, length);

            // Visual
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(0.5f, barrierHeight, length);
            visual.GetComponent<Renderer>().material = GetBarrierMat();
            Destroy(visual.GetComponent<Collider>());
        }

        // ---------------------------------------------------------------
        //  MATERIAL GETTERS — tinted by track theme colors when set
        // ---------------------------------------------------------------

        Material GetRoadMat()
        {
            if (roadMaterial) return roadMaterial;
            var mat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.Asphalt(), 0.2f, 0f, null,
                new Vector2(1f, 10f));
            if (trackRoadColor != Color.clear)
                mat.color = trackRoadColor;
            return mat;
        }

        Material GetCurbMat()
        {
            if (curbMaterial) return curbMaterial;
            var mat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.RacingStripes(), 0.3f);
            if (trackCurbColor != Color.clear)
                mat.color = trackCurbColor;
            return mat;
        }

        Material GetGrassMat()
        {
            if (grassMaterial) return grassMaterial;
            var mat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.Grass(), 0.1f, 0f, null,
                new Vector2(1f, 10f));
            if (trackGrassColor != Color.clear)
                mat.color = trackGrassColor;
            return mat;
        }

        Material GetBarrierMat()
        {
            if (barrierMaterial) return barrierMaterial;
            var mat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.Barrier(), 0.4f, 0.2f);
            if (trackBarrierColor != Color.clear)
                mat.color = trackBarrierColor;
            return mat;
        }
    }
}
