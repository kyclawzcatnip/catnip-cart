using UnityEngine;

namespace CatnipCart.Kart
{
    /// <summary>
    /// Visual effects for the kart: wheel spin, body tilt, drift sparks, exhaust, landing squash.
    /// Attach to the kart root and assign references.
    /// </summary>
    public class KartVisuals : MonoBehaviour
    {
        [Header("References")]
        public KartController kart;
        public Transform kartBody;
        public Transform[] wheels = new Transform[4]; // FL, FR, RL, RR
        public float wheelRadius = 0.3f;

        [Header("Drift Particles")]
        public ParticleSystem leftDriftSpark;
        public ParticleSystem rightDriftSpark;
        public TrailRenderer leftDriftTrail;
        public TrailRenderer rightDriftTrail;
        public ParticleSystem leftDriftSmoke;
        public ParticleSystem rightDriftSmoke;

        [Header("Exhaust")]
        public ParticleSystem exhaustParticle;

        [Header("Speed Lines")]
        public ParticleSystem speedLines;

        [Header("Tilt Settings")]
        public float maxTiltAngle = 15f;
        public float tiltSpeed = 8f;

        [Header("Squash & Stretch")]
        public float landSquashAmount = 0.15f;
        public float squashRecoverySpeed = 8f;

        // Drift spark colors per stage
        private readonly Color[] sparkColors = new Color[]
        {
            new Color(0.3f, 0.5f, 1f),   // Blue - stage 1
            new Color(1f, 0.6f, 0.1f),    // Orange - stage 2
            new Color(0.8f, 0.2f, 1f)     // Purple - stage 3
        };

        private float currentTilt;
        private float squashFactor = 1f;
        private int lastDriftStage;

        void Start()
        {
            if (kart == null) kart = GetComponentInParent<KartController>();

            kart.OnDriftStart += () => EnableDriftEffects(true);
            kart.OnDriftEnd += () => EnableDriftEffects(false);
            kart.OnDriftStageChange += OnDriftStage;
            kart.OnBoostStart += OnBoost;
            kart.OnLand += OnLand;
            kart.OnSpinOut += () => EnableDriftEffects(false);

            EnableDriftEffects(false);
        }

        void Update()
        {
            UpdateWheelSpin();
            UpdateBodyTilt();
            UpdateSquash();
            UpdateExhaust();
            UpdateSpeedLines();
        }

        void UpdateWheelSpin()
        {
            float spinAngle = kart.CurrentSpeed / wheelRadius * Time.deltaTime * Mathf.Rad2Deg;
            foreach (var wheel in wheels)
            {
                if (wheel != null)
                    wheel.Rotate(spinAngle, 0f, 0f, Space.Self);
            }

            // Front wheel steering visual
            float steerAngle = 0f;
            if (kart.CurrentState == KartController.KartState.Drifting)
            {
                // During drift, show counter-steer — wheels point opposite to drift direction
                float counterSteer = -kart.DriftDirection * 25f;
                // Player input modulates it
                float playerInput = kart.NormalizedSpeed * 15f;
                steerAngle = Mathf.Lerp(counterSteer, counterSteer + playerInput, 0.5f);
            }
            else if (kart.CurrentState == KartController.KartState.Normal)
            {
                steerAngle = kart.NormalizedSpeed * 25f;
            }
            if (wheels[0] != null) wheels[0].localEulerAngles = new Vector3(
                wheels[0].localEulerAngles.x, steerAngle, 0f);
            if (wheels[1] != null) wheels[1].localEulerAngles = new Vector3(
                wheels[1].localEulerAngles.x, steerAngle, 0f);
        }

        void UpdateBodyTilt()
        {
            if (kartBody == null) return;

            float targetTilt = 0f;
            if (kart.CurrentState == KartController.KartState.Drifting)
            {
                // Tilt based on actual drift angle — feels connected to the physics
                float normalizedDriftAngle = kart.DriftAngle / kart.stats.maxDriftAngle;
                targetTilt = -normalizedDriftAngle * maxTiltAngle * 1.5f;
            }

            currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSpeed * Time.deltaTime);
            kartBody.localRotation = Quaternion.Euler(0f, 0f, currentTilt);
        }

        void UpdateSquash()
        {
            squashFactor = Mathf.Lerp(squashFactor, 1f, squashRecoverySpeed * Time.deltaTime);
            if (kartBody != null)
            {
                kartBody.localScale = new Vector3(
                    1f + (1f - squashFactor) * 0.3f,
                    squashFactor,
                    1f + (1f - squashFactor) * 0.3f
                );
            }
        }

        void UpdateExhaust()
        {
            if (exhaustParticle == null) return;
            var emission = exhaustParticle.emission;
            emission.rateOverTime = kart.CurrentSpeed > 1f ? kart.NormalizedSpeed * 20f : 0f;
        }

        void UpdateSpeedLines()
        {
            if (speedLines == null) return;
            var emission = speedLines.emission;
            emission.rateOverTime = kart.IsBoosting ? 50f : (kart.NormalizedSpeed > 0.8f ? 20f : 0f);
        }

        void EnableDriftEffects(bool enable)
        {
            if (leftDriftSpark) { if (enable) leftDriftSpark.Play(); else leftDriftSpark.Stop(); }
            if (rightDriftSpark) { if (enable) rightDriftSpark.Play(); else rightDriftSpark.Stop(); }
            if (leftDriftTrail) leftDriftTrail.emitting = enable;
            if (rightDriftTrail) rightDriftTrail.emitting = enable;
            if (leftDriftSmoke) { if (enable) leftDriftSmoke.Play(); else leftDriftSmoke.Stop(); }
            if (rightDriftSmoke) { if (enable) rightDriftSmoke.Play(); else rightDriftSmoke.Stop(); }
        }

        void OnDriftStage(int stage)
        {
            if (stage < 1 || stage > sparkColors.Length) return;
            Color c = sparkColors[stage - 1];
            SetSparkColor(leftDriftSpark, c);
            SetSparkColor(rightDriftSpark, c);
            if (leftDriftTrail) leftDriftTrail.startColor = c;
            if (rightDriftTrail) rightDriftTrail.startColor = c;
        }

        void SetSparkColor(ParticleSystem ps, Color c)
        {
            if (ps == null) return;
            var main = ps.main;
            main.startColor = c;
        }

        void OnBoost(float duration)
        {
            if (exhaustParticle)
            {
                var main = exhaustParticle.main;
                main.startColor = new Color(1f, 0.4f, 0f);
            }
        }

        void OnLand()
        {
            squashFactor = 1f - landSquashAmount;
        }
    }
}
