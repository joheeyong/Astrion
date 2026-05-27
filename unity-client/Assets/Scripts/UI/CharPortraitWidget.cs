using UnityEngine;
using UnityEngine.UI;

namespace Astrion.UI
{
    /// Runtime tint for the character portrait swatch. Reads the active
    /// character's class from PlayerPrefs and recolors the portrait Image
    /// to a class-specific palette. Cheap, no extra textures — same
    /// procedural sprite, different tint.
    ///
    /// Class tints picked to match each class's biome city color from the
    /// world map, so a player coming back from Pyresummit-warm Warrior
    /// territory sees the same warmth in their character panel.
    [RequireComponent(typeof(Image))]
    public class CharPortraitWidget : MonoBehaviour
    {
        private Image _img;
        private string _lastClass = "";

        private void Awake() { _img = GetComponent<Image>(); }
        private void OnEnable() { Apply(); }

        private void Update()
        {
            // Class can switch between sessions; cheap string compare so we
            // only retint when something actually changes.
            string cls = PlayerPrefs.GetString("characterClass", "");
            if (cls != _lastClass) Apply();
        }

        private void Apply()
        {
            if (_img == null) return;
            _lastClass = PlayerPrefs.GetString("characterClass", "");
            _img.color = ClassTint(_lastClass);
        }

        public static Color ClassTint(string className)
        {
            switch (className)
            {
                case "Warrior": return new Color(0.85f, 0.42f, 0.30f);  // ember red
                case "Mage":    return new Color(0.55f, 0.55f, 0.92f);  // arcane blue
                case "Archer":  return new Color(0.55f, 0.85f, 0.55f);  // forest green
                case "Thief":   return new Color(0.62f, 0.50f, 0.85f);  // shadow violet
                default:        return new Color(0.85f, 0.72f, 0.40f);  // neutral gold
            }
        }
    }
}
