using UnityEngine;
using UnityEngine.UI;
using CatnipCart.Kart;
using CatnipCart.Track;
using CatnipCart.Items;

namespace CatnipCart.UI
{
    /// <summary>
    /// In-race HUD: position, lap counter, item display, countdown, speedometer, results.
    /// </summary>
    public class RaceUI : MonoBehaviour
    {
        [Header("References")]
        public Core.RaceManager raceManager;
        public CheckpointSystem checkpointSystem;
        public KartController playerKart;

        // UI elements (created at runtime)
        private Text positionText;
        private Text lapText;
        private Text countdownText;
        private Text speedText;
        private Text itemText;
        private Text resultsText;
        private GameObject resultsPanel;
        private CanvasGroup countdownGroup;

        void Start()
        {
            BuildUI();

            if (raceManager != null)
            {
                raceManager.OnCountdownTick += OnCountdown;
                raceManager.OnRaceComplete += ShowResults;
                raceManager.OnLapComplete += OnLap;
            }

            if (playerKart != null)
            {
                var holder = playerKart.GetComponent<ItemHolder>();
                if (holder != null)
                {
                    holder.OnItemReceived += (item) => UpdateItemDisplay(item.ToString());
                    holder.OnItemUsed += () => UpdateItemDisplay("");
                }
            }
        }

        void BuildUI()
        {
            // Create Canvas
            var canvasGO = new GameObject("RaceCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Position display (top-left)
            positionText = CreateText(canvasGO.transform, "PositionText",
                new Vector2(120, -60), 72, TextAnchor.MiddleLeft, FontStyle.Bold);
            positionText.text = "1st";

            // Lap counter (top-center)
            lapText = CreateText(canvasGO.transform, "LapText",
                new Vector2(0, -40), 36, TextAnchor.MiddleCenter, FontStyle.Normal);
            lapText.text = "Lap 1/3";

            // Speed (bottom-right)
            speedText = CreateText(canvasGO.transform, "SpeedText",
                new Vector2(-120, 60), 28, TextAnchor.MiddleRight, FontStyle.Normal);

            // Item display (top-right)
            itemText = CreateText(canvasGO.transform, "ItemText",
                new Vector2(-120, -60), 32, TextAnchor.MiddleRight, FontStyle.Normal);
            itemText.text = "";

            // Countdown (center)
            var countdownGO = new GameObject("CountdownGroup");
            countdownGO.transform.SetParent(canvasGO.transform, false);
            countdownGroup = countdownGO.AddComponent<CanvasGroup>();
            countdownText = CreateText(countdownGO.transform, "CountdownText",
                Vector2.zero, 120, TextAnchor.MiddleCenter, FontStyle.Bold);
            countdownText.text = "";

            // Results panel (hidden)
            resultsPanel = new GameObject("ResultsPanel");
            resultsPanel.transform.SetParent(canvasGO.transform, false);
            var rt = resultsPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var bg = resultsPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            resultsText = CreateText(resultsPanel.transform, "ResultsText",
                Vector2.zero, 48, TextAnchor.MiddleCenter, FontStyle.Bold);

            resultsPanel.SetActive(false);
        }

        Text CreateText(Transform parent, string name, Vector2 pos, int size,
            TextAnchor anchor, FontStyle style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400, 100);

            // Anchor based on position
            if (pos.x < -50) { rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(1, 0); }
            else if (pos.x > 50) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero; }
            else { rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); }

            if (pos.y > 0) { rt.anchorMin = new Vector2(rt.anchorMin.x, 0); rt.anchorMax = new Vector2(rt.anchorMax.x, 0); }
            else if (pos.y < 0) { rt.anchorMin = new Vector2(rt.anchorMin.x, 1); rt.anchorMax = new Vector2(rt.anchorMax.x, 1); }

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.fontStyle = style;
            text.color = Color.white;

            // Outline
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            return text;
        }

        void Update()
        {
            if (playerKart == null || checkpointSystem == null) return;

            var progress = checkpointSystem.GetProgress(playerKart.transform);
            if (progress == null) return;

            // Update position
            string[] suffixes = { "st", "nd", "rd", "th" };
            int pos = progress.position;
            string suffix = pos <= 3 ? suffixes[pos - 1] : suffixes[3];
            positionText.text = $"{pos}{suffix}";

            // Color by position
            positionText.color = pos switch
            {
                1 => new Color(1f, 0.85f, 0f),    // Gold
                2 => new Color(0.75f, 0.75f, 0.8f), // Silver
                3 => new Color(0.8f, 0.5f, 0.2f),   // Bronze
                _ => Color.white
            };

            // Lap
            int lap = Mathf.Max(1, progress.currentLap + 1);
            lapText.text = $"Lap {Mathf.Min(lap, raceManager.totalLaps)}/{raceManager.totalLaps}";

            // Speed
            float mph = Mathf.Abs(playerKart.CurrentSpeed) * 3.6f; // rough km/h
            speedText.text = $"{mph:F0} km/h";

            // Fade countdown
            if (countdownGroup != null && countdownGroup.alpha > 0)
                countdownGroup.alpha -= Time.deltaTime;
        }

        void OnCountdown(int num)
        {
            if (countdownGroup != null) countdownGroup.alpha = 1f;

            if (num > 0)
                countdownText.text = num.ToString();
            else
                countdownText.text = "GO!";
        }

        void OnLap(CheckpointSystem.RacerProgress progress)
        {
            // Flash lap text
            if (progress.racer == playerKart.transform)
                lapText.color = Color.yellow;
        }

        void UpdateItemDisplay(string itemName)
        {
            if (itemText != null)
            {
                string display = itemName switch
                {
                    "YarnBall" => "🧶 Yarn Ball",
                    "Hairball" => "🐾 Hairball",
                    "CatnipBoost" => "🌿 Catnip Boost",
                    "LaserPointer" => "🔴 Laser Pointer",
                    "GoldenCatnip" => "✨ Golden Catnip",
                    _ => ""
                };
                itemText.text = display;
            }
        }

        void ShowResults()
        {
            if (resultsPanel == null) return;
            resultsPanel.SetActive(true);

            var progress = checkpointSystem.GetProgress(playerKart.transform);
            if (progress == null) return;

            string place = progress.position switch
            {
                1 => "🏆 1st Place! 🏆",
                2 => "🥈 2nd Place!",
                3 => "🥉 3rd Place!",
                _ => $"{progress.position}th Place"
            };

            string celebrate = progress.position <= 3 ? "\n\nGreat job! 🐱" : "\n\nBetter luck next time!";
            resultsText.text = $"FINISH!\n\n{place}{celebrate}\n\nPress R to restart";
        }
    }
}
