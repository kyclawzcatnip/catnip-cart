using UnityEngine;
using System.Collections.Generic;

namespace CatnipCart.Track
{
    /// <summary>
    /// Checkpoint system for tracking racer progress, lap counting, and positions.
    /// </summary>
    public class CheckpointSystem : MonoBehaviour
    {
        [Header("Setup")]
        public TrackSpline spline;
        public int checkpointCount = 16;
        public int totalLaps = 3;
        public float checkpointWidth = 15f;
        public float checkpointHeight = 5f;

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

                // Create trigger collider
                var cpGO = new GameObject($"Checkpoint_{i}");
                cpGO.transform.SetParent(transform, false);
                cpGO.transform.position = pos + Vector3.up * checkpointHeight * 0.5f;
                cpGO.transform.rotation = Quaternion.LookRotation(fwd);
                cpGO.layer = 2; // Ignore Raycast

                var box = cpGO.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(checkpointWidth, checkpointHeight, 2f);

                var handler = cpGO.AddComponent<CheckpointTrigger>();
                handler.system = this;
                handler.checkpointIndex = i;
            }
        }

        public void RegisterRacer(Transform racer)
        {
            racers.Add(new RacerProgress
            {
                racer = racer,
                currentCheckpoint = 0,
                currentLap = 0,
                distanceAlongTrack = 0,
                finished = false
            });
        }

        public void OnCheckpointHit(Transform racer, int cpIndex)
        {
            var progress = racers.Find(r => r.racer == racer);
            if (progress == null || progress.finished) return;

            int expected = (progress.currentCheckpoint + 1) % checkpointCount;

            // Only count if hitting the next expected checkpoint (anti-cheat)
            if (cpIndex == expected)
            {
                progress.currentCheckpoint = cpIndex;

                // Crossed start line = new lap
                if (cpIndex == 0 && progress.currentLap >= 0)
                {
                    progress.currentLap++;
                    OnLapComplete?.Invoke(progress);

                    if (progress.currentLap >= totalLaps)
                    {
                        progress.finished = true;
                        progress.finishTime = Time.time;
                        OnRaceFinish?.Invoke(progress);
                    }
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

        void OnTriggerEnter(Collider other)
        {
            // Check if this is a kart
            var kart = other.GetComponentInParent<Kart.KartController>();
            if (kart != null)
            {
                system.OnCheckpointHit(kart.transform, checkpointIndex);
            }
        }
    }
}
