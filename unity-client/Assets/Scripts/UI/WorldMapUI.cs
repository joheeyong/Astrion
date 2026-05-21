using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Astrion.UI
{
    /// In-game world map overlay. M to toggle. Restyled to match the
    /// LoginPanel signature: dark-leather panel + gold outline + accent
    /// lines + corner squares + eyebrow text. Same visual language as the
    /// login / character-select screens and the in-game HUD panels.
    ///
    /// DDOL singleton — built once at start, survives every scene reload.
    public class WorldMapUI : MonoBehaviour
    {
        public static WorldMapUI Instance { get; private set; }

        // Palette matching ProjectSetup's medieval theme.
        private static readonly Color PanelBg       = new Color(0.10f, 0.08f, 0.06f, 0.94f);
        private static readonly Color PanelInner    = new Color(0.13f, 0.10f, 0.07f, 0.78f);
        private static readonly Color AccentGold    = new Color(0.85f, 0.65f, 0.22f, 1f);
        private static readonly Color AccentGoldDim = new Color(0.85f, 0.65f, 0.22f, 0.30f);
        private static readonly Color AccentGoldFaint = new Color(0.85f, 0.72f, 0.40f, 0.15f);
        private static readonly Color TextLight     = new Color(0.94f, 0.88f, 0.74f);
        private static readonly Color TextMuted     = new Color(0.62f, 0.55f, 0.42f);
        private static readonly Color CityNodeBg    = new Color(0.20f, 0.14f, 0.08f, 1f);
        private static readonly Color HuntNodeBg    = new Color(0.16f, 0.13f, 0.10f, 1f);
        private static readonly Color HaloGold      = new Color(0.95f, 0.78f, 0.30f, 0.65f);
        private static readonly Color EdgeColor     = new Color(0.55f, 0.42f, 0.22f, 0.65f);

        private CanvasGroup _group;
        private RectTransform _mapArea;
        private Text _statusText;
        private RectTransform _highlightRing;
        private Outline _highlightOutline;
        private bool _open;

        private const float PanelW = 1000f;
        private const float PanelH = 600f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) Toggle();
            if (_open && Input.GetKeyDown(KeyCode.Escape)) Toggle();

            // Halo subtle pulse — keeps the same heartbeat as the previous
            // version but using the LoginPanel gold instead of a parchment hue.
            if (_open && _highlightRing != null && _highlightRing.gameObject.activeSelf)
            {
                float t = 0.40f + 0.35f * Mathf.Sin(Time.unscaledTime * 3.4f);
                var c = HaloGold; c.a = t;
                var img = _highlightRing.GetComponent<Image>();
                if (img != null) img.color = c;
            }
        }

        private void OnSceneChanged(Scene prev, Scene next) => UpdateCurrentHighlight();

        private void Toggle()
        {
            _open = !_open;
            _group.alpha = _open ? 1f : 0f;
            _group.blocksRaycasts = _open;
            if (_open) UpdateCurrentHighlight();
        }

        // ────────────────────────────────────────────────────────────────────
        // BUILD
        // ────────────────────────────────────────────────────────────────────

        private void Build()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ── Canvas ───────────────────────────────────────────────────────
            var canvasGo = new GameObject("WorldMapCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9000;
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

            // Dim backdrop — click-blocking so the world doesn't react.
            var dimGo = new GameObject("Dim");
            dimGo.transform.SetParent(groupGo.transform, false);
            var dimRt = dimGo.AddComponent<RectTransform>();
            Stretch(dimRt);
            var dim = dimGo.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.62f);

            // ── Outer panel (LoginPanel signature) ───────────────────────────
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(groupGo.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(PanelW, PanelH);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = PanelBg;
            var panelOutline = panelGo.AddComponent<Outline>();
            panelOutline.effectColor = AccentGoldDim;
            panelOutline.effectDistance = new Vector2(1, 1);

            // ── Inner panel (12 px inset, slightly lighter — depth) ─────────
            var innerGo = new GameObject("InnerPanel");
            innerGo.transform.SetParent(panelGo.transform, false);
            var innerRt = innerGo.AddComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(12, 12);
            innerRt.offsetMax = new Vector2(-12, -12);
            var inner = innerGo.AddComponent<Image>();
            inner.color = PanelInner;
            inner.raycastTarget = false;

            // Top + bottom gold accent lines (central 70%).
            AddAccentLine(panelGo.transform, new Vector2(0.15f, 1f), new Vector2(0.85f, 1f), AccentGold, 2);
            AddAccentLine(panelGo.transform, new Vector2(0.15f, 0f), new Vector2(0.85f, 0f), AccentGoldFaint, 1);

            // Four corner deco.
            AddCornerDeco(panelGo.transform, new Vector2(0, 1), new Vector2( 8, -8));
            AddCornerDeco(panelGo.transform, new Vector2(1, 1), new Vector2(-8, -8));
            AddCornerDeco(panelGo.transform, new Vector2(0, 0), new Vector2( 8,  8));
            AddCornerDeco(panelGo.transform, new Vector2(1, 0), new Vector2(-8,  8));

            // ── Eyebrow + Title (LoginPanel style) ──────────────────────────
            var eyebrowGo = new GameObject("Eyebrow");
            eyebrowGo.transform.SetParent(panelGo.transform, false);
            var ebRt = eyebrowGo.AddComponent<RectTransform>();
            ebRt.anchorMin = new Vector2(0.5f, 1f);
            ebRt.anchorMax = new Vector2(0.5f, 1f);
            ebRt.pivot = new Vector2(0.5f, 1f);
            ebRt.sizeDelta = new Vector2(420, 22);
            ebRt.anchoredPosition = new Vector2(0, -22);
            var ebText = eyebrowGo.AddComponent<Text>();
            ebText.font = font;
            ebText.fontSize = 11;
            ebText.alignment = TextAnchor.MiddleCenter;
            ebText.color = AccentGold;
            ebText.text = "—  T H E   A S T R A L   V E I L  —";

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(600, 42);
            titleRt.anchoredPosition = new Vector2(0, -44);
            var title = titleGo.AddComponent<Text>();
            title.font = font;
            title.fontSize = 28;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = TextLight;
            title.text = "WORLD MAP";

            // Thin gold underline below the title.
            var tlGo = new GameObject("TitleLine");
            tlGo.transform.SetParent(panelGo.transform, false);
            var tlRt = tlGo.AddComponent<RectTransform>();
            tlRt.anchorMin = new Vector2(0.5f, 1f);
            tlRt.anchorMax = new Vector2(0.5f, 1f);
            tlRt.pivot = new Vector2(0.5f, 1f);
            tlRt.sizeDelta = new Vector2(140, 1);
            tlRt.anchoredPosition = new Vector2(0, -88);
            var tlImg = tlGo.AddComponent<Image>();
            tlImg.color = new Color(0.85f, 0.72f, 0.40f, 0.45f);
            tlImg.raycastTarget = false;

            // ── Status banner (bottom) ──────────────────────────────────────
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(panelGo.transform, false);
            var stRt = statusGo.AddComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0, 0);
            stRt.anchorMax = new Vector2(1, 0);
            stRt.pivot = new Vector2(0.5f, 0);
            stRt.sizeDelta = new Vector2(0, 24);
            stRt.anchoredPosition = new Vector2(0, 20);
            _statusText = statusGo.AddComponent<Text>();
            _statusText.font = font;
            _statusText.fontSize = 12;
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = TextMuted;
            _statusText.text = "press [M] to close";

            // ── Map drawing area ─────────────────────────────────────────────
            var mapGo = new GameObject("MapArea");
            mapGo.transform.SetParent(panelGo.transform, false);
            _mapArea = mapGo.AddComponent<RectTransform>();
            _mapArea.anchorMin = _mapArea.anchorMax = new Vector2(0.5f, 0.5f);
            _mapArea.pivot = new Vector2(0.5f, 0.5f);
            _mapArea.sizeDelta = new Vector2(PanelW - 100, PanelH - 200);
            _mapArea.anchoredPosition = new Vector2(0, -18);

            // Portal lines (background layer).
            foreach (var (a, b) in WorldMapData.Edges)
            {
                int ai = WorldMapData.NodeIndex(a);
                int bi = WorldMapData.NodeIndex(b);
                if (ai < 0 || bi < 0) continue;
                DrawEdge(_mapArea, WorldMapData.Nodes[ai].pos, WorldMapData.Nodes[bi].pos);
            }

            // Pulsing halo (under nodes; repositioned per current zone).
            var ringGo = new GameObject("Highlight");
            ringGo.transform.SetParent(_mapArea, false);
            _highlightRing = ringGo.AddComponent<RectTransform>();
            _highlightRing.anchorMin = _highlightRing.anchorMax = new Vector2(0.5f, 0.5f);
            _highlightRing.pivot = new Vector2(0.5f, 0.5f);
            _highlightRing.sizeDelta = new Vector2(88, 44);
            var haloImg = ringGo.AddComponent<Image>();
            haloImg.color = HaloGold;
            haloImg.raycastTarget = false;
            _highlightOutline = ringGo.AddComponent<Outline>();
            _highlightOutline.effectColor = AccentGold;
            _highlightOutline.effectDistance = new Vector2(1, 1);
            ringGo.SetActive(false);

            // Nodes (top layer).
            for (int i = 0; i < WorldMapData.Nodes.Length; i++)
            {
                var n = WorldMapData.Nodes[i];
                if (n.isCity) DrawCityNode(_mapArea, n, font);
                else          DrawHuntingNode(_mapArea, n, font);
            }

            UpdateCurrentHighlight();
        }

        private void DrawCityNode(RectTransform parent, WorldMapData.Node n, Font font)
        {
            // City — slightly larger box, gold-rimmed (via Outline), gold label.
            var go = new GameObject($"City_{n.id}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(86, 28);
            rt.anchoredPosition = n.pos;
            var bg = go.AddComponent<Image>();
            bg.color = CityNodeBg;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = AccentGold;
            ol.effectDistance = new Vector2(1, 1);

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lblRt = lblGo.AddComponent<RectTransform>();
            Stretch(lblRt);
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = font;
            lbl.fontSize = 11;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = AccentGold;
            lbl.text = n.display;

            DrawLevelTag(parent, n.pos + new Vector2(0, -22), n, font);
        }

        private void DrawHuntingNode(RectTransform parent, WorldMapData.Node n, Font font)
        {
            // Hunting — small dark box with a faint gold rim and parchment label.
            var go = new GameObject($"Hunt_{n.id}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(80, 22);
            rt.anchoredPosition = n.pos;
            var bg = go.AddComponent<Image>();
            bg.color = HuntNodeBg;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = AccentGoldDim;
            ol.effectDistance = new Vector2(1, 1);

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lblRt = lblGo.AddComponent<RectTransform>();
            Stretch(lblRt);
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = font;
            lbl.fontSize = 10;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = TextLight;
            lbl.text = n.display;

            DrawLevelTag(parent, n.pos + new Vector2(0, -18), n, font);
        }

        private void DrawLevelTag(RectTransform parent, Vector2 pos, WorldMapData.Node n, Font font)
        {
            var go = new GameObject("Lv");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(86, 12);
            rt.anchoredPosition = pos;
            var lv = go.AddComponent<Text>();
            lv.font = font;
            lv.fontSize = 9;
            lv.alignment = TextAnchor.MiddleCenter;
            lv.color = TextMuted;
            lv.text = $"Lv. {n.minLv}–{n.maxLv}";
        }

        /// Single rotated rectangle for a portal edge — no LineRenderer dep.
        private void DrawEdge(RectTransform parent, Vector2 a, Vector2 b)
        {
            var go = new GameObject("Edge");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            Vector2 mid = (a + b) * 0.5f;
            Vector2 d = b - a;
            float len = d.magnitude;
            float deg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            rt.sizeDelta = new Vector2(len, 2f);
            rt.anchoredPosition = mid;
            rt.localRotation = Quaternion.Euler(0, 0, deg);
            var img = go.AddComponent<Image>();
            img.color = EdgeColor;
            img.raycastTarget = false;
        }

        private void UpdateCurrentHighlight()
        {
            string scene = SceneManager.GetActiveScene().name;
            string zoneId = Astrion.Network.SceneZoneMap.SceneToZone(scene);
            int idx = WorldMapData.NodeIndex(zoneId);

            if (idx < 0 || _highlightRing == null)
            {
                if (_highlightRing != null) _highlightRing.gameObject.SetActive(false);
                if (_statusText != null) _statusText.text = "(uncharted region)   press [M] to close";
                return;
            }

            _highlightRing.gameObject.SetActive(true);
            _highlightRing.anchoredPosition = WorldMapData.Nodes[idx].pos;

            var n = WorldMapData.Nodes[idx];
            string kind = n.isCity ? "city" : "wilds";
            _statusText.text = $"current:  {n.display}   ·   {kind}   ·   Lv. {n.minLv}–{n.maxLv}   ·   press [M] to close";
        }

        // ────────────────────────────────────────────────────────────────────
        // PRIMITIVE HELPERS
        // ────────────────────────────────────────────────────────────────────

        private static void AddAccentLine(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color, float thickness)
        {
            var go = new GameObject("AccentLine");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, thickness);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static void AddCornerDeco(Transform parent, Vector2 anchor, Vector2 offset)
        {
            var go = new GameObject("CornerDeco");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
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
