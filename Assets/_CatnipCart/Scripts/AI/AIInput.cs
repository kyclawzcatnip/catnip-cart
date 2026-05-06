using UnityEngine;
using CatnipCart.Kart;
using CatnipCart.Track;

namespace CatnipCart.AI
{
    /// <summary>
    /// AI input that follows the track spline. Implements IKartInput
    /// so it can drive any kartController exactly like player input.
    /// </summary>
    public class AIInput : MonoBehaviour, IKartInput
    {
        [Header("References")]
        public TrackSpline spline;
        public KartController kart;

        [Header("Difficulty")]
        [Range(5f, 30f)] public float lookAheadDistance = 15f;
        [Range(0.5f, 1f)] public float maxSpeedMultiplier = 0.9f;
        [Range(0f, 1f)] public float reactionDelay = 0.1f;

        [Header("Rubber Banding")]
        public bool enableRubberBanding = true;
        public float rubberBandSpeedBoost = 0.15f;

        // IKartInput implementation
        public float Accelerate { get; private set; }
        public float Brake { get; private set; }
        public float Steer { get; private set; }
        public bool Drift { get; private set; }
        public bool UseItem { get; private set; }
        public bool LookBack { get; private set; }

        private float currentDist;
        private float steerSmooth;

        void Update()
        {
            if (spline == null || kart == null) return;

            // Find current position on spline
            currentDist = spline.GetNearestDistance(transform.position);

            // Look ahead target
            float targetDist = currentDist + lookAheadDistance;
            Vector3 target = spline.GetPointAtDistance(targetDist);

            // Calculate steering
            Vector3 toTarget = (target - transform.position).normalized;
            toTarget.y = 0;
            Vector3 fwd = transform.forward;
            fwd.y = 0;

            float signedAngle = Vector3.SignedAngle(fwd, toTarget, Vector3.up);
            float targetSteer = Mathf.Clamp(signedAngle / 45f, -1f, 1f);

            steerSmooth = Mathf.Lerp(steerSmooth, targetSteer, (1f / Mathf.Max(reactionDelay, 0.01f)) * Time.deltaTime);
            Steer = steerSmooth;

            // Acceleration — brake for sharp turns
            float absAngle = Mathf.Abs(signedAngle);
            if (absAngle > 60f)
            {
                Accelerate = 0.3f;
                Brake = 0.5f;
            }
            else if (absAngle > 35f)
            {
                Accelerate = 0.6f;
                Brake = 0f;
            }
            else
            {
                Accelerate = 1f;
                Brake = 0f;
            }

            // Simple drift AI — drift on sharp turns
            Drift = absAngle > 40f && kart.CurrentSpeed > 10f && kart.IsGrounded;

            // Rubber banding
            if (enableRubberBanding)
            {
                var cs = FindAnyObjectByType<CheckpointSystem>();
                if (cs != null)
                {
                    var progress = cs.GetProgress(transform);
                    if (progress != null)
                    {
                        if (progress.position >= 3) Accelerate = Mathf.Min(1f, Accelerate + rubberBandSpeedBoost);
                        if (progress.position == 1 && kart.CurrentSpeed > kart.stats.maxSpeed * 0.85f) Accelerate *= 0.85f;
                    }
                }
            }

            UseItem = false;
            LookBack = false;
        }
    }
}
