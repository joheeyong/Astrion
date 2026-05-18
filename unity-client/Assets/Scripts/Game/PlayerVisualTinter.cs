using UnityEngine;
using UnityEngine.UI;

namespace Astrion.Game
{
    /// Tints the multi-part player sprite based on PlayerPrefs.characterClass.
    /// Lives on the local player game object (and on a remote player if its
    /// class is ever populated). Single source of truth for class colors —
    /// CharacterSelectUI also pulls from ClassColors so the preview matches.
    public class PlayerVisualTinter : MonoBehaviour
    {
        [SerializeField] private string overrideClass = ""; // optional inspector override

        private void Start()
        {
            string cls = !string.IsNullOrEmpty(overrideClass)
                ? overrideClass
                : PlayerPrefs.GetString("characterClass", "");
            ApplyTo(transform, cls);
        }

        public static (Color body, Color leg) ClassColors(string className)
        {
            switch (className)
            {
                case "Warrior":  return (new Color(0.62f, 0.16f, 0.16f), new Color(0.32f, 0.22f, 0.18f));
                case "Mage":     return (new Color(0.30f, 0.32f, 0.65f), new Color(0.18f, 0.18f, 0.32f));
                case "Archer":   return (new Color(0.30f, 0.55f, 0.32f), new Color(0.30f, 0.22f, 0.14f));
                case "Thief":    return (new Color(0.22f, 0.22f, 0.26f), new Color(0.16f, 0.16f, 0.20f));
                default:         return (new Color(0.30f, 0.48f, 0.22f), new Color(0.38f, 0.26f, 0.16f));
            }
        }

        /// Applies the class tint to a SpriteContainer hierarchy (BuildPlayerVisual layout).
        public static void ApplyTo(Transform root, string className)
        {
            var colors = ClassColors(className);
            TintSprite(root, "SpriteContainer/Body",     colors.body);
            TintSprite(root, "SpriteContainer/LeftArm",  colors.body);
            TintSprite(root, "SpriteContainer/RightArm", colors.body);
            TintSprite(root, "SpriteContainer/LeftLeg",  colors.leg);
            TintSprite(root, "SpriteContainer/RightLeg", colors.leg);
        }

        private static void TintSprite(Transform root, string path, Color c)
        {
            var t = root.Find(path);
            if (t == null) return;
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = c;
        }

        /// Applies the class tint to a UI hierarchy (BuildPlayerVisualUI layout).
        public static void ApplyToUI(Transform charVisual, string className)
        {
            if (charVisual == null) return;
            var colors = ClassColors(className);
            for (int i = 0; i < charVisual.childCount; i++)
            {
                var child = charVisual.GetChild(i);
                var img = child.GetComponent<Image>();
                if (img == null) continue;
                string n = child.name;
                if (n == "Body" || n == "LeftArm" || n == "RightArm") img.color = colors.body;
                else if (n == "LeftLeg" || n == "RightLeg") img.color = colors.leg;
            }
        }
    }
}
