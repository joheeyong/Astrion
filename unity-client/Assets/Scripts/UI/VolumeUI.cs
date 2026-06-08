using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Astrion.Audio;

namespace Astrion.UI
{
    /// Compact volume panel — three sliders (master / sfx / bgm). Persists
    /// via SoundSystem's PlayerPrefs-backed properties. Toggle with F11
    /// (avoiding the busier 1-12 key range). DDOL singleton, built on the
    /// login boot path with the rest of the audio system.
    public class VolumeUI : MonoBehaviour
    {
        public static VolumeUI Instance { get; private set; }

        private static readonly Color PanelBg     = new Color(0.10f, 0.08f, 0.06f, 0.96f);
        private static readonly Color AccentGold  = new Color(0.85f, 0.65f, 0.22f, 1f);
        private static readonly Color AccentDim   = new Color(0.85f, 0.65f, 0.22f, 0.30f);
        private static readonly Color TextLight   = new Color(0.94f, 0.88f, 0.74f);
        private static readonly Color TextMuted   = new Color(0.62f, 0.55f, 0.42f);
        private static readonly Color FillColor   = new Color(0.85f, 0.65f, 0.22f);
        private static readonly Color HandleColor = new Color(0.95f, 0.82f, 0.45f);

        private CanvasGroup _group;
        private bool _open;
        private Slider _master, _sfx, _bgm;
        private Text _masterVal, _sfxVal, _bgmVal;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Toggle() { if (_open) Close(); else Open(); }

        public void Open()
        {
            _open = true;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            // Reflect current persisted values into the sliders.
            if (_master != null) _master.SetValueWithoutNotify(SoundSystem.MasterVolume);
            if (_sfx    != null) _sfx.SetValueWithoutNotify(SoundSystem.SfxVolume);
            if (_bgm    != null) _bgm.SetValueWithoutNotify(SoundSystem.BgmVolume);
            RefreshLabels();
        }

        public void Close()
        {
            _open = false;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        private void Update()
        {
            if (IsTextInputFocused()) return;
            if (Input.GetKeyDown(KeyCode.F11)) Toggle();
            else if (_open && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private bool IsTextInputFocused()
        {
            if (EventSystem.current == null) return false;
            var sel = EventSystem.current.currentSelectedGameObject;
            return sel != null && sel.GetComponent<InputField>() != null;
        }

        // ──────────────────── BUILD ────────────────────

        private void Build()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("VolumeCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9070;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var groupGo = new GameObject("Group");
            groupGo.transform.SetParent(canvasGo.transform, false);
            var groupRt = groupGo.AddComponent<RectTransform>();
            Stretch(groupRt);
            _group = groupGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            var dimGo = new GameObject("Dim");
            dimGo.transform.SetParent(groupGo.transform, false);
            var dimRt = dimGo.AddComponent<RectTransform>();
            Stretch(dimRt);
            var dim = dimGo.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.55f);

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(groupGo.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(440, 280);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = PanelBg;
            var panelOl = panelGo.AddComponent<Outline>();
            panelOl.effectColor = AccentDim;
            panelOl.effectDistance = new Vector2(1, 1);

            AddText(panelGo.transform, "Eyebrow", "—  V O L U M E  —",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, AccentGold,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -18), new Vector2(300, 22));
            AddText(panelGo.transform, "Title", "★  사운드",
                font, 18, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -44), new Vector2(380, 24));

            _master = BuildRow(panelGo.transform, font, "전체", -82, SoundSystem.MasterVolume,
                v => { SoundSystem.MasterVolume = v; RefreshLabels(); }, out _masterVal);
            _sfx    = BuildRow(panelGo.transform, font, "효과음", -124, SoundSystem.SfxVolume,
                v => { SoundSystem.SfxVolume = v; RefreshLabels();
                       SoundSystem.Instance?.Play(Sfx.Blip); }, out _sfxVal);
            _bgm    = BuildRow(panelGo.transform, font, "배경음", -166, SoundSystem.BgmVolume,
                v => { SoundSystem.BgmVolume = v; RefreshLabels(); }, out _bgmVal);

            AddText(panelGo.transform, "Hint", "[F11] 토글  ·  [Esc] 닫기",
                font, 10, FontStyle.Normal, TextAnchor.MiddleCenter, TextMuted,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 24), new Vector2(300, 18));
        }

