using UnityEngine;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    /// Renders the bottom-of-screen hotbar: shows each slot's bound skill (color + glyph)
    /// and the cooldown overlay/timer while the skill is recharging.
    public class HotbarHUD : MonoBehaviour
    {
        [SerializeField] private Transform actionRoot;

        private Image[] _icons;
        private Text[] _letters;
        private Image[] _cooldownOverlays;
        private Text[] _cooldownTexts;
        private string[] _boundIds;

        private void Awake()
        {
            int n = HotbarSystem.SLOT_COUNT;
            _icons = new Image[n];
            _letters = new Text[n];
            _cooldownOverlays = new Image[n];
            _cooldownTexts = new Text[n];
            _boundIds = new string[n];
            if (actionRoot == null) return;

            for (int i = 0; i < n; i++)
            {
                var slot = actionRoot.Find($"Slot_{i}");
                if (slot == null) continue;
                var iconRT = slot.Find("SkillIcon");
                if (iconRT != null)
                {
                    _icons[i] = iconRT.GetComponent<Image>();
                    var letterRT = iconRT.Find("Letter");
                    if (letterRT != null) _letters[i] = letterRT.GetComponent<Text>();
                }
                var cdRT = slot.Find("CooldownOverlay");
                if (cdRT != null)
                {
                    _cooldownOverlays[i] = cdRT.GetComponent<Image>();
                    var cdTextRT = cdRT.Find("Text");
                    if (cdTextRT != null) _cooldownTexts[i] = cdTextRT.GetComponent<Text>();
                }
            }
        }

        private void Start()
        {
            if (HotbarSystem.Instance != null) HotbarSystem.Instance.OnChanged += RefreshBindings;
            if (SkillSystem.Instance != null) SkillSystem.Instance.OnChanged += RefreshBindings;
            RefreshBindings();
        }

        private void OnDestroy()
        {
            if (HotbarSystem.Instance != null) HotbarSystem.Instance.OnChanged -= RefreshBindings;
            if (SkillSystem.Instance != null) SkillSystem.Instance.OnChanged -= RefreshBindings;
        }

        private void RefreshBindings()
        {
            var hb = HotbarSystem.Instance;
            for (int i = 0; i < HotbarSystem.SLOT_COUNT; i++)
            {
                string id = hb != null ? hb.GetSkillIdAt(i) : "";
                _boundIds[i] = id;
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
                if (_cooldownOverlays[i] != null)
                {
                    _cooldownOverlays[i].fillAmount = 0f;
                    _cooldownOverlays[i].gameObject.SetActive(false);
                }
                if (_cooldownTexts[i] != null) _cooldownTexts[i].text = "";
            }
        }

        private void Update()
        {
            var sc = SkillCaster.Instance;
            if (sc == null) return;

            for (int i = 0; i < HotbarSystem.SLOT_COUNT; i++)
            {
                string id = _boundIds[i];
                var overlay = _cooldownOverlays[i];
                var text = _cooldownTexts[i];
                if (overlay == null) continue;
                if (string.IsNullOrEmpty(id))
                {
                    if (overlay.gameObject.activeSelf) overlay.gameObject.SetActive(false);
                    if (text != null) text.text = "";
                    continue;
                }
                float remaining = sc.GetCooldownRemaining(id);
                if (remaining <= 0.01f)
                {
                    if (overlay.gameObject.activeSelf) overlay.gameObject.SetActive(false);
                    if (text != null) text.text = "";
                    continue;
                }
                if (!overlay.gameObject.activeSelf) overlay.gameObject.SetActive(true);
                overlay.fillAmount = sc.GetCooldownPct(id);
                if (text != null)
                {
                    text.text = remaining >= 1f
                        ? Mathf.CeilToInt(remaining).ToString()
                        : remaining.ToString("0.0");
                }
            }
        }
    }
}
