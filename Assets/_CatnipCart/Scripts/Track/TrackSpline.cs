using UnityEngine;
using System.Collections.Generic;

namespace CatnipCart.Track
{
    /// <summary>
    /// Catmull-Rom spline defining the race circuit.
    /// Used by AI, checkpoint system, track mesh generator, and camera.
    /// </summary>
    public class TrackSpline : MonoBehaviour
    {
        [Header("Waypoints")]
        public List<Vector3> waypoints = new List<Vector3>();
        public bool isClosed = true;

        [Header("Debug")]
        public Color gizmoColor = Color.yellow;
        public int gizmoResolution = 20;

        public float TotalLength { get; private set; }
        private float[] segmentLengths;
        private float[] cumulativeLengths;

        void Awake()
        {
            CalculateLengths();
        }

        void CalculateLengths()
        {
            if (waypoints.Count < 2) return;
            int segCount = isClosed ? waypoints.Count : waypoints.Count - 1;
            segmentLengths = new float[segCount];
            cumulativeLengths = new float[segCount];
            TotalLength = 0;

            for (int i = 0; i < segCount; i++)
            {
                float len = 0;
                Vector3 prev = GetCatmullRomPoint(i, 0);
                for (int s = 1; s <= 20; s++)
                {
                    Vector3 cur = GetCatmullRomPoint(i, s / 20f);
                    len += Vector3.Distance(prev, cur);
                    prev = cur;
                }
                segmentLengths[i] = len;
                TotalLength += len;
                cumulativeLengths[i] = TotalLength;
            }
        }

        /// <summary>Get a point on the spline at a given distance (0 to TotalLength).</summary>
        public Vector3 GetPointAtDistance(float dist)
        {
            if (waypoints.Count < 2) return Vector3.zero;
            if (isClosed) dist = Mathf.Repeat(dist, TotalLength);
            else dist = Mathf.Clamp(dist, 0, TotalLength);

            float accumulated = 0;
            for (int i = 0; i < segmentLengths.Length; i++)
            {
                if (accumulated + segmentLengths[i] >= dist)
                {
                    float localT = (dist - accumulated) / segmentLengths[i];
                    return GetCatmullRomPoint(i, localT);
                }
                accumulated += segmentLengths[i];
            }
            return GetCatmullRomPoint(segmentLengths.Length - 1, 1f);
        }

        /// <summary>Get the forward direction at a distance along the spline.</summary>
        public Vector3 GetDirectionAtDistance(float dist)
        {
            float delta = 0.5f;
            Vector3 a = GetPointAtDistance(dist - delta);
            Vector3 b = GetPointAtDistance(dist + delta);
            return (b - a).normalized;
        }

        /// <summary>Find the nearest point on the spline to a world position.</summary>
        public float GetNearestDistance(Vector3 worldPos)
        {
            float bestDist = float.MaxValue;
            float bestT = 0;
            int samples = Mathf.CeilToInt(TotalLength / 2f);

            for (int i = 0; i < samples; i++)
            {
                float d = (i / (float)samples) * TotalLength;
                Vector3 p = GetPointAtDistance(d);
                float sqDist = (p - worldPos).sqrMagnitude;
                if (sqDist < bestDist) { bestDist = sqDist; bestT = d; }
            }
            return bestT;
        }

        Vector3 GetCatmullRomPoint(int segIdx, float t)
        {
            int count = waypoints.Count;
            Vector3 p0 = waypoints[WrapIndex(segIdx - 1, count)];
            Vector3 p1 = waypoints[WrapIndex(segIdx, count)];
            Vector3 p2 = waypoints[WrapIndex(segIdx + 1, count)];
            Vector3 p3 = waypoints[WrapIndex(segIdx + 2, count)];
            return CatmullRom(p0, p1, p2, p3, t);
        }

        int WrapIndex(int i, int count)
        {
            if (isClosed) return ((i % count) + count) % count;
            return Mathf.Clamp(i, 0, count - 1);
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        void OnDrawGizmos()
        {
            if (waypoints.Count < 2) return;
            Gizmos.color = gizmoColor;
            int segCount = isClosed ? waypoints.Count : waypoints.Count - 1;
            for (int i = 0; i < segCount; i++)
            {
                Vector3 prev = GetCatmullRomPoint(i, 0);
                for (int s = 1; s <= gizmoResolution; s++)
                {
                    Vector3 cur = GetCatmullRomPoint(i, s / (float)gizmoResolution);
                    Gizmos.DrawLine(prev, cur);
                    prev = cur;
                }
            }
            // Waypoint spheres
            Gizmos.color = Color.red;
            foreach (var wp in waypoints) Gizmos.DrawSphere(wp, 1f);
        }
    }
}
