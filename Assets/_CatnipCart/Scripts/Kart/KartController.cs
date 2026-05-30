using UnityEngine;
using CatnipCart.Core;

namespace CatnipCart.Kart
{
    [RequireComponent(typeof(Rigidbody))]
    public class KartController : MonoBehaviour
    {
        [Header("Configuration")]
        public KartStats stats;
        [Header("Ground Detection")]
        public Transform groundRayOrigin;
        public LayerMask groundLayer = ~0;

        public enum KartState { Normal, Drifting, Boosting, SpinOut, Falling }
        public KartState CurrentState { get; private set; } = KartState.Normal;
        public float CurrentSpeed { get; private set; }
        public float NormalizedSpeed => stats ? Mathf.Clamp01(CurrentSpeed / stats.maxSpeed) : 0f;
        public bool IsGrounded { get; private set; }
        public int DriftStage { get; private set; }
        public float DriftTime { get; private set; }
        public int DriftDirection { get; private set; }
        public float DriftAngle { get; private set; }
        public float DriftSlipVelocity { get; private set; }
        public bool IsBoosting => boostTimer > 0f;
        public bool IsEntangled => entangleTimer > 0f;

        private Rigidbody rb;
        private IKartInput input;
        private float currentSteerInput, boostTimer, boostForce, spinOutTimer, entangleTimer;
        private Vector3 groundNormal = Vector3.up;
        private bool wasGrounded;
        private float driftAngleCurrent;
        private float driftIntensity; // 0..1 smooth ramp for auto-drift blend
        private float driftExitAngularVelocity;
        private float driftExitTimer;

        public System.Action OnDriftStart, OnDriftEnd, OnSpinOut, OnLand, OnEntangle;
        public System.Action<int> OnDriftStageChange;
        public System.Action<float> OnBoostStart;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            input = GetComponent<IKartInput>() as IKartInput;
        }

        void Start()
        {
            rb.mass = stats.mass;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
        }

        void FixedUpdate()
        {
            if (input == null || stats == null) return;
            CheckGround();
            HandleState();
        }

        void CheckGround()
        {
            Vector3 origin = groundRayOrigin ? groundRayOrigin.position : transform.position;
            wasGrounded = IsGrounded;
            if (Physics.Raycast(origin, -transform.up, out RaycastHit hit, stats.groundCheckDistance, groundLayer))
            {
                IsGrounded = true;
                groundNormal = hit.normal;
                Vector3 pos = transform.position;
                pos.y = Mathf.Lerp(pos.y, hit.point.y + stats.groundCheckDistance * 0.5f, Time.fixedDeltaTime * 15f);
                transform.position = pos;
                if (rb.linearVelocity.y < 0) { var v = rb.linearVelocity; v.y = 0; rb.linearVelocity = v; }
                if (!wasGrounded) OnLand?.Invoke();
            }
            else { IsGrounded = false; groundNormal = Vector3.up; }
        }

        void HandleState()
        {
            switch (CurrentState)
            {
                case KartState.Normal:
                case KartState.Boosting:
                case KartState.Drifting: // Auto-drift is a smooth blend inside UpdateDriving
                    UpdateDriving(); break;
                case KartState.SpinOut:
                    UpdateSpinOut(); break;
                case KartState.Falling:
                    if (IsGrounded) CurrentState = KartState.Normal; break;
            }
            if (entangleTimer > 0f) entangleTimer -= Time.fixedDeltaTime;
            if (boostTimer > 0f) { boostTimer -= Time.fixedDeltaTime; if (boostTimer <= 0) { boostTimer = 0; boostForce = 0; if (CurrentState == KartState.Boosting) CurrentState = KartState.Normal; } }
            if (!IsGrounded) rb.AddForce(Vector3.down * stats.gravity, ForceMode.Acceleration);
            AlignToGround();
            CurrentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        }

