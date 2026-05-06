using UnityEngine;

namespace CatnipCart.Core
{
    /// <summary>
    /// Color scheme data for each cat character, matching the Super Cat World closet system.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCatColor", menuName = "Catnip Cart/Cat Color Data")]
    public class CatColorData : ScriptableObject
    {
        public string catName = "Ginger";

        [Header("Body Colors")]
        public Color body = new Color(0.96f, 0.62f, 0.04f);       // #f59e0b
        public Color bodyDark = new Color(0.85f, 0.47f, 0.02f);   // #d97706
        public Color belly = new Color(1f, 0.95f, 0.78f);         // #fef3c7

        [Header("Details")]
        public Color paw = new Color(0.99f, 0.9f, 0.54f);         // #fde68a
        public Color innerEar = new Color(0.99f, 0.65f, 0.65f);   // #fca5a5
        public Color eyes = new Color(0.02f, 0.37f, 0.27f);       // #065f46
        public Color nose = new Color(0.96f, 0.45f, 0.71f);       // #f472b6

        [Header("Kart")]
        public Color kartPrimary = new Color(0.96f, 0.62f, 0.04f);
        public Color kartSecondary = new Color(1f, 1f, 1f);
        public Color kartAccent = new Color(0.96f, 0.45f, 0.71f);

        /// <summary>
        /// Pre-defined color schemes matching the Super Cat World closet.
        /// </summary>
        public static CatColorData CreateGinger()
        {
            var data = CreateInstance<CatColorData>();
            data.catName = "Ginger";
            data.body = HexToColor("#f59e0b");
            data.bodyDark = HexToColor("#d97706");
            data.belly = HexToColor("#fef3c7");
            data.paw = HexToColor("#fde68a");
            data.innerEar = HexToColor("#fca5a5");
            data.eyes = HexToColor("#065f46");
            data.nose = HexToColor("#f472b6");
            data.kartPrimary = HexToColor("#f59e0b");
            data.kartSecondary = HexToColor("#ffffff");
            data.kartAccent = HexToColor("#f472b6");
            return data;
        }

        public static CatColorData CreateShadow()
        {
            var data = CreateInstance<CatColorData>();
            data.catName = "Shadow";
            data.body = HexToColor("#6b7280");
            data.bodyDark = HexToColor("#4b5563");
            data.belly = HexToColor("#e5e7eb");
            data.paw = HexToColor("#d1d5db");
            data.innerEar = HexToColor("#fca5a5");
            data.eyes = HexToColor("#1e40af");
            data.nose = HexToColor("#f472b6");
            data.kartPrimary = HexToColor("#6b7280");
            data.kartSecondary = HexToColor("#e5e7eb");
            data.kartAccent = HexToColor("#1e40af");
            return data;
        }

        public static CatColorData CreateMidnight()
        {
            var data = CreateInstance<CatColorData>();
            data.catName = "Midnight";
            data.body = HexToColor("#374151");
            data.bodyDark = HexToColor("#1f2937");
            data.belly = HexToColor("#6b7280");
            data.paw = HexToColor("#9ca3af");
            data.innerEar = HexToColor("#fb923c");
            data.eyes = HexToColor("#eab308");
            data.nose = HexToColor("#d1d5db");
            data.kartPrimary = HexToColor("#374151");
            data.kartSecondary = HexToColor("#6b7280");
            data.kartAccent = HexToColor("#eab308");
            return data;
        }

        public static CatColorData CreateSnow()
        {
            var data = CreateInstance<CatColorData>();
            data.catName = "Snow";
            data.body = HexToColor("#e5e7eb");
            data.bodyDark = HexToColor("#d1d5db");
            data.belly = HexToColor("#f9fafb");
            data.paw = HexToColor("#fce7f3");
            data.innerEar = HexToColor("#fca5a5");
            data.eyes = HexToColor("#7c3aed");
            data.nose = HexToColor("#f9a8d4");
            data.kartPrimary = HexToColor("#e5e7eb");
            data.kartSecondary = HexToColor("#f9fafb");
            data.kartAccent = HexToColor("#7c3aed");
            return data;
        }

        private static Color HexToColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
