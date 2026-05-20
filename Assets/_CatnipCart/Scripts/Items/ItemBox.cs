using UnityEngine;
using CatnipCart.Core;
using CatnipCart.Kart;

namespace CatnipCart.Items
{
    /// <summary>
    /// Floating item box that gives a random item when driven through.
    /// Respawns after a delay.
    /// </summary>
    public class ItemBox : MonoBehaviour
    {
        public float respawnTime = 5f;
        public float bobSpeed = 2f;
        public float bobHeight = 0.3f;
        public float rotateSpeed = 90f;

        private bool isActive = true;
        private float respawnTimer;
        private Vector3 startPos;
        private GameObject visual;

        void Start()
        {
            startPos = transform.position;
            BuildVisual();
            GetComponent<Collider>().isTrigger = true;
        }

        void BuildVisual()
        {
            visual = new GameObject("ItemBoxVisual");
            visual.transform.SetParent(transform, false);

            // Main cube
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Cube";
            cube.transform.SetParent(visual.transform, false);
            cube.transform.localScale = Vector3.one * 1.2f;
            Destroy(cube.GetComponent<Collider>());

            var mat = ProceduralTextureLib.MakeLitMaterial(
                ProceduralTextureLib.ItemBoxGold(), 0.7f, 0.3f,
                new Color(1f, 0.85f, 0f) * 0.5f);
            cube.GetComponent<Renderer>().material = mat;

            // Question mark sphere on each face
            for (int i = 0; i < 6; i++)
            {
                var qmark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                qmark.name = "QMark";
                qmark.transform.SetParent(visual.transform, false);
                Destroy(qmark.GetComponent<Collider>());

                Vector3 dir = i switch
                {
                    0 => Vector3.forward, 1 => Vector3.back,
                    2 => Vector3.left, 3 => Vector3.right,
                    4 => Vector3.up, _ => Vector3.down
                };
                qmark.transform.localPosition = dir * 0.55f;
                qmark.transform.localScale = Vector3.one * 0.25f;

                var qMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                qMat.color = new Color(0.9f, 0.2f, 0.2f);
                qmark.GetComponent<Renderer>().material = qMat;
            }
        }

        void Update()
        {
            if (!isActive)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0)
                {
                    isActive = true;
                    visual.SetActive(true);
                    GetComponent<Collider>().enabled = true;
                }
                return;
            }

            // Bob and rotate
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = startPos + Vector3.up * bob;
            visual.transform.Rotate(0, rotateSpeed * Time.deltaTime, 0, Space.Self);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;
            var kart = other.GetComponentInParent<KartController>();
            if (kart == null) return;

            // Give item to kart's ItemHolder
            var holder = kart.GetComponent<ItemHolder>();
            if (holder != null && !holder.HasItem)
            {
                holder.GiveRandomItem();
            }

            // Deactivate
            isActive = false;
            respawnTimer = respawnTime;
            visual.SetActive(false);
            GetComponent<Collider>().enabled = false;
        }
    }
}
