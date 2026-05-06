using UnityEngine;
using CatnipCart.Kart;

namespace CatnipCart.Track
{
    /// <summary>
    /// Boost pad on the track. Gives a speed boost when driven over.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class BoostPad : MonoBehaviour
    {
        public float boostForce = 12f;
        public float boostDuration = 1f;

        [Header("Visuals")]
        public Color padColor = new Color(0.2f, 0.8f, 1f);
        private Material padMat;
        private float pulseTimer;

        void Start()
        {
            GetComponent<BoxCollider>().isTrigger = true;

            // Create visual
            padMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            padMat.color = padColor;
            padMat.SetFloat("_Smoothness", 0.8f);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = "BoostVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = new Vector3(0, 0.02f, 0);
            visual.transform.localRotation = Quaternion.Euler(90, 0, 0);
            visual.transform.localScale = new Vector3(3f, 5f, 1f);
            visual.GetComponent<Renderer>().material = padMat;
            Destroy(visual.GetComponent<Collider>());
        }

        void Update()
        {
            // Pulsing glow
            pulseTimer += Time.deltaTime;
            float pulse = 0.7f + Mathf.Sin(pulseTimer * 4f) * 0.3f;
            padMat.SetColor("_EmissionColor", padColor * pulse * 2f);
        }

        void OnTriggerEnter(Collider other)
        {
            var kart = other.GetComponentInParent<KartController>();
            if (kart != null)
            {
                kart.ApplyBoost(boostForce, boostDuration);
            }
        }
    }
}
