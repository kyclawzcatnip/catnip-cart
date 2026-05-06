using UnityEngine;
using CatnipCart.Kart;
using CatnipCart.Items;
using CatnipCart.Track;

namespace CatnipCart.AI
{
    /// <summary>
    /// AI logic for using items. Uses items with randomized delay and
    /// basic tactical decisions.
    /// </summary>
    [RequireComponent(typeof(ItemHolder))]
    public class AIItemUser : MonoBehaviour
    {
        public float minUseDelay = 0.5f;
        public float maxUseDelay = 3f;

        private ItemHolder holder;
        private KartController kart;
        private float useTimer;
        private bool waitingToUse;

        void Start()
        {
            holder = GetComponent<ItemHolder>();
            kart = GetComponent<KartController>();

            holder.OnItemReceived += OnGotItem;
        }

        void OnGotItem(ItemHolder.ItemType item)
        {
            waitingToUse = true;
            useTimer = Random.Range(minUseDelay, maxUseDelay);
        }

        void Update()
        {
            if (!waitingToUse || !holder.HasItem || holder.IsRoulette) return;

            useTimer -= Time.deltaTime;
            if (useTimer <= 0)
            {
                // Tactical decision
                bool shouldUse = true;

                switch (holder.CurrentItem)
                {
                    case ItemHolder.ItemType.YarnBall:
                        // Only fire if a kart is ahead and relatively close
                        shouldUse = IsKartAhead(30f);
                        break;
                    case ItemHolder.ItemType.Hairball:
                        // Drop if a kart is behind
                        shouldUse = IsKartBehind(20f);
                        break;
                    case ItemHolder.ItemType.CatnipBoost:
                    case ItemHolder.ItemType.GoldenCatnip:
                        // Use immediately
                        shouldUse = true;
                        break;
                    case ItemHolder.ItemType.LaserPointer:
                        // Use when not in 1st
                        var cs = FindAnyObjectByType<CheckpointSystem>();
                        if (cs != null)
                        {
                            var p = cs.GetProgress(transform);
                            shouldUse = p != null && p.position > 1;
                        }
                        break;
                }

                if (shouldUse)
                {
                    holder.UseItem();
                    waitingToUse = false;
                }
                else
                {
                    useTimer = 1f; // Re-check in 1 second
                }
            }
        }

        bool IsKartAhead(float range)
        {
            var karts = FindObjectsByType<KartController>(FindObjectsSortMode.None);
            foreach (var k in karts)
            {
                if (k == kart) continue;
                Vector3 toKart = k.transform.position - transform.position;
                if (Vector3.Dot(toKart, transform.forward) > 0 && toKart.magnitude < range)
                    return true;
            }
            return false;
        }

        bool IsKartBehind(float range)
        {
            var karts = FindObjectsByType<KartController>(FindObjectsSortMode.None);
            foreach (var k in karts)
            {
                if (k == kart) continue;
                Vector3 toKart = k.transform.position - transform.position;
                if (Vector3.Dot(toKart, transform.forward) < 0 && toKart.magnitude < range)
                    return true;
            }
            return false;
        }
    }
}
