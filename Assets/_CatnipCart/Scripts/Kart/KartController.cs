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
                    UpdateDriving(); break;
                case KartState.Drifting:
                    UpdateDrifting(); break;
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
            if (input.Drift && IsGrounded && Mathf.Abs(currentSteerInput) > 0.3f && CurrentSpeed > 5f) { StartDrift(); return; }
            float eMult = IsEntangled ? 0.6f : 1f;
            float maxSpd = stats.maxSpeed * eMult + (boostTimer > 0 ? boostForce : 0);
            if (input.Accelerate > 0.1f && CurrentSpeed < maxSpd) rb.AddForce(transform.forward * stats.acceleration * input.Accelerate, ForceMode.Acceleration);
            else if (CurrentSpeed > 0.5f) rb.AddForce(-transform.forward * stats.coastDeceleration, ForceMode.Acceleration);
            if (input.Brake > 0.1f) { if (CurrentSpeed > 0) rb.AddForce(-transform.forward * stats.brakeForce * input.Brake, ForceMode.Acceleration); else if (CurrentSpeed > -stats.maxReverseSpeed) rb.AddForce(-transform.forward * stats.acceleration * 0.5f * input.Brake, ForceMode.Acceleration); }
            if (IsGrounded && Mathf.Abs(CurrentSpeed) > 1f) { float s = currentSteerInput * stats.turnSpeed * Time.fixedDeltaTime * (CurrentSpeed < 0 ? -1 : 1); transform.Rotate(0, s, 0, Space.Self); }
            ApplyLateralFriction(stats.lateralGrip);
            ClampSpeed(maxSpd);

            // Drift exit inertia — carry residual angular momentum after ending a drift
            if (driftExitTimer > 0f)
            {
                driftExitTimer -= Time.fixedDeltaTime;
                float inertiaDecay = driftExitTimer / 0.4f; // 0.4s total exit slide
                transform.Rotate(0, driftExitAngularVelocity * inertiaDecay * Time.fixedDeltaTime, 0, Space.Self);
            }
        }

        void StartDrift()
        {
            CurrentState = KartState.Drifting;
            DriftDirection = currentSteerInput > 0 ? 1 : -1;
            DriftTime = 0; DriftStage = 0;
            driftAngleCurrent = 0f;

            // Entry kick: apply a lateral impulse to break rear traction
            // This is the "snap" that makes drift entry feel punchy — like yanking the handbrake
            float speedFactor = Mathf.Clamp01(CurrentSpeed / stats.maxSpeed);
            Vector3 kickDir = transform.right * DriftDirection;
            rb.AddForce(kickDir * stats.driftEntryKickForce * speedFactor, ForceMode.VelocityChange);

            OnDriftStart?.Invoke();
        }

        void UpdateDrifting()
        {
            // Exit conditions: release drift, leave ground, or speed too low
            if (!input.Drift || !IsGrounded || CurrentSpeed < stats.minDriftSpeed) { EndDrift(); return; }

            DriftTime += Time.fixedDeltaTime;

            // --- Mini-turbo stage progression ---
            int ns = 0;
            for (int i = stats.miniTurboThresholds.Length - 1; i >= 0; i--)
                if (DriftTime >= stats.miniTurboThresholds[i]) { ns = i + 1; break; }
            if (ns != DriftStage) { DriftStage = ns; OnDriftStageChange?.Invoke(DriftStage); }

            // --- Drift angle: build up the slip angle over time ---
            // The drift angle represents how far sideways the rear end has swung out.
            // Counter-steering reduces it, same-direction steering increases it.
            float counterSteerEffect = 0f;
            if (DriftDirection > 0 && input.Steer < -0.1f)
                counterSteerEffect = input.Steer * stats.driftCounterSteerSensitivity;
            else if (DriftDirection < 0 && input.Steer > 0.1f)
                counterSteerEffect = input.Steer * stats.driftCounterSteerSensitivity;

            float targetAngle = stats.maxDriftAngle * DriftDirection;
            // Counter-steer pulls the target angle back toward center
            targetAngle += counterSteerEffect * stats.maxDriftAngle;

            driftAngleCurrent = Mathf.MoveTowards(driftAngleCurrent, targetAngle,
                stats.driftAngleBuildSpeed * stats.maxDriftAngle * Time.fixedDeltaTime);
            DriftAngle = driftAngleCurrent;

            // --- Rotation: base turn + drift angle contribution ---
            // The kart rotates from the locked drift direction plus player modulation
            float baseTurn = DriftDirection * stats.turnSpeed * stats.driftTurnMultiplier;
            float playerModulation = input.Steer * stats.turnSpeed * 0.5f;
            float driftAngleSteer = (driftAngleCurrent / stats.maxDriftAngle) * stats.turnSpeed * 0.3f;
            float totalRotation = (baseTurn + playerModulation + driftAngleSteer) * Time.fixedDeltaTime;
            transform.Rotate(0, totalRotation, 0, Space.Self);

            // --- Lateral velocity: simulate the rear end sliding ---
            // Instead of just reducing grip, we actively inject lateral velocity
            // proportional to the drift angle. This creates the actual sideways slide.
            float normalizedAngle = Mathf.Abs(driftAngleCurrent) / stats.maxDriftAngle;
            float slipSpeed = CurrentSpeed * stats.driftRearSlipFactor * normalizedAngle;
            Vector3 desiredLateral = transform.right * DriftDirection * slipSpeed;
            Vector3 currentLateral = Vector3.Dot(rb.linearVelocity, transform.right) * transform.right;
            Vector3 lateralCorrection = (desiredLateral - currentLateral) * stats.driftStiffness;
            rb.AddForce(lateralCorrection, ForceMode.Acceleration);
            DriftSlipVelocity = Vector3.Dot(rb.linearVelocity, transform.right);

            // --- Forward acceleration (reduced during drift — tire scrub) ---
            float eMult = IsEntangled ? 0.6f : 1f;
            float maxSpd = stats.maxSpeed * eMult + (boostTimer > 0 ? boostForce : 0);
            // Acceleration is reduced proportionally to how sideways we are
            float accelReduction = 1f - (normalizedAngle * 0.35f);
            if (input.Accelerate > 0.1f && CurrentSpeed < maxSpd)
                rb.AddForce(transform.forward * stats.acceleration * input.Accelerate * accelReduction * 0.8f, ForceMode.Acceleration);

            // --- Tire scrub deceleration: drifting costs speed ---
            // The more sideways the kart, the more speed is lost to friction
            rb.AddForce(-transform.forward * stats.driftDeceleration * normalizedAngle, ForceMode.Acceleration);

            // --- Partial lateral grip (not zero — keeps the drift controllable) ---
            // Use reduced grip based on stiffness, but don't fully cancel lateral velocity
            float driftGrip = stats.lateralGrip * stats.driftStiffness * (1f - normalizedAngle * 0.5f);
            ApplyLateralFriction(driftGrip);

            ClampSpeed(maxSpd);
        }

        void EndDrift()
        {
            // Apply mini-turbo boost based on drift stage
            if (DriftStage > 0 && DriftStage <= stats.miniTurboForces.Length)
                ApplyBoost(stats.miniTurboForces[DriftStage - 1], stats.miniTurboDurations[DriftStage - 1]);

            // Angular inertia: carry residual rotation after drift ends
            // This prevents the jarring instant-straighten and makes exits feel smooth
            float angularVelocity = driftAngleCurrent * stats.driftAngularInertia * 2f;
            driftExitAngularVelocity = angularVelocity;
            driftExitTimer = 0.4f;

            // Blend lateral velocity back toward zero smoothly
            // (the remaining lateral vel will be eaten by normal-mode grip over ~0.3s)

            CurrentState = KartState.Normal;
            DriftStage = 0; DriftTime = 0; DriftDirection = 0;
            DriftAngle = 0; DriftSlipVelocity = 0;
            driftAngleCurrent = 0f;
            OnDriftEnd?.Invoke();
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
            if (CurrentState == KartState.Drifting) EndDrift();
            if (CurrentState == KartState.Normal) CurrentState = KartState.Boosting;
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
