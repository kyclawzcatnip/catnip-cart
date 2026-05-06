using UnityEngine;
using CatnipCart.Kart;

namespace CatnipCart.Core
{
    /// <summary>
    /// Third-person camera that follows the player kart.
    /// Features: smooth follow, drift offset, boost FOV, look-back, shake on hit.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;
        public KartController kart;

        [Header("Follow")]
        public Vector3 offset = new Vector3(0f, 4f, -8f);
        public float followSpeed = 8f;
        public float rotationSpeed = 6f;

        [Header("Effects")]
        public float normalFOV = 60f;
        public float boostFOV = 75f;
        public float fovSpeed = 5f;
        public float driftOffsetX = 1.5f;

        [Header("Shake")]
        public float shakeDecay = 5f;

        private Camera cam;
        private float shakeAmount;
        private Vector3 currentOffset;

        void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = gameObject.AddComponent<Camera>();
            currentOffset = offset;

            if (kart != null)
            {
                kart.OnSpinOut += () => shakeAmount = 0.5f;
                kart.OnEntangle += () => shakeAmount = 0.3f;
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Calculate desired offset based on state
            Vector3 desiredOffset = offset;

            // Drift camera offset
            if (kart != null && kart.CurrentState == KartController.KartState.Drifting)
            {
                desiredOffset += target.right * kart.DriftDirection * driftOffsetX;
            }

            // Look back
            IKartInput input = kart != null ? kart.GetComponent<IKartInput>() as IKartInput : null;
            if (input != null && input.LookBack)
            {
                desiredOffset = new Vector3(0f, offset.y, -offset.z); // Flip Z
            }

            currentOffset = Vector3.Lerp(currentOffset, desiredOffset, followSpeed * Time.deltaTime);

            // Position
            Vector3 desiredPos = target.position + target.rotation * currentOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

            // Rotation — look at target
            Vector3 lookTarget = target.position + Vector3.up * 1.5f;
            if (input != null && input.LookBack)
                lookTarget = target.position + target.forward * -10f + Vector3.up * 1.5f;

            Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationSpeed * Time.deltaTime);

            // FOV
            float targetFOV = normalFOV;
            if (kart != null && kart.IsBoosting) targetFOV = boostFOV;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovSpeed * Time.deltaTime);

            // Shake
            if (shakeAmount > 0)
            {
                transform.position += Random.insideUnitSphere * shakeAmount;
                shakeAmount = Mathf.Max(0, shakeAmount - shakeDecay * Time.deltaTime);
            }
        }
    }
}
