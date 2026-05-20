using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Astrion.UI
{
    /// In-game world map overlay. M to toggle. Draws every zone as a labelled
    /// node and the portal graph as line segments between them; the node
    /// matching the active scene gets a bright highlight ring.
    ///
    /// DDOL singleton — built once at game start and stays available across
    /// scene loads. Rebuilding the canvas on every scene transition would
    /// flicker every time the player walked through a portal.
    public class WorldMapUI : MonoBehaviour
    {
        public static WorldMapUI Instance { get; private set; }

        private Canvas _canvas;
        private CanvasGroup _group;
        private RectTransform _panel;
        private RectTransform _mapArea;
        private Text _statusText;
        private Image _highlightRing;
        private bool _open;

        // Per-node UI so we can repaint the highlight on scene change.
        private RectTransform[] _nodeRects;

        private const float PanelW = 960f;
        private const float PanelH = 560f;
        private const float NodeW = 86f;
        private const float NodeH = 24f;

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
            // ESC closes if open — same as inventory/quest panels elsewhere.
            if (_open && Input.GetKeyDown(KeyCode.Escape)) Toggle();
        }

        private void OnSceneChanged(Scene prev, Scene next)
        {
            // Refresh the highlight even if the panel is closed so it's ready
            // the next time the player opens it.
            UpdateCurrentHighlight();
        }

        private void Toggle()
        {
            _open = !_open;
            _group.alpha = _open ? 1f : 0f;
            _group.blocksRaycasts = _open;
            if (_open) UpdateCurrentHighlight();
        }

        private void Build()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ── Canvas ───────────────────────────────────────────────────────
            var canvasGo = new GameObject("WorldMapCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9000;  // above HUD, below reconnect overlay
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

            // Dim backdrop — click-blocking so the world doesn't react to clicks.
            var dimGo = new GameObject("Dim");
            dimGo.transform.SetParent(groupGo.transform, false);
            var dimRt = dimGo.AddComponent<RectTransform>();
            Stretch(dimRt);
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.55f);

            // ── Panel (parchment) ────────────────────────────────────────────
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(groupGo.transform, false);
            _panel = panelGo.AddComponent<RectTransform>();
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta = new Vector2(PanelW, PanelH);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.13f, 0.10f, 0.07f, 0.96f);

            // Header
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.sizeDelta = new Vector2(0, 44);
            titleRt.anchoredPosition = new Vector2(0, 0);
            var title = titleGo.AddComponent<Text>();
            title.font = font;
            title.fontSize = 22;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.92f, 0.86f, 0.72f);
            title.text = "ASTRION  •  World Map";

            // Status (current zone) bottom-left
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(panelGo.transform, false);
            var statusRt = statusGo.AddComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0, 0);
            statusRt.anchorMax = new Vector2(1, 0);
            statusRt.pivot = new Vector2(0.5f, 0);
            statusRt.sizeDelta = new Vector2(0, 30);
            statusRt.anchoredPosition = new Vector2(0, 6);
            _statusText = statusGo.AddComponent<Text>();
            _statusText.font = font;
            _statusText.fontSize = 14;
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = new Color(0.78f, 0.72f, 0.55f);
            _statusText.text = "press M to close";

            // ── Map drawing area (inside panel) ─────────────────────────────
            var mapGo = new GameObject("MapArea");
            mapGo.transform.SetParent(panelGo.transform, false);
            _mapArea = mapGo.AddComponent<RectTransform>();
            _mapArea.anchorMin = _mapArea.anchorMax = new Vector2(0.5f, 0.5f);
            _mapArea.pivot = new Vector2(0.5f, 0.5f);
            _mapArea.sizeDelta = new Vector2(PanelW - 60, PanelH - 100);
            _mapArea.anchoredPosition = new Vector2(0, 0);

            // ── Edges (background layer) ────────────────────────────────────
            foreach (var (a, b) in WorldMapData.Edges)
            {
                int ai = WorldMapData.NodeIndex(a);
                int bi = WorldMapData.NodeIndex(b);
                if (ai < 0 || bi < 0) continue;
                DrawEdge(_mapArea, WorldMapData.Nodes[ai].pos, WorldMapData.Nodes[bi].pos);
            }

            // ── Highlight ring (under nodes, gets repositioned on scene change)
            var ringGo = new GameObject("Highlight");
            ringGo.transform.SetParent(_mapArea, false);
            var ringRt = ringGo.AddComponent<RectTransform>();
            ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
            ringRt.pivot = new Vector2(0.5f, 0.5f);
            ringRt.sizeDelta = new Vector2(NodeW + 14, NodeH + 14);
            _highlightRing = ringGo.AddComponent<Image>();
            _highlightRing.color = new Color(1f, 0.78f, 0.20f, 0.55f);  // gold halo
            ringGo.SetActive(false);

            // ── Nodes (top layer) ───────────────────────────────────────────
            _nodeRects = new RectTransform[WorldMapData.Nodes.Length];
            for (int i = 0; i < WorldMapData.Nodes.Length; i++)
            {
                var n = WorldMapData.Nodes[i];
                _nodeRects[i] = DrawNode(_mapArea, n, font);
            }

            UpdateCurrentHighlight();
        }

        private RectTransform DrawNode(RectTransform parent, WorldMapData.Node n, Font font)
        {
            var go = new GameObject($"Node_{n.id}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(NodeW, NodeH);
            rt.anchoredPosition = n.pos;
            var bg = go.AddComponent<Image>();
            bg.color = n.isCity
                ? new Color(0.42f, 0.28f, 0.15f, 1f)        // city — oak
                : new Color(0.18f, 0.18f, 0.20f, 0.92f);    // hunting — slate

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lblRt = lblGo.AddComponent<RectTransform>();
            Stretch(lblRt);
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = font;
            lbl.fontSize = 11;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = n.isCity ? new Color(1f, 0.92f, 0.65f) : new Color(0.85f, 0.85f, 0.85f);
            lbl.text = n.display;

            // Lv tag under the node
            var lvGo = new GameObject("Lv");
            lvGo.transform.SetParent(go.transform, false);
            var lvRt = lvGo.AddComponent<RectTransform>();
            lvRt.anchorMin = new Vector2(0.5f, 0);
            lvRt.anchorMax = new Vector2(0.5f, 0);
            lvRt.pivot = new Vector2(0.5f, 1);
            lvRt.sizeDelta = new Vector2(NodeW + 10, 14);
            lvRt.anchoredPosition = new Vector2(0, -2);
            var lv = lvGo.AddComponent<Text>();
            lv.font = font;
            lv.fontSize = 9;
            lv.alignment = TextAnchor.MiddleCenter;
            lv.color = new Color(0.66f, 0.60f, 0.45f);
            lv.text = $"Lv {n.minLv}–{n.maxLv}";
            return rt;
        }

        /// Draws a 1-D rectangle rotated to span from a to b. Cheapest way to
        /// get straight portal lines without bringing in LineRenderer/UGUI vector.
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
            img.color = new Color(0.55f, 0.45f, 0.30f, 0.65f);
        }

        private void UpdateCurrentHighlight()
        {
            string scene = SceneManager.GetActiveScene().name;
            string zoneId = Astrion.Network.SceneZoneMap.SceneToZone(scene);
            int idx = WorldMapData.NodeIndex(zoneId);

            if (idx < 0 || _highlightRing == null)
            {
                if (_highlightRing != null) _highlightRing.gameObject.SetActive(false);
                if (_statusText != null) _statusText.text = "current: (unknown scene)  •  press M to close";
                return;
            }

            _highlightRing.gameObject.SetActive(true);
            var ringRt = _highlightRing.rectTransform;
            ringRt.anchoredPosition = WorldMapData.Nodes[idx].pos;

            var node = WorldMapData.Nodes[idx];
            _statusText.text = $"current: {node.display}  (Lv {node.minLv}–{node.maxLv})  •  press M to close";
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
