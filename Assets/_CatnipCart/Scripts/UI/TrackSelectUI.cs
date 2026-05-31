using UnityEngine;
using UnityEngine.UI;
using CatnipCart.Track;
using CatnipCart.Core;

namespace CatnipCart.UI
{
    /// <summary>
    /// Visual track selection menu shown before a race.
    /// Displays all available tracks with name, emoji, description, and theme colors.
    /// Navigate with arrow keys or A/D, confirm with Enter/Space.
    /// </summary>
    public class TrackSelectUI : MonoBehaviour
    {
        private TrackData[] allTracks;
        private int selectedIndex = 0;
        private float inputCooldown = 0f;

        // UI references (built at runtime)
        private Canvas canvas;
        private Text titleText;
        private Text trackNameText;
        private Text trackDescText;
        private Text navHintText;
        private Image bgPanel;
        private Image[] trackDots;
        private Image trackPreviewBg;
        private CanvasGroup fadeGroup;
        private float fadeAlpha = 0f;
        private bool launching = false;
        private float launchTimer = 0f;

        // Animation
        private float selectAnimTime = 0f;
        private int lastSelectedIndex = -1;

        void Start()
        {
            allTracks = TrackData.GetAllTracks();
            BuildUI();
            UpdateDisplay();
        }

        void BuildUI()
        {
            // === Canvas ===
            var canvasGO = new GameObject("TrackSelectCanvas");
            canvasGO.transform.SetParent(transform, false);
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // === Full-screen background ===
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            bgPanel = bgGO.AddComponent<Image>();
            bgPanel.color = new Color(0.06f, 0.04f, 0.1f);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // === Title ===
            titleText = CreateText(canvasGO.transform, "Title",
                new Vector2(0, 420), 64, TextAnchor.MiddleCenter, FontStyle.Bold,
                new Color(1f, 0.85f, 0.3f));
            titleText.text = "🐱 SELECT YOUR TRACK 🐱";

            // === Track preview card background ===
            var cardGO = new GameObject("TrackCard");
            cardGO.transform.SetParent(canvasGO.transform, false);
            trackPreviewBg = cardGO.AddComponent<Image>();
            trackPreviewBg.color = new Color(0.12f, 0.1f, 0.18f, 0.8f);
            var cardRT = cardGO.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.anchoredPosition = new Vector2(0, 40);
            cardRT.sizeDelta = new Vector2(800, 400);

            // Rounded corners via outline
            var cardOutline = cardGO.AddComponent<Outline>();
            cardOutline.effectColor = new Color(1f, 0.85f, 0.3f, 0.6f);
            cardOutline.effectDistance = new Vector2(3, -3);

            // === Track emoji + name ===
            trackNameText = CreateText(cardGO.transform, "TrackName",
                new Vector2(0, 100), 72, TextAnchor.MiddleCenter, FontStyle.Bold,
                Color.white);

            // === Track description ===
            trackDescText = CreateText(cardGO.transform, "TrackDesc",
                new Vector2(0, -20), 28, TextAnchor.MiddleCenter, FontStyle.Italic,
                new Color(0.8f, 0.8f, 0.85f));
            var descRT = trackDescText.GetComponent<RectTransform>();
            descRT.sizeDelta = new Vector2(700, 80);

            // === Track info details ===
            // (will be set dynamically in UpdateDisplay)

            // === Navigation dots ===
            var dotsGO = new GameObject("Dots");
            dotsGO.transform.SetParent(canvasGO.transform, false);
            var dotsRT = dotsGO.AddComponent<RectTransform>();
            dotsRT.anchorMin = new Vector2(0.5f, 0.5f);
            dotsRT.anchorMax = new Vector2(0.5f, 0.5f);
            dotsRT.anchoredPosition = new Vector2(0, -200);
            dotsRT.sizeDelta = new Vector2(600, 30);

            var dotsLayout = dotsGO.AddComponent<HorizontalLayoutGroup>();
            dotsLayout.childAlignment = TextAnchor.MiddleCenter;
            dotsLayout.spacing = 16;
            dotsLayout.childForceExpandWidth = false;
            dotsLayout.childForceExpandHeight = false;

            trackDots = new Image[allTracks.Length];
            for (int i = 0; i < allTracks.Length; i++)
            {
                var dotGO = new GameObject($"Dot_{i}");
                dotGO.transform.SetParent(dotsGO.transform, false);
                var dotImg = dotGO.AddComponent<Image>();
                dotImg.color = new Color(0.4f, 0.4f, 0.5f);
                var dotLE = dotGO.AddComponent<LayoutElement>();
                dotLE.preferredWidth = 16;
                dotLE.preferredHeight = 16;
                trackDots[i] = dotImg;
            }

            // === Navigation hint text ===
            navHintText = CreateText(canvasGO.transform, "NavHint",
                new Vector2(0, -300), 24, TextAnchor.MiddleCenter, FontStyle.Normal,
                new Color(0.6f, 0.6f, 0.7f));
            navHintText.text = "◄  A / ←      ENTER to Race      D / →  ►";

            // === Fade overlay for launch transition ===
            var fadeGO = new GameObject("FadeOverlay");
            fadeGO.transform.SetParent(canvasGO.transform, false);
            fadeGroup = fadeGO.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;

            var fadeImg = fadeGO.AddComponent<Image>();
            fadeImg.color = Color.black;
            var fadeRT = fadeGO.GetComponent<RectTransform>();
            fadeRT.anchorMin = Vector2.zero;
            fadeRT.anchorMax = Vector2.one;
            fadeRT.sizeDelta = Vector2.zero;
        }