        void UpdateDriving()
        {
            currentSteerInput = Mathf.MoveTowards(currentSteerInput, input.Steer, stats.steerInputSmoothing * Time.fixedDeltaTime);

            // --- Auto-drift: smoothly ramp drift intensity based on speed + steer ---
            float absSteer = Mathf.Abs(currentSteerInput);
            float speedRatio = Mathf.Clamp01(CurrentSpeed / stats.maxSpeed);
            bool wantDrift = IsGrounded && absSteer > 0.5f && speedRatio > 0.4f;
            // Also allow manual drift button as an override
            if (input.Drift && IsGrounded && absSteer > 0.2f && CurrentSpeed > stats.minDriftSpeed)
                wantDrift = true;

            float targetIntensity = wantDrift ? Mathf.Clamp01(absSteer * speedRatio * 1.4f) : 0f;
            float rampSpeed = wantDrift ? 2.5f : 4f; // Ramp in smoothly, ramp out faster
            driftIntensity = Mathf.Lerp(driftIntensity, targetIntensity, rampSpeed * Time.fixedDeltaTime);

            // Track drift direction — lock when entering, release when intensity drops
            bool isDrifting = driftIntensity > 0.1f;
            if (isDrifting && DriftDirection == 0)
            {
                DriftDirection = currentSteerInput > 0 ? 1 : -1;
                CurrentState = KartState.Drifting;
                OnDriftStart?.Invoke();
            }
            else if (!isDrifting && DriftDirection != 0)
            {
                EndDrift();
            }

            // --- Mini-turbo accumulation (only when drifting above 50% intensity) ---
            if (isDrifting && driftIntensity > 0.5f)
            {
                DriftTime += Time.fixedDeltaTime;
                int ns = 0;
                for (int i = stats.miniTurboThresholds.Length - 1; i >= 0; i--)
                    if (DriftTime >= stats.miniTurboThresholds[i]) { ns = i + 1; break; }
                if (ns != DriftStage) { DriftStage = ns; OnDriftStageChange?.Invoke(DriftStage); }
            }

            // --- Acceleration / braking ---
            float eMult = IsEntangled ? 0.6f : 1f;
            float maxSpd = stats.maxSpeed * eMult + (boostTimer > 0 ? boostForce : 0);
            float accelReduction = 1f - (driftIntensity * 0.3f); // Slight speed cost while sliding
            if (input.Accelerate > 0.1f && CurrentSpeed < maxSpd)
                rb.AddForce(transform.forward * stats.acceleration * input.Accelerate * accelReduction, ForceMode.Acceleration);
            else if (CurrentSpeed > 0.5f)
                rb.AddForce(-transform.forward * stats.coastDeceleration, ForceMode.Acceleration);
            if (input.Brake > 0.1f)
            {
                if (CurrentSpeed > 0) rb.AddForce(-transform.forward * stats.brakeForce * input.Brake, ForceMode.Acceleration);
                else if (CurrentSpeed > -stats.maxReverseSpeed) rb.AddForce(-transform.forward * stats.acceleration * 0.5f * input.Brake, ForceMode.Acceleration);
            }

            // --- Steering: blend between normal grip turn and wider drift turn ---
            if (IsGrounded && Mathf.Abs(CurrentSpeed) > 1f)
            {
                float sign = CurrentSpeed < 0 ? -1f : 1f;
                float normalTurn = currentSteerInput * stats.turnSpeed;
                float driftTurn = (DriftDirection != 0 ? DriftDirection : Mathf.Sign(currentSteerInput))
                    * stats.turnSpeed * stats.driftTurnMultiplier
                    + currentSteerInput * stats.turnSpeed * 0.4f; // Player can modulate
                float blendedTurn = Mathf.Lerp(normalTurn, driftTurn, driftIntensity);
                transform.Rotate(0, blendedTurn * sign * Time.fixedDeltaTime, 0, Space.Self);
            }

            // --- Lateral physics: blend between full grip and drift slide ---
            if (isDrifting)
            {
                // Drift angle ramps up smoothly with intensity
                float targetAngle = stats.maxDriftAngle * DriftDirection * driftIntensity;
                // Counter-steer modulation
                if ((DriftDirection > 0 && input.Steer < -0.1f) || (DriftDirection < 0 && input.Steer > 0.1f))
                    targetAngle += input.Steer * stats.driftCounterSteerSensitivity * stats.maxDriftAngle;
                driftAngleCurrent = Mathf.Lerp(driftAngleCurrent, targetAngle, stats.driftAngleBuildSpeed * Time.fixedDeltaTime);
                DriftAngle = driftAngleCurrent;

                // Lateral slide proportional to drift intensity (smooth, not snappy)
                float normalizedAngle = Mathf.Abs(driftAngleCurrent) / stats.maxDriftAngle;
                float slipSpeed = CurrentSpeed * stats.driftRearSlipFactor * normalizedAngle * driftIntensity;
                Vector3 desiredLateral = transform.right * DriftDirection * slipSpeed;
                Vector3 currentLateral = Vector3.Dot(rb.linearVelocity, transform.right) * transform.right;
                Vector3 lateralCorrection = (desiredLateral - currentLateral) * stats.lateralGrip;
                rb.AddForce(lateralCorrection, ForceMode.Acceleration);
                DriftSlipVelocity = Vector3.Dot(rb.linearVelocity, transform.right);

                // Tire scrub deceleration scaled by intensity
                rb.AddForce(-transform.forward * stats.driftDeceleration * normalizedAngle * driftIntensity, ForceMode.Acceleration);

                // Blend lateral grip: less grip = more slide
                float gripBlend = Mathf.Lerp(stats.lateralGrip, stats.lateralGrip * 0.15f, driftIntensity);
                ApplyLateralFriction(gripBlend);
            }
            else
            {
                // Full grip — normal driving
                driftAngleCurrent = Mathf.Lerp(driftAngleCurrent, 0f, 6f * Time.fixedDeltaTime);
                DriftAngle = driftAngleCurrent;
                ApplyLateralFriction(stats.lateralGrip);
            }

            ClampSpeed(maxSpd);

            // Drift exit inertia — carry residual angular momentum after ending a drift
            if (driftExitTimer > 0f)
            {
                driftExitTimer -= Time.fixedDeltaTime;
                float inertiaDecay = driftExitTimer / 0.4f;
                transform.Rotate(0, driftExitAngularVelocity * inertiaDecay * Time.fixedDeltaTime, 0, Space.Self);
            }
        }

