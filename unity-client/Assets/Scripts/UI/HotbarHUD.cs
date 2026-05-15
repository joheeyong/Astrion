using UnityEngine;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    /// Renders the bottom-of-screen hotbar: shows each slot's bound skill (color + glyph).
    public class HotbarHUD : MonoBehaviour
    {
        [SerializeField] private Transform actionRoot;

        private Image[] _icons;
        private Text[] _letters;

        private void Awake()
        {
            _icons = new Image[HotbarSystem.SLOT_COUNT];
            _letters = new Text[HotbarSystem.SLOT_COUNT];
            if (actionRoot == null) return;

            for (int i = 0; i < HotbarSystem.SLOT_COUNT; i++)
            {
                var slot = actionRoot.Find($"Slot_{i}");
                if (slot == null) continue;
                var iconRT = slot.Find("SkillIcon");
                if (iconRT == null) continue;
                _icons[i] = iconRT.GetComponent<Image>();
                var letterRT = iconRT.Find("Letter");
                if (letterRT != null) _letters[i] = letterRT.GetComponent<Text>();
            }
        }

        private void Start()
        {
            if (HotbarSystem.Instance != null) HotbarSystem.Instance.OnChanged += Refresh;
            if (SkillSystem.Instance != null) SkillSystem.Instance.OnChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (HotbarSystem.Instance != null) HotbarSystem.Instance.OnChanged -= Refresh;
            if (SkillSystem.Instance != null) SkillSystem.Instance.OnChanged -= Refresh;
        }

        private void Refresh()
        {
            var hb = HotbarSystem.Instance;
            for (int i = 0; i < HotbarSystem.SLOT_COUNT; i++)
            {
                string id = hb != null ? hb.GetSkillIdAt(i) : "";
                var def = string.IsNullOrEmpty(id) ? null : SkillDatabase.Get(id);
                bool show = def != null;
                if (_icons[i] != null)
                {
                    _icons[i].gameObject.SetActive(show);
                    if (show) _icons[i].color = def.iconColor;
                }
                if (_letters[i] != null)
                {
                    _letters[i].text = show ? def.iconLetter : "";
                }
            }
        }
    }
}
