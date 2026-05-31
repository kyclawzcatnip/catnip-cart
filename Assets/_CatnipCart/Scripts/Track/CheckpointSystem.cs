using UnityEngine;
using System.Collections.Generic;

namespace CatnipCart.Track
{
    /// <summary>
    /// Checkpoint system for tracking racer progress, lap counting, and positions.
    /// Uses a forgiving checkpoint system that allows skipping up to 1 checkpoint
    /// and requires visiting enough checkpoints before counting a lap.
    /// </summary>
    public class CheckpointSystem : MonoBehaviour
    {
        [Header("Setup")]
        public TrackSpline spline;
        public int checkpointCount = 16;
        public int totalLaps = 3;
        public float checkpointWidth = 18f;
        public float checkpointHeight = 8f;

        // How many checkpoints can be skipped and still count
        private const int MAX_SKIP = 1;
        // Minimum fraction of checkpoints that must be hit before a lap counts
        private const float MIN_CHECKPOINT_FRACTION = 0.5f;

        // Racer tracking
        public class RacerProgress
        {
            public Transform racer;
            public int currentCheckpoint;
            public int currentLap;
            public float distanceAlongTrack;
            public bool finished;
            public float finishTime;
            public int position; // 1st, 2nd, etc.
            public int checkpointsHitThisLap; // How many unique checkpoints hit this lap
            public HashSet<int> visitedThisLap = new HashSet<int>(); // Track which ones

            public float TotalProgress => (currentLap * 1000f) + currentCheckpoint + (distanceAlongTrack / 1000f);
        }

        public List<RacerProgress> racers = new List<RacerProgress>();
        private List<Vector3> checkpointPositions = new List<Vector3>();
        private List<Vector3> checkpointForwards = new List<Vector3>();

        public System.Action<RacerProgress> OnLapComplete;
        public System.Action<RacerProgress> OnRaceFinish;

        void Start()
        {
            GenerateCheckpoints();
        }

        void GenerateCheckpoints()
        {
            checkpointPositions.Clear();
            checkpointForwards.Clear();

            for (int i = 0; i < checkpointCount; i++)
            {
                float dist = (i / (float)checkpointCount) * spline.TotalLength;
                Vector3 pos = spline.GetPointAtDistance(dist);
                Vector3 fwd = spline.GetDirectionAtDistance(dist);

                checkpointPositions.Add(pos);
                checkpointForwards.Add(fwd);

                // Create trigger collider — thicker depth (4m) so fast karts can't skip through
                var cpGO = new GameObject($"Checkpoint_{i}");
                cpGO.transform.SetParent(transform, false);
                cpGO.transform.position = pos + Vector3.up * checkpointHeight * 0.5f;
                cpGO.transform.rotation = Quaternion.LookRotation(fwd);
                cpGO.layer = 2; // Ignore Raycast

                var box = cpGO.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(checkpointWidth, checkpointHeight, 4f);

                var handler = cpGO.AddComponent<CheckpointTrigger>();
                handler.system = this;
                handler.checkpointIndex = i;
            }
        }

        public void RegisterRacer(Transform racer)
        {
            // Start at checkpoint (checkpointCount - 1) so that the FIRST
            // checkpoint they naturally hit (checkpoint 0 or 1) is accepted.
            racers.Add(new RacerProgress
            {
                racer = racer,
                currentCheckpoint = checkpointCount - 1,
                currentLap = 0,
                distanceAlongTrack = 0,
                finished = false,
                checkpointsHitThisLap = 0
            });
        }

        /// <summary>
        /// Calculate how far ahead cpIndex is from currentCheckpoint in the circular sequence.
        /// Returns 0 if same, 1 if next, 2 if two ahead, etc.
        /// </summary>
        int CircularDistance(int from, int to)
        {
            return ((to - from) % checkpointCount + checkpointCount) % checkpointCount;
        }

        public void OnCheckpointHit(Transform racer, int cpIndex)
        {
            var progress = racers.Find(r => r.racer == racer);
            if (progress == null || progress.finished) return;

            int expected = (progress.currentCheckpoint + 1) % checkpointCount;
            int dist = CircularDistance(progress.currentCheckpoint, cpIndex);

            // Accept the checkpoint if it's within skip tolerance (1 = next, 2 = skipped one)
            // dist == 0 means re-hitting current checkpoint — ignore
            if (dist >= 1 && dist <= (1 + MAX_SKIP))
            {
                progress.currentCheckpoint = cpIndex;

                // Track unique checkpoint visits this lap
                if (!progress.visitedThisLap.Contains(cpIndex))
                {
                    progress.visitedThisLap.Add(cpIndex);
                    progress.checkpointsHitThisLap++;
                }

                // Crossed start/finish line (checkpoint 0)?
                if (cpIndex == 0)
                {
                    int minRequired = Mathf.CeilToInt(checkpointCount * MIN_CHECKPOINT_FRACTION);

                    // Only count the lap if we've actually gone around the track
                    if (progress.checkpointsHitThisLap >= minRequired)
                    {
                        progress.currentLap++;
                        Debug.Log($"[Lap] {racer.name} completed lap {progress.currentLap}/{totalLaps} " +
                                  $"(checkpoints hit: {progress.checkpointsHitThisLap}/{checkpointCount})");
                        OnLapComplete?.Invoke(progress);

                        if (progress.currentLap >= totalLaps)
                        {
                            progress.finished = true;
                            progress.finishTime = Time.time;
                            OnRaceFinish?.Invoke(progress);
                        }
                    }

                    // Reset checkpoint tracking for the new lap
                    progress.visitedThisLap.Clear();
                    progress.checkpointsHitThisLap = 0;
                }
            }
        }

        void Update()
        {
            // Update distance along track for each racer
            foreach (var r in racers)
            {
                if (r.racer != null)
                    r.distanceAlongTrack = spline.GetNearestDistance(r.racer.position);
            }

            // Calculate positions
            racers.Sort((a, b) => b.TotalProgress.CompareTo(a.TotalProgress));
            for (int i = 0; i < racers.Count; i++)
                racers[i].position = i + 1;
        }

        public RacerProgress GetProgress(Transform racer)
        {
            return racers.Find(r => r.racer == racer);
        }
    }

    /// <summary>Trigger handler for individual checkpoints.</summary>
    public class CheckpointTrigger : MonoBehaviour
    {
        [HideInInspector] public CheckpointSystem system;
        [HideInInspector] public int checkpointIndex;
        private HashSet<int> recentHits = new HashSet<int>();

        void OnTriggerEnter(Collider other)
        {
            HandleTrigger(other);
        }

        // Also use OnTriggerStay as a fallback — if a kart was already
        // overlapping when the collider was created, OnTriggerEnter won't fire.
        void OnTriggerStay(Collider other)
        {
            HandleTrigger(other);
        }

        void HandleTrigger(Collider other)
        {
            var kart = other.GetComponentInParent<Kart.KartController>();
            if (kart != null)
            {
                int kartId = kart.GetInstanceID();
                if (!recentHits.Contains(kartId))
                {
                    recentHits.Add(kartId);
                    system.OnCheckpointHit(kart.transform, checkpointIndex);
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            var kart = other.GetComponentInParent<Kart.KartController>();
            if (kart != null)
            {
                recentHits.Remove(kart.GetInstanceID());
            }
        }
    }
}
