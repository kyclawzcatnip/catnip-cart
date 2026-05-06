using UnityEngine;
using CatnipCart.Kart;

namespace CatnipCart.Items
{
    /// <summary>
    /// Yarn Ball projectile — fires forward, bounces off walls, spins out karts.
    /// </summary>
    public class YarnBallProjectile : MonoBehaviour
    {
        public Vector3 direction;
        public float speed = 30f;
        public int maxBounces = 5;
        public float lifetime = 8f;
        public Transform owner;

        private int bounceCount;
        private float timer;
        private Rigidbody rb;
        private GameObject visual;

        void Start()
        {
            // Visual — colorful sphere
            visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "YarnVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * 0.6f;
            Destroy(visual.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color[] colors = { Color.red, Color.blue, Color.green, new Color(1f, 0.5f, 0f), Color.magenta };
            mat.color = colors[Random.Range(0, colors.Length)];
            mat.SetFloat("_Smoothness", 0.6f);
            visual.GetComponent<Renderer>().material = mat;

            // Physics
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearVelocity = direction.normalized * speed;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.mass = 0.5f;

            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.3f;

            gameObject.layer = LayerMask.NameToLayer("Default");
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= lifetime) Destroy(gameObject);

            // Spin visual
            visual.transform.Rotate(300 * Time.deltaTime, 200 * Time.deltaTime, 0);
        }

        void OnCollisionEnter(Collision collision)
        {
            // Hit a kart?
            var kart = collision.gameObject.GetComponentInParent<KartController>();
            if (kart != null && kart.transform != owner)
            {
                kart.SpinOut(1.5f);
                Destroy(gameObject);
                return;
            }

            // Bounce off wall
            bounceCount++;
            if (bounceCount >= maxBounces) { Destroy(gameObject); return; }

            Vector3 normal = collision.contacts[0].normal;
            Vector3 reflected = Vector3.Reflect(rb.linearVelocity.normalized, normal);
            rb.linearVelocity = reflected * speed;
        }
    }
}
