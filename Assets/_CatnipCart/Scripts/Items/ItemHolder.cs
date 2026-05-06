using UnityEngine;
using CatnipCart.Kart;
using CatnipCart.Track;

namespace CatnipCart.Items
{
    /// <summary>
    /// Manages the item a kart is holding. Handles item roulette,
    /// firing/dropping items, and item weighting by race position.
    /// </summary>
    public class ItemHolder : MonoBehaviour
    {
        public enum ItemType { None, YarnBall, Hairball, CatnipBoost, LaserPointer, GoldenCatnip }

        public ItemType CurrentItem { get; private set; } = ItemType.None;
        public bool HasItem => CurrentItem != ItemType.None;
        public bool IsRoulette { get; private set; }

        [Header("Prefab References")]
        public GameObject yarnBallPrefab;
        public GameObject hairballPrefab;

        private KartController kart;
        private float rouletteTimer;
        private float rouletteDuration = 2f;
        private int rouletteDisplay;

        public System.Action<ItemType> OnItemReceived;
        public System.Action OnItemUsed;
        public System.Action<int> OnRouletteUpdate; // cycling index

        void Start()
        {
            kart = GetComponent<KartController>();
        }

        void Update()
        {
            if (IsRoulette)
            {
                rouletteTimer -= Time.deltaTime;
                rouletteDisplay = (rouletteDisplay + 1) % 5;
                OnRouletteUpdate?.Invoke(rouletteDisplay);

                if (rouletteTimer <= 0)
                {
                    IsRoulette = false;
                    OnItemReceived?.Invoke(CurrentItem);
                }
            }
        }

        public void GiveRandomItem()
        {
            if (HasItem) return;

            // Weight items by position (last place gets better items)
            int position = 4; // default
            var cs = FindAnyObjectByType<CheckpointSystem>();
            if (cs != null)
            {
                var progress = cs.GetProgress(transform);
                if (progress != null) position = progress.position;
            }

            CurrentItem = RollItem(position);
            IsRoulette = true;
            rouletteTimer = rouletteDuration;
        }

        ItemType RollItem(int position)
        {
            float r = Random.value;

            // Position-weighted probabilities
            if (position <= 1) // 1st place — mostly defensive
            {
                if (r < 0.4f) return ItemType.Hairball;
                if (r < 0.7f) return ItemType.YarnBall;
                return ItemType.CatnipBoost;
            }
            else if (position == 2)
            {
                if (r < 0.3f) return ItemType.YarnBall;
                if (r < 0.5f) return ItemType.CatnipBoost;
                if (r < 0.7f) return ItemType.Hairball;
                if (r < 0.9f) return ItemType.LaserPointer;
                return ItemType.GoldenCatnip;
            }
            else // 3rd, 4th — powerful items
            {
                if (r < 0.25f) return ItemType.GoldenCatnip;
                if (r < 0.45f) return ItemType.LaserPointer;
                if (r < 0.65f) return ItemType.CatnipBoost;
                if (r < 0.85f) return ItemType.YarnBall;
                return ItemType.Hairball;
            }
        }

        public void UseItem()
        {
            if (!HasItem || IsRoulette) return;

            switch (CurrentItem)
            {
                case ItemType.YarnBall:
                    FireYarnBall();
                    break;
                case ItemType.Hairball:
                    DropHairball();
                    break;
                case ItemType.CatnipBoost:
                    kart.ApplyBoost(kart.stats.boostForce, kart.stats.boostDuration);
                    break;
                case ItemType.LaserPointer:
                    ActivateLaserPointer();
                    break;
                case ItemType.GoldenCatnip:
                    ActivateGoldenCatnip();
                    break;
            }

            CurrentItem = ItemType.None;
            OnItemUsed?.Invoke();
        }

        void FireYarnBall()
        {
            var go = new GameObject("YarnBall");
            go.transform.position = transform.position + transform.forward * 2f + Vector3.up * 0.5f;
            var proj = go.AddComponent<YarnBallProjectile>();
            proj.direction = transform.forward;
            proj.owner = transform;
        }

        void DropHairball()
        {
            var go = new GameObject("Hairball");
            go.transform.position = transform.position - transform.forward * 2f;
            go.AddComponent<HairballTrap>();
        }

        void ActivateLaserPointer()
        {
            // Shrink & slow all other karts temporarily
            var allKarts = FindObjectsByType<KartController>(FindObjectsSortMode.None);
            foreach (var k in allKarts)
            {
                if (k.transform == transform) continue;
                k.transform.localScale = Vector3.one * 0.5f;
                k.Entangle(4f); // Slowed
                // Reset scale after 4 seconds
                StartCoroutine(ResetScale(k.transform, 4f));
            }
        }

        System.Collections.IEnumerator ResetScale(Transform t, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (t != null) t.localScale = Vector3.one;
        }

        void ActivateGoldenCatnip()
        {
            // Invincibility + boost for 8 seconds
            kart.ApplyBoost(kart.stats.boostForce * 1.5f, 8f);
            // Golden glow effect
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.material.SetColor("_EmissionColor", new Color(1f, 0.85f, 0f) * 3f);
            }
            StartCoroutine(RemoveGlow(renderers, 8f));
        }

        System.Collections.IEnumerator RemoveGlow(Renderer[] renderers, float delay)
        {
            yield return new WaitForSeconds(delay);
            foreach (var r in renderers)
            {
                if (r != null) r.material.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}