        void Update()
        {
            if (launching)
            {
                launchTimer += Time.deltaTime;
                fadeGroup.alpha = Mathf.Clamp01(launchTimer / 0.5f);
                if (launchTimer >= 0.6f)
                {
                    LaunchRace();
                }
                return;
            }

            inputCooldown -= Time.deltaTime;
            if (inputCooldown > 0) return;

            // Navigation
            bool left = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            bool right = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
            bool confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);

            if (left)
            {
                selectedIndex = (selectedIndex - 1 + allTracks.Length) % allTracks.Length;
                inputCooldown = 0.15f;
                selectAnimTime = 0f;
                UpdateDisplay();
            }
            else if (right)
            {
                selectedIndex = (selectedIndex + 1) % allTracks.Length;
                inputCooldown = 0.15f;
                selectAnimTime = 0f;
                UpdateDisplay();
            }
            else if (confirm)
            {
                launching = true;
                launchTimer = 0f;
                navHintText.text = "Loading...";
            }

            // Animate the card
            selectAnimTime += Time.deltaTime;
            AnimateCard();
        }

        void UpdateDisplay()
        {
            var track = allTracks[selectedIndex];

            trackNameText.text = $"{track.trackEmoji}  {track.trackName}  {track.trackEmoji}";
            trackDescText.text = track.trackDescription;

            // Update background gradient to match track theme
            Color themeColor = track.skyHorizon;
            bgPanel.color = Color.Lerp(new Color(0.06f, 0.04f, 0.1f), themeColor, 0.15f);
            trackPreviewBg.color = Color.Lerp(new Color(0.12f, 0.1f, 0.18f, 0.8f), themeColor, 0.1f);

            // Update card outline color to match track curb color
            var outline = trackPreviewBg.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = Color.Lerp(track.curbColor, Color.white, 0.3f);
            }

            // Update dots
            for (int i = 0; i < trackDots.Length; i++)
            {
                if (i == selectedIndex)
                {
                    trackDots[i].color = new Color(1f, 0.85f, 0.3f);
                    trackDots[i].GetComponent<RectTransform>().sizeDelta = Vector2.zero;
                    trackDots[i].GetComponent<LayoutElement>().preferredWidth = 24;
                    trackDots[i].GetComponent<LayoutElement>().preferredHeight = 24;
                }
                else
                {
                    trackDots[i].color = new Color(0.4f, 0.4f, 0.5f);
                    trackDots[i].GetComponent<LayoutElement>().preferredWidth = 16;
                    trackDots[i].GetComponent<LayoutElement>().preferredHeight = 16;
                }
            }

            // Track number indicator
            titleText.text = $"🐱 SELECT YOUR TRACK ({selectedIndex + 1}/{allTracks.Length}) 🐱";

            lastSelectedIndex = selectedIndex;
        }

        void AnimateCard()
        {
            // Subtle floating animation on the card
            if (trackPreviewBg != null)
            {
                var rt = trackPreviewBg.GetComponent<RectTransform>();
                float floatY = Mathf.Sin(Time.time * 1.5f) * 4f;
                rt.anchoredPosition = new Vector2(0, 40 + floatY);
            }

            // Pulsing glow on the track name
            if (trackNameText != null)
            {
                float pulse = 0.85f + Mathf.Sin(Time.time * 2f) * 0.15f;
                trackNameText.color = Color.white * pulse;
            }
        }

        void LaunchRace()
        {
            SceneSetup.SelectedTrackIndex = selectedIndex;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        Text CreateText(Transform parent, string name, Vector2 pos, int size,
            TextAnchor anchor, FontStyle style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1200, 100);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.fontStyle = style;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Outline for readability
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(2, -2);

            // Shadow for depth
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(3, -3);

            return text;
        }
    }
}
