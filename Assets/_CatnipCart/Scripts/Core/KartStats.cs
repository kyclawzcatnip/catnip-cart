using UnityEngine;

namespace CatnipCart.Core
{
    /// <summary>
    /// ScriptableObject defining kart performance parameters.
    /// Create different instances for each cat racer's unique handling.
    /// </summary>
    [CreateAssetMenu(fileName = "NewKartStats", menuName = "Catnip Cart/Kart Stats")]
    public class KartStats : ScriptableObject
    {
        [Header("Speed")]
        [Tooltip("Maximum forward speed in units/sec")]
        public float maxSpeed = 25f;

        [Tooltip("Acceleration force applied per second")]
        public float acceleration = 40f;

        [Tooltip("Braking deceleration force")]
        public float brakeForce = 60f;

        [Tooltip("Maximum reverse speed")]
        public float maxReverseSpeed = 10f;

        [Header("Steering")]
        [Tooltip("Base turn speed in degrees/sec")]
        public float turnSpeed = 120f;

        [Tooltip("Multiplier for turn speed while drifting")]
        public float driftTurnMultiplier = 1.6f;

        [Tooltip("How quickly the kart loses lateral velocity (higher = more grip)")]
        [Range(0.5f, 10f)]
        public float lateralGrip = 5f;

        [Header("Drift")]
        [Tooltip("How easily the kart slides sideways during drift (lower = more slide)")]
        [Range(0.1f, 1f)]
        public float driftStiffness = 0.25f;

        [Tooltip("Lateral impulse applied when entering a drift to kick the rear end out")]
        public float driftEntryKickForce = 12f;

        [Tooltip("How much the rear slides out during drift (simulates rear slip angle)")]
        [Range(0f, 1f)]
        public float driftRearSlipFactor = 0.8f;

        [Tooltip("How much the player can counter-steer to modulate drift angle (0 = none, 1 = full control)")]
        [Range(0f, 1f)]
        public float driftCounterSteerSensitivity = 0.6f;

        [Tooltip("Angular momentum carry-through when exiting drift (higher = more slide after release)")]
        [Range(0f, 1f)]
        public float driftAngularInertia = 0.35f;

        [Tooltip("Speed loss multiplier during drift from tire scrub (higher = more speed loss)")]
        [Range(0f, 15f)]
        public float driftDeceleration = 5f;

        [Tooltip("How quickly the drift angle builds up (higher = snappier entry)")]
        [Range(1f, 20f)]
        public float driftAngleBuildSpeed = 12f;

        [Tooltip("Maximum drift angle in degrees (controls how sideways the kart can get)")]
        [Range(15f, 60f)]
        public float maxDriftAngle = 50f;

        [Tooltip("Minimum speed to maintain a drift (below this the drift ends)")]
        public float minDriftSpeed = 4f;

        [Tooltip("Time thresholds for mini-turbo boost levels (blue, orange, purple)")]
        public float[] miniTurboThresholds = new float[] { 1.0f, 2.0f, 3.0f };

        [Tooltip("Boost force for each mini-turbo level")]
        public float[] miniTurboForces = new float[] { 8f, 14f, 22f };

        [Tooltip("Duration of each mini-turbo level in seconds")]
        public float[] miniTurboDurations = new float[] { 0.8f, 1.2f, 1.8f };

        [Header("Boost")]
        [Tooltip("Generic boost force (boost pads, items)")]
        public float boostForce = 15f;

        [Tooltip("Generic boost duration in seconds")]
        public float boostDuration = 1.5f;

        [Header("Physics")]
        [Tooltip("Custom gravity strength when airborne")]
        public float gravity = 30f;

        [Tooltip("Distance to raycast downward for ground detection")]
        public float groundCheckDistance = 1.2f;

        [Tooltip("How quickly the kart aligns to ground normal")]
        [Range(1f, 20f)]
        public float groundAlignSpeed = 8f;

        [Tooltip("Mass of the kart rigidbody")]
        public float mass = 1000f;

        [Header("Handling Feel")]
        [Tooltip("Speed at which steering input is applied (smoothing)")]
        [Range(1f, 20f)]
        public float steerInputSmoothing = 10f;

        [Tooltip("How much the kart decelerates when not pressing gas")]
        public float coastDeceleration = 8f;
    }
}
