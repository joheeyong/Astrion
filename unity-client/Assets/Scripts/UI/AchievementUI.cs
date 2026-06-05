using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Astrion.Game;

namespace Astrion.UI
{
    /// Trophy panel — every defined achievement in a vertical list with
    /// status (unlocked / locked), progress bar against current counter,
    /// reward label, and a small dot/cross on the left. Toggle with T.
    /// Pulls fresh data each Open() so progress bars are current.
    /// DDOL singleton on networkGo (LoginScene bootstrap).
    public class AchievementUI : MonoBehaviour
    {
        public static AchievementUI Instance { get; private set; }

        private static readonly Color PanelBg     = new Color(0.10f, 0.08f, 0.06f, 0.96f);
        private static readonly Color PanelInner  = new Color(0.13f, 0.10f, 0.07f, 0.85f);
        private static readonly Color AccentGold  = new Color(0.85f, 0.65f, 0.22f, 1f);
        private static readonly Color AccentDim   = new Color(0.85f, 0.65f, 0.22f, 0.30f);
        private static readonly Color TextLight   = new Color(0.94f, 0.88f, 0.74f);
        private static readonly Color TextMuted   = new Color(0.62f, 0.55f, 0.42f);
        private static readonly Color RowLocked   = new Color(0.14f, 0.12f, 0.09f, 1f);
        private static readonly Color RowUnlocked = new Color(0.25f, 0.20f, 0.10f, 1f);
        private static readonly Color BarFull     = new Color(0.95f, 0.78f, 0.30f);
        private static readonly Color BarPartial  = new Color(0.55f, 0.45f, 0.22f);
        private static readonly Color StatusOk    = new Color(0.55f, 0.85f, 0.45f);

        private CanvasGroup _group;
        private bool _open;
        private RectTransform _listRoot;
        private Text _summaryText;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
        }

        private void Start()
        {
            if (AchievementSystem.Instance != null)
                AchievementSystem.Instance.OnListUpdated += Refresh;
        }

        private void OnDestroy()
        {
            if (AchievementSystem.Instance != null)
                AchievementSystem.Instance.OnListUpdated -= Refresh;
            if (Instance == this) Instance = null;
        }

        public void Toggle() { if (_open) Close(); else Open(); }

        public void Open()
        {
            _open = true;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            AchievementSystem.Instance?.RequestList();
            Refresh();
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
            if (Input.GetKeyDown(KeyCode.T)) Toggle();
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

            var canvasGo = new GameObject("AchievementCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9140;
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
            dim.color = new Color(0, 0, 0, 0.65f);

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(groupGo.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(760, 720);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = PanelBg;
            var panelOl = panelGo.AddComponent<Outline>();
            panelOl.effectColor = AccentDim;
            panelOl.effectDistance = new Vector2(1, 1);

            AddCornerDeco(panelGo.transform, new Vector2(0, 1), new Vector2(8, -8));
            AddCornerDeco(panelGo.transform, new Vector2(1, 1), new Vector2(-8, -8));
            AddCornerDeco(panelGo.transform, new Vector2(0, 0), new Vector2(8, 8));
            AddCornerDeco(panelGo.transform, new Vector2(1, 0), new Vector2(-8, 8));
            AddAccentLine(panelGo.transform, new Vector2(0.15f, 1f), new Vector2(0.85f, 1f), AccentGold, 2);

            AddText(panelGo.transform, "Eyebrow", "—  A C H I E V E M E N T S  —",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, AccentGold,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -22), new Vector2(420, 22));
            AddText(panelGo.transform, "Title", "★  업적",
                font, 24, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -52), new Vector2(560, 36));
            _summaryText = AddText(panelGo.transform, "Summary", "",
                font, 12, FontStyle.Normal, TextAnchor.MiddleCenter, TextMuted,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -88), new Vector2(560, 18));

