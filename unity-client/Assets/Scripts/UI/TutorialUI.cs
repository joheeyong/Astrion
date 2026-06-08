using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Astrion.Game;

namespace Astrion.UI
{
    /// Bottom-center panel that hosts the tutorial flow. Auto-shows when
    /// TutorialSystem activates, hides when done/skipped. F1 anywhere
    /// restarts the tour; the panel itself exposes a [건너뛰기] button.
    /// DDOL singleton built alongside the rest of the boot UI on networkGo.
    public class TutorialUI : MonoBehaviour
    {
        public static TutorialUI Instance { get; private set; }

        private static readonly Color PanelBg     = new Color(0.10f, 0.08f, 0.06f, 0.94f);
        private static readonly Color AccentGold  = new Color(0.85f, 0.65f, 0.22f, 1f);
        private static readonly Color AccentDim   = new Color(0.85f, 0.65f, 0.22f, 0.30f);
        private static readonly Color TextLight   = new Color(0.94f, 0.88f, 0.74f);
        private static readonly Color TextMuted   = new Color(0.62f, 0.55f, 0.42f);

        private CanvasGroup _group;
        private Text _stepText;
        private Text _stepNumberText;
        private Image _progressFill;
        private Image _progressBg;
        private bool _visible;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
        }

        private void Start()
        {
            if (TutorialSystem.Instance != null)
                TutorialSystem.Instance.OnUpdated += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (TutorialSystem.Instance != null)
                TutorialSystem.Instance.OnUpdated -= Refresh;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // F1 — restart the tour from step 0. Available from anywhere
            // *unless* a text field has focus (don't fight chat input).
            if (Input.GetKeyDown(KeyCode.F1) && !IsTextInputFocused())
            {
                TutorialSystem.Instance?.Reset();
            }
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
            var canvasGo = new GameObject("TutorialCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 8800; // above HUD, below modals
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

            // Bottom-center panel — small, non-blocking. Players can still
            // run around and interact with anything underneath it.
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(groupGo.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0); panelRt.anchorMax = new Vector2(0.5f, 0);
            panelRt.pivot = new Vector2(0.5f, 0);
            panelRt.sizeDelta = new Vector2(640, 96);
            panelRt.anchoredPosition = new Vector2(0, 200);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = PanelBg;
            var panelOl = panelGo.AddComponent<Outline>();
            panelOl.effectColor = AccentDim;
            panelOl.effectDistance = new Vector2(1, 1);

            // Top eyebrow
            AddText(panelGo.transform, "Eyebrow", "—  T U T O R I A L  —",
                font, 11, FontStyle.Normal, TextAnchor.MiddleLeft, AccentGold,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(18, -14), new Vector2(240, 18));

            _stepNumberText = AddText(panelGo.transform, "Step", "STEP 1 / 6",
                font, 11, FontStyle.Bold, TextAnchor.MiddleRight, AccentGold,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-18, -14), new Vector2(180, 18));

            _stepText = AddText(panelGo.transform, "Body", "",
                font, 15, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 4), new Vector2(600, 28));

            // Progress bar
            var barBg = new GameObject("BarBg");
            barBg.transform.SetParent(panelGo.transform, false);
            var bbRt = barBg.AddComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0, 0); bbRt.anchorMax = new Vector2(1, 0);
            bbRt.pivot = new Vector2(0.5f, 0);
            bbRt.offsetMin = new Vector2(18, 0); bbRt.offsetMax = new Vector2(-130, 0);
            bbRt.sizeDelta = new Vector2(0, 6);
            bbRt.anchoredPosition = new Vector2(0, 16);
            _progressBg = barBg.AddComponent<Image>();
            _progressBg.color = new Color(0.06f, 0.05f, 0.04f, 0.95f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(barBg.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            _progressFill = fillGo.AddComponent<Image>();
            _progressFill.color = AccentGold;
            _progressFill.type = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Horizontal;
            _progressFill.fillAmount = 0f;

            // Skip button (right side)
            var skipGo = new GameObject("SkipBtn");
            skipGo.transform.SetParent(panelGo.transform, false);
            var skipRt = skipGo.AddComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(1, 0); skipRt.anchorMax = new Vector2(1, 0);
            skipRt.pivot = new Vector2(1, 0);
            skipRt.anchoredPosition = new Vector2(-18, 12);
            skipRt.sizeDelta = new Vector2(96, 24);
            var skipImg = skipGo.AddComponent<Image>();
            skipImg.color = new Color(0.20f, 0.16f, 0.14f, 1f);
            var skipOl = skipGo.AddComponent<Outline>();
            skipOl.effectColor = AccentDim;
            skipOl.effectDistance = new Vector2(1, 1);
            var skipBtn = skipGo.AddComponent<Button>();
            skipBtn.onClick.AddListener(() => TutorialSystem.Instance?.Skip());
            AddText(skipGo.transform, "L", "건너뛰기",
                font, 11, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        // ──────────────────── REFRESH ────────────────────

        private void Refresh()
        {
            var ts = TutorialSystem.Instance;
            if (ts == null) { Hide(); return; }
            if (!ts.Active) { Hide(); return; }
            Show();
            if (_stepText != null) _stepText.text = ts.CurrentLabel;
            if (_stepNumberText != null) _stepNumberText.text = $"STEP {ts.Current + 1} / {ts.Total}";
            if (_progressFill != null)
                _progressFill.fillAmount = ts.Total > 0
                    ? (ts.Current + 1) / (float)ts.Total
                    : 0f;
        }

        private void Show()
        {
            _visible = true;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
        }

        private void Hide()
        {
            _visible = false;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
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
