using UnityEngine;
using CatnipCart.Core;
using CatnipCart.Kart;

namespace CatnipCart.Items
{
    /// <summary>
    /// Hairball trap — drops on ground, spins out and entangles karts that drive over it.
    /// Spin out for 1s + entangled (40% slow) for 3s.
    /// </summary>
    public class HairballTrap : MonoBehaviour
    {
        public float lifetime = 30f;
        private float timer;

        void Start()
        {
            // Visual — dark fuzzy-looking sphere
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "HairballVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * 0.5f;
            Destroy(visual.GetComponent<Collider>());

            var mat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.Hairball(), 0.1f);
            visual.GetComponent<Renderer>().material = mat;

            // Add some "fur" spikes
            for (int i = 0; i < 8; i++)
            {
                var spike = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                spike.name = "Fur";
                spike.transform.SetParent(visual.transform, false);
                spike.transform.localScale = new Vector3(0.15f, 0.3f, 0.15f);
                spike.transform.localPosition = Random.onUnitSphere * 0.3f;
                spike.transform.localRotation = Random.rotation;
                spike.GetComponent<Renderer>().material = mat;
                Destroy(spike.GetComponent<Collider>());
            }

            // Trigger collider
            var col = gameObject.AddComponent<SphereCollider>();
            col.radius = 0.6f;
            col.isTrigger = true;

            // Sit on ground
            transform.position = new Vector3(transform.position.x, 0.3f, transform.position.z);
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime) Destroy(gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            var kart = other.GetComponentInParent<KartController>();
            if (kart != null)
            {
                // Spin out + entangle combo
                kart.HairballHit();
                Destroy(gameObject);
            }
        }
    }
}