            // Scrollable list
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0); scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(30, 70); scrollRt.offsetMax = new Vector2(-30, -110);
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = PanelInner;
            scrollImg.raycastTarget = true;
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var maskGo = new GameObject("Viewport");
            maskGo.transform.SetParent(scrollGo.transform, false);
            var maskRt = maskGo.AddComponent<RectTransform>();
            maskRt.anchorMin = Vector2.zero; maskRt.anchorMax = Vector2.one;
            maskRt.offsetMin = new Vector2(2, 2); maskRt.offsetMax = new Vector2(-2, -2);
            var maskImg = maskGo.AddComponent<Image>();
            maskImg.color = new Color(0, 0, 0, 0.01f);
            maskGo.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = maskRt;

            _listRoot = new GameObject("Content").AddComponent<RectTransform>();
            _listRoot.transform.SetParent(maskGo.transform, false);
            _listRoot.anchorMin = new Vector2(0, 1); _listRoot.anchorMax = new Vector2(1, 1);
            _listRoot.pivot = new Vector2(0.5f, 1);
            _listRoot.offsetMin = Vector2.zero; _listRoot.offsetMax = Vector2.zero;
            _listRoot.sizeDelta = new Vector2(0, 100);
            _listRoot.anchoredPosition = Vector2.zero;
            scroll.content = _listRoot;

            BuildCloseButton(panelGo.transform, font);
        }

        private void BuildCloseButton(Transform parent, Font font)
        {
            var go = new GameObject("CloseB");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-30, 22);
            rt.sizeDelta = new Vector2(120, 32);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.20f, 0.16f, 0.14f, 1f);
            var ol = go.AddComponent<Outline>();
            ol.effectColor = AccentDim;
            ol.effectDistance = new Vector2(1, 1);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(Close);
            AddText(go.transform, "L", "닫기  [Esc]",
                font, 12, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        // ──────────────────── REFRESH ────────────────────

        private void Refresh()
        {
            if (_listRoot == null) return;
            for (int i = _listRoot.childCount - 1; i >= 0; i--)
                Destroy(_listRoot.GetChild(i).gameObject);

            var sys = AchievementSystem.Instance;
            int unlocked = 0;
            int total = AchievementDatabase.All.Count;
            if (sys != null) foreach (var d in AchievementDatabase.All) if (sys.IsUnlocked(d.id)) unlocked++;

            if (_summaryText != null)
                _summaryText.text = $"진행도  ·  {unlocked}/{total}";

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            float y = 4f;
            const float rowH = 58f;
            const float rowGap = 6f;
            foreach (var d in AchievementDatabase.All)
            {
                bool ok = sys != null && sys.IsUnlocked(d.id);
                long cur = AchievementDatabase.CurrentValue(d, sys?.CurrentProgress);
                BuildRow(_listRoot, font, d, ok, cur, y);
                y += rowH + rowGap;
            }
            _listRoot.sizeDelta = new Vector2(0, y + 8f);
        }

        private void BuildRow(RectTransform parent, Font font, AchievementDatabase.Def d,
                               bool unlocked, long current, float yOffset)
        {
            var go = new GameObject($"Row_{d.id}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(8, 0); rt.offsetMax = new Vector2(-8, 0);
            rt.sizeDelta = new Vector2(0, 58);
            rt.anchoredPosition = new Vector2(0, -yOffset);
            var bg = go.AddComponent<Image>();
            bg.color = unlocked ? RowUnlocked : RowLocked;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = unlocked ? AccentDim : new Color(0.30f, 0.24f, 0.16f, 0.4f);
            ol.effectDistance = new Vector2(1, 1);

            // Status badge — ★ for unlocked, ○ for locked.
            AddText(go.transform, "Badge", unlocked ? "★" : "○",
                font, 22, FontStyle.Bold, TextAnchor.MiddleCenter,
                unlocked ? StatusOk : TextMuted,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(20, 0), new Vector2(30, 0));

            AddText(go.transform, "Name", d.displayName,
                font, 14, FontStyle.Bold, TextAnchor.MiddleLeft,
                unlocked ? AccentGold : TextLight,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(54, -8), new Vector2(360, 20));

            AddText(go.transform, "Desc", d.description,
                font, 11, FontStyle.Normal, TextAnchor.MiddleLeft, TextMuted,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(54, -28), new Vector2(360, 16));

            // Reward
            string rewardLabel = d.rewardItemId switch
            {
                "stardust" => $"+{d.rewardQty} 별 가루",
                _ => $"+{d.rewardQty}",
            };
            AddText(go.transform, "Reward", rewardLabel,
                font, 11, FontStyle.Bold, TextAnchor.MiddleRight, AccentGold,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-12, -10), new Vector2(180, 16));

            // Progress bar (bottom-right)
            float ratio = d.target > 0 ? Mathf.Clamp01((float)current / d.target) : 0f;
            string progLabel = unlocked
                ? "완료"
                : $"{current:N0}/{d.target:N0}";
            AddText(go.transform, "ProgLbl", progLabel,
                font, 10, FontStyle.Normal, TextAnchor.MiddleRight,
                unlocked ? StatusOk : TextMuted,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-12, -30), new Vector2(180, 14));

            var barBgGo = new GameObject("BarBg");
            barBgGo.transform.SetParent(go.transform, false);
            var barBgRt = barBgGo.AddComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(1, 0); barBgRt.anchorMax = new Vector2(1, 0);
            barBgRt.pivot = new Vector2(1, 0);
            barBgRt.anchoredPosition = new Vector2(-12, 8);
            barBgRt.sizeDelta = new Vector2(180, 6);
            var barBg = barBgGo.AddComponent<Image>();
            barBg.color = new Color(0, 0, 0, 0.55f);
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(barBgGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(1, 1); fillRt.offsetMax = new Vector2(-1, -1);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = unlocked ? BarFull : BarPartial;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = unlocked ? 1f : ratio;
        }

        // ──────────────────── PRIMITIVES ────────────────────

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
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }

        private static void AddAccentLine(Transform parent, Vector2 a, Vector2 b, Color c, float h)
        {
            var go = new GameObject("AccentLine");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = a; rt.anchorMax = b;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, h);
            var img = go.AddComponent<Image>();
            img.color = c; img.raycastTarget = false;
        }
        private static void AddCornerDeco(Transform parent, Vector2 anchor, Vector2 offset)
        {
            var go = new GameObject("Corner");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(6, 6);
            var img = go.AddComponent<Image>();
            img.color = AccentGold;
            img.raycastTarget = false;
        }
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
