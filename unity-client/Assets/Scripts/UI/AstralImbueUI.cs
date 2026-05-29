using UnityEngine;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    /// Modal panel for the Star Sage. Four rows, each showing a permanent
    /// stat boost the player can buy with stardust. Caps scale with character
    /// level so the buttons gate themselves automatically.
    ///
    /// DDOL singleton — built once when networkGo wakes up, survives every
    /// scene reload. Same lifecycle pattern as WorldMapUI.
    public class AstralImbueUI : MonoBehaviour
    {
        public static AstralImbueUI Instance { get; private set; }

        // Palette matches the LoginPanel / WorldMapUI signature so the panel
        // reads as part of the same visual language.
        private static readonly Color PanelBg       = new Color(0.10f, 0.08f, 0.06f, 0.96f);
        private static readonly Color PanelInner    = new Color(0.13f, 0.10f, 0.07f, 0.85f);
        private static readonly Color AccentGold    = new Color(0.85f, 0.65f, 0.22f, 1f);
        private static readonly Color AccentGoldDim = new Color(0.85f, 0.65f, 0.22f, 0.30f);
        private static readonly Color AccentViolet  = new Color(0.78f, 0.42f, 0.92f, 1f);
        private static readonly Color TextLight     = new Color(0.94f, 0.88f, 0.74f);
        private static readonly Color TextMuted     = new Color(0.62f, 0.55f, 0.42f);
        private static readonly Color RowBg         = new Color(0.16f, 0.13f, 0.10f, 1f);
        private static readonly Color BtnBg         = new Color(0.32f, 0.22f, 0.40f, 1f);
        private static readonly Color BtnBgDisabled = new Color(0.18f, 0.16f, 0.16f, 1f);

        private CanvasGroup _group;
        private bool _open;

        // Per-row references rebuilt by Refresh() each time the panel opens.
        private class Row
        {
            public string kind;
            public Text current;
            public Text cap;
            public Text cost;
            public Button button;
            public Image buttonBg;
            public Text buttonLbl;
        }
        private Row[] _rows;
        private Text _stardustText;
        private Text _statusText;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.OnChanged += OnStatsChanged;
            if (InventorySystem.Instance != null)
                InventorySystem.Instance.OnChanged += OnInventoryChanged;
        }

        private void OnDestroy()
        {
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.OnChanged -= OnStatsChanged;
            if (InventorySystem.Instance != null)
                InventorySystem.Instance.OnChanged -= OnInventoryChanged;
            if (Instance == this) Instance = null;
        }

        private void OnStatsChanged() { if (_open) Refresh(); }
        private void OnInventoryChanged() { if (_open) Refresh(); }

        public void Open()
        {
            _open = true;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
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
            if (_open && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        // ───────────────────────── BUILD ─────────────────────────

        private void Build()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("AstralImbueCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9100; // above WorldMap (9000), below toasts
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

            // Click-blocking dim backdrop.
            var dimGo = new GameObject("Dim");
            dimGo.transform.SetParent(groupGo.transform, false);
            var dimRt = dimGo.AddComponent<RectTransform>();
            Stretch(dimRt);
            var dim = dimGo.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.65f);

            // Outer panel — same LoginPanel signature.
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(groupGo.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(620, 520);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = PanelBg;
            var panelOutline = panelGo.AddComponent<Outline>();
            panelOutline.effectColor = AccentGoldDim;
            panelOutline.effectDistance = new Vector2(1, 1);

            // Inner inset.
            var innerGo = new GameObject("Inner");
            innerGo.transform.SetParent(panelGo.transform, false);
            var innerRt = innerGo.AddComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(10, 10);
            innerRt.offsetMax = new Vector2(-10, -10);
            var innerImg = innerGo.AddComponent<Image>();
            innerImg.color = PanelInner;
            innerImg.raycastTarget = false;

            AddAccentLine(panelGo.transform, new Vector2(0.15f, 1f), new Vector2(0.85f, 1f), AccentGold, 2);
            AddAccentLine(panelGo.transform, new Vector2(0.15f, 0f), new Vector2(0.85f, 0f),
                new Color(0.85f, 0.72f, 0.40f, 0.18f), 1);
            AddCornerDeco(panelGo.transform, new Vector2(0, 1), new Vector2(8, -8));
            AddCornerDeco(panelGo.transform, new Vector2(1, 1), new Vector2(-8, -8));
            AddCornerDeco(panelGo.transform, new Vector2(0, 0), new Vector2(8, 8));
            AddCornerDeco(panelGo.transform, new Vector2(1, 0), new Vector2(-8, 8));

            // Eyebrow + title.
            AddText(panelGo.transform, "Eyebrow", "—  A S T R A L   I M B U E  —",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, AccentGold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -22), new Vector2(420, 22));

            AddText(panelGo.transform, "Title", "★  별빛 각인",
                font, 26, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -50), new Vector2(560, 38));

            // Thin underline.
            var ul = new GameObject("TitleLine");
            ul.transform.SetParent(panelGo.transform, false);
            var ulRt = ul.AddComponent<RectTransform>();
            ulRt.anchorMin = ulRt.anchorMax = new Vector2(0.5f, 1f);
            ulRt.pivot = new Vector2(0.5f, 1f);
            ulRt.sizeDelta = new Vector2(130, 1);
            ulRt.anchoredPosition = new Vector2(0, -82);
            var ulImg = ul.AddComponent<Image>();
            ulImg.color = new Color(0.85f, 0.72f, 0.40f, 0.45f);
            ulImg.raycastTarget = false;

            // 4 rows.
            _rows = new Row[4];
            string[] kinds  = { "atk",       "hp",         "mp",         "crit" };
            string[] titles = { "공격력",     "활력",        "정신력",       "별의 가호" };
            string[] gains  = { "+1 공격력",  "+20 최대 HP", "+10 최대 MP", "+1% 크리티컬" };
            for (int i = 0; i < 4; i++)
                _rows[i] = BuildRow(panelGo.transform, font, i, kinds[i], titles[i], gains[i]);

            // Footer — current stardust, status text, close button.
            _stardustText = AddText(panelGo.transform, "StardustCount", "별 가루: 0",
                font, 14, FontStyle.Bold, TextAnchor.MiddleLeft, AccentGold,
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(30, 70), new Vector2(240, 22));

            _statusText = AddText(panelGo.transform, "Status",
                "별 가루를 봉헌하여 영원한 가호를 새기시오.",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, TextMuted,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(0, 38), new Vector2(0, 18));

            BuildCloseButton(panelGo.transform, font);
        }

        private Row BuildRow(Transform parent, Font font, int idx, string kind, string title, string gainText)
        {
            float rowTop = -100f;
            float rowH = 56f;
            float rowGap = 8f;
            float y = rowTop - idx * (rowH + rowGap);

            var rowGo = new GameObject($"Row_{kind}");
            rowGo.transform.SetParent(parent, false);
            var rowRt = rowGo.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 1);
            rowRt.anchorMax = new Vector2(1, 1);
            rowRt.pivot = new Vector2(0.5f, 1);
            rowRt.offsetMin = new Vector2(30, 0);
            rowRt.offsetMax = new Vector2(-30, 0);
            rowRt.sizeDelta = new Vector2(0, rowH);
            rowRt.anchoredPosition = new Vector2(0, y);
            var rowBg = rowGo.AddComponent<Image>();
            rowBg.color = RowBg;
            var rowOl = rowGo.AddComponent<Outline>();
            rowOl.effectColor = AccentGoldDim;
            rowOl.effectDistance = new Vector2(1, 1);

            var titleT = AddText(rowGo.transform, "Title", title,
                font, 14, FontStyle.Bold, TextAnchor.MiddleLeft, AccentGold,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(16, 10), new Vector2(140, 22));

            var gainT = AddText(rowGo.transform, "Gain", gainText,
                font, 11, FontStyle.Normal, TextAnchor.MiddleLeft, TextLight,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(16, -12), new Vector2(160, 16));

            var curT = AddText(rowGo.transform, "Current", "현재  +0",
                font, 12, FontStyle.Bold, TextAnchor.MiddleLeft, TextLight,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(200, 10), new Vector2(110, 18));

            var capT = AddText(rowGo.transform, "Cap", "최대  0",
                font, 11, FontStyle.Normal, TextAnchor.MiddleLeft, TextMuted,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(200, -10), new Vector2(110, 16));

            var costT = AddText(rowGo.transform, "Cost", "별 가루 0",
                font, 12, FontStyle.Normal, TextAnchor.MiddleCenter, AccentGold,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-180, 0), new Vector2(130, 22));

            // Button
            var btnGo = new GameObject("ImbueB");
            btnGo.transform.SetParent(rowGo.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(1, 0.5f);
            btnRt.pivot = new Vector2(1, 0.5f);
            btnRt.anchoredPosition = new Vector2(-16, 0);
            btnRt.sizeDelta = new Vector2(140, 34);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = BtnBg;
            var btnOl = btnGo.AddComponent<Outline>();
            btnOl.effectColor = new Color(0.95f, 0.78f, 0.30f, 0.35f);
            btnOl.effectDistance = new Vector2(1, 1);
            var btn = btnGo.AddComponent<Button>();
            var lblT = AddText(btnGo.transform, "L", "▶  봉헌",
                font, 13, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.95f, 0.85f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            string captureKind = kind;
            btn.onClick.AddListener(() => OnImbueClicked(captureKind));

            return new Row {
                kind = kind, current = curT, cap = capT, cost = costT,
                button = btn, buttonBg = btnImg, buttonLbl = lblT,
            };
        }

        private void BuildCloseButton(Transform parent, Font font)
        {
            var btnGo = new GameObject("CloseB");
            btnGo.transform.SetParent(parent, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(1, 0);
            btnRt.pivot = new Vector2(1, 0);
            btnRt.anchoredPosition = new Vector2(-30, 22);
            btnRt.sizeDelta = new Vector2(120, 34);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.20f, 0.16f, 0.14f, 1f);
            var btnOl = btnGo.AddComponent<Outline>();
            btnOl.effectColor = AccentGoldDim;
            btnOl.effectDistance = new Vector2(1, 1);
            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(Close);
            AddText(btnGo.transform, "L", "닫기  [Esc]",
                font, 12, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
        }

        // ───────────────────────── REFRESH ─────────────────────────

        private void Refresh()
        {
            var stats = PlayerStats.Instance;
            var inv   = InventorySystem.Instance;
            if (stats == null || inv == null || _rows == null) return;

            int stardust = inv.CountOf("stardust");
            if (_stardustText != null) _stardustText.text = $"별 가루 보유: {stardust}";

            foreach (var r in _rows)
            {
                int cur = stats.ImbueCurrent(r.kind);
                int cap = stats.ImbueCap(r.kind);
                int cost = stats.ImbueCost(r.kind);
                if (r.current != null) r.current.text = $"현재  +{FormatBonus(r.kind, cur)}";
                if (r.cap     != null) r.cap.text     = $"최대  +{FormatBonus(r.kind, cap)}";
                if (r.cost    != null) r.cost.text    = $"별 가루 {cost}";

                bool atCap = cur >= cap;
                bool poor  = stardust < cost;
                bool disabled = atCap || poor;
                r.button.interactable = !disabled;
                if (r.buttonBg != null)
                    r.buttonBg.color = disabled ? BtnBgDisabled : BtnBg;
                if (r.buttonLbl != null)
                    r.buttonLbl.text = atCap ? "최대치" : (poor ? "재료 부족" : "▶  봉헌");
            }
        }

        private static string FormatBonus(string kind, int tier)
        {
            switch (kind)
            {
                case "atk":  return $"{tier} 공격력";
                case "hp":   return $"{tier * 20} HP";
                case "mp":   return $"{tier * 10} MP";
                case "crit": return $"{tier}% 크리";
                default:     return tier.ToString();
            }
        }

        private void OnImbueClicked(string kind)
        {
            var stats = PlayerStats.Instance;
            if (stats == null) return;
            stats.TryAstralImbue(kind);
            // PlayerStats.OnChanged will fire Refresh.
        }

        // ───────────────────── PRIMITIVE HELPERS ─────────────────────

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
            // Stretch on either axis when min/max differ.
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