        private Slider BuildRow(Transform parent, Font font, string label, float y,
                                 float initial, System.Action<float> onChanged, out Text valLabel)
        {
            var rowGo = new GameObject($"Row_{label}");
            rowGo.transform.SetParent(parent, false);
            var rowRt = rowGo.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 1); rowRt.anchorMax = new Vector2(1, 1);
            rowRt.pivot = new Vector2(0.5f, 1);
            rowRt.offsetMin = new Vector2(30, 0); rowRt.offsetMax = new Vector2(-30, 0);
            rowRt.sizeDelta = new Vector2(0, 32);
            rowRt.anchoredPosition = new Vector2(0, y);

            AddText(rowGo.transform, "L", label,
                font, 13, FontStyle.Bold, TextAnchor.MiddleLeft, TextLight,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(0, 0), new Vector2(80, 0));

            valLabel = AddText(rowGo.transform, "V", "100%",
                font, 12, FontStyle.Bold, TextAnchor.MiddleRight, AccentGold,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                new Vector2(0, 0), new Vector2(54, 0));

            // Slider track
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(rowGo.transform, false);
            var sRt = sliderGo.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0, 0); sRt.anchorMax = new Vector2(1, 1);
            sRt.offsetMin = new Vector2(80, 8); sRt.offsetMax = new Vector2(-60, -8);
            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.5f); bgRt.anchorMax = new Vector2(1, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(0, 6);
            bgRt.anchoredPosition = Vector2.zero;
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.05f, 0.04f, 0.95f);

            // Fill
            var fillAreaGo = new GameObject("FillArea");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRt = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0, 0.5f); fillAreaRt.anchorMax = new Vector2(1, 0.5f);
            fillAreaRt.pivot = new Vector2(0.5f, 0.5f);
            fillAreaRt.sizeDelta = new Vector2(-10, 6);
            fillAreaRt.anchoredPosition = Vector2.zero;
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            var fill = fillGo.AddComponent<Image>();
            fill.color = FillColor;
            slider.fillRect = fillRt;

            // Handle
            var handleAreaGo = new GameObject("HandleArea");
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            var handleAreaRt = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRt.anchorMin = new Vector2(0, 0); handleAreaRt.anchorMax = new Vector2(1, 1);
            handleAreaRt.sizeDelta = new Vector2(-10, 0);
            handleAreaRt.anchoredPosition = Vector2.zero;
            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRt = handleGo.AddComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(14, 18);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = HandleColor;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;

            slider.SetValueWithoutNotify(initial);
            slider.onValueChanged.AddListener(v => onChanged?.Invoke(v));
            return slider;
        }

        private void RefreshLabels()
        {
            if (_masterVal != null) _masterVal.text = Mathf.RoundToInt(SoundSystem.MasterVolume * 100f) + "%";
            if (_sfxVal    != null) _sfxVal.text    = Mathf.RoundToInt(SoundSystem.SfxVolume    * 100f) + "%";
            if (_bgmVal    != null) _bgmVal.text    = Mathf.RoundToInt(SoundSystem.BgmVolume    * 100f) + "%";
        }

        // ──────────────────── primitives ────────────────────

        private static Text AddText(Transform parent, string name, string text,
            Font font, int size, FontStyle style, TextAnchor align, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            if (anchorMin.x != anchorMax.x) { rt.offsetMin = new Vector2(0, rt.offsetMin.y); rt.offsetMax = new Vector2(0, rt.offsetMax.y); }
            if (anchorMin.y != anchorMax.y) { rt.offsetMin = new Vector2(rt.offsetMin.x, 0); rt.offsetMax = new Vector2(rt.offsetMax.x, 0); }
            if (anchorMin == anchorMax) rt.sizeDelta = sizeDelta;
            var t = go.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.fontStyle = style;
            t.alignment = align; t.color = color; t.text = text;
            t.raycastTarget = false;
            return t;
        }
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
