using UnityEngine;
using CatnipCart.Kart;
using CatnipCart.Track;
using CatnipCart.Items;

namespace CatnipCart.Core
{
    /// <summary>
    /// Central race state machine. Manages countdown, race progression,
    /// finish detection, and results display.
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        public enum RaceState { PreRace, Countdown, Racing, Finished }
        public RaceState CurrentState { get; private set; } = RaceState.PreRace;

        [Header("Config")]
        public int totalLaps = 3;
        public float countdownDuration = 4f; // 3..2..1..GO!

        [Header("References")]
        public TrackSpline spline;
        public CheckpointSystem checkpointSystem;

        // Events for UI
        public System.Action<int> OnCountdownTick; // 3, 2, 1, 0(GO!)
        public System.Action<CheckpointSystem.RacerProgress> OnRacerFinish;
        public System.Action OnRaceComplete;
        public System.Action<CheckpointSystem.RacerProgress> OnLapComplete;

        private float countdownTimer;
        private int lastCountdownNum = -1;
        private int finishCount;
        private KartController[] allKarts;

        void Start()
        {
            allKarts = FindObjectsByType<KartController>(FindObjectsSortMode.None);

            if (checkpointSystem != null)
            {
                checkpointSystem.totalLaps = totalLaps;
                checkpointSystem.OnRaceFinish += OnRacerFinished;
                checkpointSystem.OnLapComplete += (p) => OnLapComplete?.Invoke(p);

                // Register all karts
                foreach (var k in allKarts)
                    checkpointSystem.RegisterRacer(k.transform);
            }

            // Start countdown
            StartCountdown();
        }

        void StartCountdown()
        {
            CurrentState = RaceState.Countdown;
            countdownTimer = countdownDuration;

            // Freeze all karts
            foreach (var k in allKarts)
            {
                var rb = k.GetComponent<Rigidbody>();
                if (rb) rb.isKinematic = true;
            }
        }

        void Update()
        {
            switch (CurrentState)
            {
                case RaceState.Countdown:
                    UpdateCountdown();
                    break;
                case RaceState.Racing:
                    // Race runs via physics, checkpoint system handles tracking
                    break;
                case RaceState.Finished:
                    break;
            }
        }

        void UpdateCountdown()
        {
            countdownTimer -= Time.deltaTime;
            int num = Mathf.CeilToInt(countdownTimer);

            if (num != lastCountdownNum && num >= 0)
            {
                lastCountdownNum = num;
                OnCountdownTick?.Invoke(num);
            }

            if (countdownTimer <= 0)
            {
                // GO!
                CurrentState = RaceState.Racing;

                // Unfreeze karts
                foreach (var k in allKarts)
                {
                    var rb = k.GetComponent<Rigidbody>();
                    if (rb) rb.isKinematic = false;
                }

                OnCountdownTick?.Invoke(0); // "GO!"
            }
        }

        void OnRacerFinished(CheckpointSystem.RacerProgress progress)
        {
            finishCount++;
            OnRacerFinish?.Invoke(progress);

            // Check if player finished (1st, 2nd, or 3rd = celebrate!)
            var playerKart = progress.racer.GetComponent<KartInput>();
            if (playerKart != null)
            {
                // Player finished! Show results
                CurrentState = RaceState.Finished;
                OnRaceComplete?.Invoke();
            }

            // All racers finished
            if (finishCount >= allKarts.Length)
            {
                CurrentState = RaceState.Finished;
                OnRaceComplete?.Invoke();
            }
        }
    }
}