        void EndDrift()
        {
            // Save boost params before resetting state
            int savedStage = DriftStage;

            // Angular inertia: carry residual rotation after drift ends
            // Scaled by how intense the drift was for smooth exits
            float angularVelocity = driftAngleCurrent * stats.driftAngularInertia * 1.5f;
            driftExitAngularVelocity = angularVelocity;
            driftExitTimer = 0.35f;

            // Reset state BEFORE calling ApplyBoost to prevent infinite recursion
            CurrentState = KartState.Normal;
            DriftStage = 0; DriftTime = 0; DriftDirection = 0;
            DriftSlipVelocity = 0;
            // Don't snap driftAngleCurrent to 0 — let it decay smoothly in UpdateDriving
            OnDriftEnd?.Invoke();

            // Apply mini-turbo boost based on saved drift stage
            if (savedStage > 0 && savedStage <= stats.miniTurboForces.Length)
                ApplyBoost(stats.miniTurboForces[savedStage - 1], stats.miniTurboDurations[savedStage - 1]);
        }

        void UpdateSpinOut()
        {
            spinOutTimer -= Time.fixedDeltaTime;
            transform.Rotate(0, 720 * Time.fixedDeltaTime, 0, Space.Self);
            rb.AddForce(-rb.linearVelocity * 3f, ForceMode.Acceleration);
            if (spinOutTimer <= 0) CurrentState = KartState.Normal;
        }

        void ApplyLateralFriction(float grip)
        {
            Vector3 lat = Vector3.Dot(rb.linearVelocity, transform.right) * transform.right;
            rb.AddForce(-lat * grip, ForceMode.Acceleration);
        }

        void ClampSpeed(float maxSpeed)
        {
            float fs = Vector3.Dot(rb.linearVelocity, transform.forward);
            if (fs > maxSpeed) { Vector3 lat = rb.linearVelocity - transform.forward * fs; rb.linearVelocity = transform.forward * maxSpeed + lat; }
        }

        void AlignToGround()
        {
            if (IsGrounded) { Quaternion t = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation; transform.rotation = Quaternion.Slerp(transform.rotation, t, stats.groundAlignSpeed * Time.fixedDeltaTime); }
        }

        public void ApplyBoost(float force, float duration)
        {
            boostForce = force; boostTimer = duration;

            // If drifting, reset drift state directly (do NOT call EndDrift —
            // EndDrift calls ApplyBoost for mini-turbo, causing infinite recursion).
            // The external boost replaces the mini-turbo anyway.
            if (CurrentState == KartState.Drifting)
            {
                DriftStage = 0; DriftTime = 0; DriftDirection = 0;
                DriftSlipVelocity = 0; driftIntensity = 0f;
                OnDriftEnd?.Invoke();
            }

            CurrentState = KartState.Boosting;
            rb.AddForce(transform.forward * force * 2f, ForceMode.VelocityChange);
            OnBoostStart?.Invoke(duration);
        }

        public void SpinOut(float duration = 1.5f)
        {
            if (CurrentState == KartState.SpinOut) return;
            CurrentState = KartState.SpinOut; spinOutTimer = duration;
            rb.linearVelocity *= 0.2f; OnSpinOut?.Invoke();
        }

        public void Entangle(float duration = 3f) { entangleTimer = duration; OnEntangle?.Invoke(); }
        public void HairballHit() { SpinOut(1f); Entangle(3f); }
        public void SetInput(IKartInput newInput) { input = newInput; }
    }
}
