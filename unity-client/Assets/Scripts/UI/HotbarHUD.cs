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
        // Per-slot bookkeeping so Update can tell when a cooldown finished
        // this frame (→ trigger a brief ready-flash) and when MP recovered
        // (→ swap the icon back from desaturated to normal).
        private bool[] _wasOnCooldown;
        private float[] _flashTimer; // seconds remaining of ready-flash
        private Color[] _baseIconColor;
        private const float FlashSeconds = 0.45f;

        private void Awake()
        {
            int n = HotbarSystem.SLOT_COUNT;
            _icons = new Image[n];
            _letters = new Text[n];
            _cooldownOverlays = new Image[n];
            _cooldownTexts = new Text[n];
            _boundIds = new string[n];
            _wasOnCooldown = new bool[n];
            _flashTimer = new float[n];
            _baseIconColor = new Color[n];
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
                    if (show)
                    {
                        _baseIconColor[i] = def.iconColor;
                        _icons[i].color = def.iconColor;
                    }
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
                _wasOnCooldown[i] = false;
                _flashTimer[i] = 0f;
            }
        }

        private void Update()
        {
            var sc = SkillCaster.Instance;
            var stats = PlayerStats.Instance;
            if (sc == null) return;

            for (int i = 0; i < HotbarSystem.SLOT_COUNT; i++)
            {
                string id = _boundIds[i];
                var overlay = _cooldownOverlays[i];
                var text = _cooldownTexts[i];
                var icon = _icons[i];
                if (overlay == null) continue;
                if (string.IsNullOrEmpty(id))
                {
                    if (overlay.gameObject.activeSelf) overlay.gameObject.SetActive(false);
                    if (text != null) text.text = "";
                    continue;
                }

                var def = SkillDatabase.Get(id);
                float remaining = sc.GetCooldownRemaining(id);
                bool onCd = remaining > 0.01f;

                // Cooldown overlay drives the radial fill + remaining seconds.
                if (!onCd)
                {
                    if (overlay.gameObject.activeSelf) overlay.gameObject.SetActive(false);
                    if (text != null) text.text = "";
                }
                else
                {
                    if (!overlay.gameObject.activeSelf) overlay.gameObject.SetActive(true);
                    overlay.fillAmount = sc.GetCooldownPct(id);
                    if (text != null)
                    {
                        text.text = remaining >= 1f
                            ? Mathf.CeilToInt(remaining).ToString()
                            : remaining.ToString("0.0");
                    }
                }

                // Ready-flash: white pulse for FlashSeconds right when a
                // cooldown completes. Catches the eye so the player notices
                // the skill is back up without watching the clock.
                if (_wasOnCooldown[i] && !onCd) _flashTimer[i] = FlashSeconds;
                _wasOnCooldown[i] = onCd;
                if (_flashTimer[i] > 0f) _flashTimer[i] -= Time.deltaTime;

                // Icon tint: desaturate when MP < cost (and not on cooldown,
                // since the overlay already conveys 'unavailable'). Overlay
                // the ready-flash pulse on top.
                if (icon != null && def != null)
                {
                    Color baseCol = _baseIconColor[i];
                    bool poorMp = !onCd && stats != null && def.mpCost > 0 && stats.Mp < def.mpCost;
                    Color tint = poorMp
                        ? new Color(baseCol.r * 0.45f, baseCol.g * 0.45f, baseCol.b * 0.45f, 0.7f)
                        : baseCol;
                    if (_flashTimer[i] > 0f)
                    {
                        float k = _flashTimer[i] / FlashSeconds; // 1 → 0
                        tint = Color.Lerp(tint, Color.white, k * 0.85f);
                    }
                    icon.color = tint;
                }
            }
        }
    }
}
