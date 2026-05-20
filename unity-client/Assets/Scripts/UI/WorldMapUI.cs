using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Astrion.UI
{
    /// In-game world map overlay. M to toggle. Drawn to feel like an
    /// unfurled parchment with a city/portal network sketched on top —
    /// procedural textures, ornate frame, compass rose, pulse-halo on the
    /// current zone.
    ///
    /// DDOL singleton — built once at start; survives every portal load.
    public class WorldMapUI : MonoBehaviour
    {
        public static WorldMapUI Instance { get; private set; }

        private CanvasGroup _group;
        private RectTransform _mapArea;
        private Text _statusText;
        private Image _highlightRing;
        private bool _open;

        private const float PanelW = 1000f;
        private const float PanelH = 620f;
        private const float NodeSize = 28f;
        private const float CityFlagW = 70f;
        private const float CityFlagH = 38f;

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

            // Subtle pulse on the current-location halo while panel is open.
            if (_open && _highlightRing != null && _highlightRing.isActiveAndEnabled)
            {
                float t = 0.55f + 0.30f * Mathf.Sin(Time.unscaledTime * 3.4f);
                var c = _highlightRing.color; c.a = t; _highlightRing.color = c;
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

            // Procedural sprites — generated once at startup.
            var parchmentSpr = TexToSprite(MakeParchmentTex(1024, 640));
            var borderSpr    = TexToSprite(MakeOrnateBorderTex(1024, 640));
            var cornerSpr    = TexToSprite(MakeCornerOrnamentTex(96));
            var compassSpr   = TexToSprite(MakeCompassRoseTex(96));
            var cityFlagSpr  = TexToSprite(MakeCityFlagTex(140, 76));
            var huntDiamondSpr = TexToSprite(MakeDiamondTex(64));
            var dashSpr      = TexToSprite(MakeDashTex(8, 3));
            var haloSpr      = TexToSprite(MakeHaloTex(96));
            var scrollTitleSpr = TexToSprite(MakeScrollBannerTex(280, 56));

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

            // Dark vignette backdrop
            var dimGo = new GameObject("Dim");
            dimGo.transform.SetParent(groupGo.transform, false);
            var dimRt = dimGo.AddComponent<RectTransform>();
            Stretch(dimRt);
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.68f);

            // ── Parchment panel ──────────────────────────────────────────────
            var panelGo = new GameObject("Parchment");
            panelGo.transform.SetParent(groupGo.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(PanelW, PanelH);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.sprite = parchmentSpr;
            panelImg.type = Image.Type.Simple;

            // Ornate border (over the parchment)
            var borderGo = new GameObject("Border");
            borderGo.transform.SetParent(panelGo.transform, false);
            var borderRt = borderGo.AddComponent<RectTransform>();
            Stretch(borderRt);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.sprite = borderSpr;

            // Four corner ornaments
            float cornerOffset = 18f;
            float cornerSize = 56f;
            (Vector2 anchor, Vector2 pivot, float rot)[] corners = {
                (new Vector2(0, 1),   new Vector2(0, 1),   0f),
                (new Vector2(1, 1),   new Vector2(1, 1),  90f),
                (new Vector2(1, 0),   new Vector2(1, 0), 180f),
                (new Vector2(0, 0),   new Vector2(0, 0), 270f),
            };
            foreach (var c in corners)
            {
                var go = new GameObject("Corner");
                go.transform.SetParent(panelGo.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = c.anchor;
                rt.pivot = c.pivot;
                rt.sizeDelta = new Vector2(cornerSize, cornerSize);
                float ox = c.pivot.x < 0.5f ? cornerOffset : -cornerOffset;
                float oy = c.pivot.y < 0.5f ? cornerOffset : -cornerOffset;
                rt.anchoredPosition = new Vector2(ox, oy);
                rt.localRotation = Quaternion.Euler(0, 0, c.rot);
                var img = go.AddComponent<Image>();
                img.sprite = cornerSpr;
            }

            // ── Title scroll (top center) ────────────────────────────────────
            var titleScrollGo = new GameObject("TitleScroll");
            titleScrollGo.transform.SetParent(panelGo.transform, false);
            var tsRt = titleScrollGo.AddComponent<RectTransform>();
            tsRt.anchorMin = new Vector2(0.5f, 1);
            tsRt.anchorMax = new Vector2(0.5f, 1);
            tsRt.pivot = new Vector2(0.5f, 1);
            tsRt.sizeDelta = new Vector2(360, 64);
            tsRt.anchoredPosition = new Vector2(0, -10);
            var tsImg = titleScrollGo.AddComponent<Image>();
            tsImg.sprite = scrollTitleSpr;

            var titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(titleScrollGo.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            Stretch(titleRt);
            titleRt.offsetMin = new Vector2(0, 8);
            titleRt.offsetMax = new Vector2(0, -8);
            var title = titleGo.AddComponent<Text>();
            title.font = font;
            title.fontSize = 22;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.32f, 0.18f, 0.08f);
            title.text = "✦  Realm of Astrion  ✦";

            // ── Compass rose (top-right corner of map area) ──────────────────
            var compassGo = new GameObject("Compass");
            compassGo.transform.SetParent(panelGo.transform, false);
            var compRt = compassGo.AddComponent<RectTransform>();
            compRt.anchorMin = new Vector2(1, 1);
            compRt.anchorMax = new Vector2(1, 1);
            compRt.pivot = new Vector2(1, 1);
            compRt.sizeDelta = new Vector2(76, 76);
            compRt.anchoredPosition = new Vector2(-44, -90);
            var compImg = compassGo.AddComponent<Image>();
            compImg.sprite = compassSpr;
            compImg.color = new Color(1, 1, 1, 0.85f);

            // Status banner (bottom)
            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(panelGo.transform, false);
            var statusRt = statusGo.AddComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0, 0);
            statusRt.anchorMax = new Vector2(1, 0);
            statusRt.pivot = new Vector2(0.5f, 0);
            statusRt.sizeDelta = new Vector2(0, 30);
            statusRt.anchoredPosition = new Vector2(0, 18);
            _statusText = statusGo.AddComponent<Text>();
            _statusText.font = font;
            _statusText.fontSize = 14;
            _statusText.fontStyle = FontStyle.Italic;
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = new Color(0.38f, 0.22f, 0.10f);
            _statusText.text = "press [M] to close the chart";

            // ── Map drawing area ─────────────────────────────────────────────
            var mapGo = new GameObject("MapArea");
            mapGo.transform.SetParent(panelGo.transform, false);
            _mapArea = mapGo.AddComponent<RectTransform>();
            _mapArea.anchorMin = _mapArea.anchorMax = new Vector2(0.5f, 0.5f);
            _mapArea.pivot = new Vector2(0.5f, 0.5f);
            _mapArea.sizeDelta = new Vector2(PanelW - 100, PanelH - 160);
            _mapArea.anchoredPosition = new Vector2(0, -10);

            // Dashed portal lines first (under nodes)
            foreach (var (a, b) in WorldMapData.Edges)
            {
                int ai = WorldMapData.NodeIndex(a);
                int bi = WorldMapData.NodeIndex(b);
                if (ai < 0 || bi < 0) continue;
                DrawDashedLine(_mapArea, WorldMapData.Nodes[ai].pos,
                                          WorldMapData.Nodes[bi].pos, dashSpr);
            }

            // Pulsing gold halo, repositioned per current zone
            var ringGo = new GameObject("Highlight");
            ringGo.transform.SetParent(_mapArea, false);
            var ringRt = ringGo.AddComponent<RectTransform>();
            ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
            ringRt.pivot = new Vector2(0.5f, 0.5f);
            ringRt.sizeDelta = new Vector2(74, 74);
            _highlightRing = ringGo.AddComponent<Image>();
            _highlightRing.sprite = haloSpr;
            _highlightRing.color = new Color(1f, 0.82f, 0.30f, 0.75f);
            ringGo.SetActive(false);

            // Nodes
            for (int i = 0; i < WorldMapData.Nodes.Length; i++)
            {
                var n = WorldMapData.Nodes[i];
                if (n.isCity) DrawCityNode(_mapArea, n, font, cityFlagSpr);
                else          DrawHuntingNode(_mapArea, n, font, huntDiamondSpr);
            }

            UpdateCurrentHighlight();
        }

        private void DrawCityNode(RectTransform parent, WorldMapData.Node n, Font font, Sprite flag)
        {
            var go = new GameObject($"City_{n.id}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(CityFlagW, CityFlagH);
            rt.anchoredPosition = n.pos;
            var img = go.AddComponent<Image>();
            img.sprite = flag;

            // Label inside the banner
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lblRt = lblGo.AddComponent<RectTransform>();
            Stretch(lblRt);
            lblRt.offsetMin = new Vector2(4, 6);
            lblRt.offsetMax = new Vector2(-4, -2);
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = font;
            lbl.fontSize = 12;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = new Color(0.96f, 0.88f, 0.55f);
            lbl.text = n.display;

            // Lv tag under the flag
            DrawLevelTag(parent, n.pos + new Vector2(0, -CityFlagH * 0.5f - 8), n, font);
        }

        private void DrawHuntingNode(RectTransform parent, WorldMapData.Node n, Font font, Sprite diamond)
        {
            // Diamond marker
            var go = new GameObject($"Hunt_{n.id}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(NodeSize, NodeSize);
            rt.anchoredPosition = n.pos;
            var img = go.AddComponent<Image>();
            img.sprite = diamond;

            // Label above the diamond
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(parent, false);
            var lblRt = lblGo.AddComponent<RectTransform>();
            lblRt.anchorMin = lblRt.anchorMax = new Vector2(0.5f, 0.5f);
            lblRt.pivot = new Vector2(0.5f, 0);
            lblRt.sizeDelta = new Vector2(120, 16);
            lblRt.anchoredPosition = n.pos + new Vector2(0, NodeSize * 0.5f + 2);
            var lbl = lblGo.AddComponent<Text>();
            lbl.font = font;
            lbl.fontSize = 11;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = new Color(0.30f, 0.18f, 0.08f);
            lbl.text = n.display;

            DrawLevelTag(parent, n.pos + new Vector2(0, -NodeSize * 0.5f - 8), n, font);
        }

        private void DrawLevelTag(RectTransform parent, Vector2 pos, WorldMapData.Node n, Font font)
        {
            var go = new GameObject("Lv");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(80, 14);
            rt.anchoredPosition = pos;
            var lv = go.AddComponent<Text>();
            lv.font = font;
            lv.fontSize = 10;
            lv.fontStyle = FontStyle.Italic;
            lv.alignment = TextAnchor.MiddleCenter;
            lv.color = new Color(0.45f, 0.30f, 0.16f);
            lv.text = $"Lv. {n.minLv}–{n.maxLv}";
        }

        private void DrawDashedLine(RectTransform parent, Vector2 a, Vector2 b, Sprite dashSpr)
        {
            // Subdivide into short dash segments so the line reads as a path,
            // not a paint stroke. ~10px per dash including gap.
            Vector2 d = b - a;
            float len = d.magnitude;
            int dashes = Mathf.Max(2, Mathf.RoundToInt(len / 11f));
            float deg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            for (int i = 0; i < dashes; i++)
            {
                float t = (i + 0.5f) / dashes;
                Vector2 p = Vector2.Lerp(a, b, t);
                var go = new GameObject("Dash");
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(7f, 2.5f);
                rt.anchoredPosition = p;
                rt.localRotation = Quaternion.Euler(0, 0, deg);
                var img = go.AddComponent<Image>();
                img.sprite = dashSpr;
                img.color = new Color(0.42f, 0.26f, 0.14f, 0.80f);
            }
        }

        private void UpdateCurrentHighlight()
        {
            string scene = SceneManager.GetActiveScene().name;
            string zoneId = Astrion.Network.SceneZoneMap.SceneToZone(scene);
            int idx = WorldMapData.NodeIndex(zoneId);

            if (idx < 0 || _highlightRing == null)
            {
                if (_highlightRing != null) _highlightRing.gameObject.SetActive(false);
                if (_statusText != null) _statusText.text = "lost in uncharted lands…  press [M] to close";
                return;
            }

            _highlightRing.gameObject.SetActive(true);
            _highlightRing.rectTransform.anchoredPosition = WorldMapData.Nodes[idx].pos;

            var n = WorldMapData.Nodes[idx];
            string kind = n.isCity ? "city" : "wilds";
            _statusText.text = $"you stand in {n.display}   ·   {kind} · Lv. {n.minLv}–{n.maxLv}   ·   press [M] to close";
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ────────────────────────────────────────────────────────────────────
        // PROCEDURAL TEXTURES
        // ────────────────────────────────────────────────────────────────────

        private static Sprite TexToSprite(Texture2D tex) =>
            Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                          new Vector2(0.5f, 0.5f), 100f);

        /// Aged parchment with vignetting and grain.
        private static Texture2D MakeParchmentTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            var px = new Color[w * h];
            Color baseColor = new Color(0.86f, 0.74f, 0.52f);  // warm beige
            Color edgeColor = new Color(0.48f, 0.32f, 0.16f);  // burnt umber
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // Distance from center → vignette
                float fx = (x - w * 0.5f) / (w * 0.5f);
                float fy = (y - h * 0.5f) / (h * 0.5f);
                float r = Mathf.Sqrt(fx * fx + fy * fy);
                float vignette = Mathf.Clamp01((r - 0.55f) * 1.4f);

                // Noise grain
                float n1 = Mathf.PerlinNoise(x * 0.012f, y * 0.012f);
                float n2 = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                float grain = n1 * 0.75f + n2 * 0.25f;
                grain = 0.85f + grain * 0.18f;

                Color c = Color.Lerp(baseColor, edgeColor, vignette);
                c *= grain;
                c.a = 0.98f;
                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply();
            return t;
        }

        /// Heavy iron/leather frame around the parchment, transparent middle.
        private static Texture2D MakeOrnateBorderTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            int thickness = 10;
            Color frame = new Color(0.22f, 0.13f, 0.07f, 1f);
            Color inner = new Color(0.55f, 0.38f, 0.18f, 1f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int dEdge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                if (dEdge <= thickness)
                {
                    // Two-layer frame: outer dark, inner brass line
                    Color c = (dEdge <= thickness - 4) ? frame : inner;
                    px[y * w + x] = c;
                }
                else
                {
                    px[y * w + x] = new Color(0, 0, 0, 0);
                }
            }
            t.SetPixels(px); t.Apply();
            return t;
        }

        /// Corner flourish — a stylised filigree wedge. Pixel-art-ish so it
        /// reads at small sizes.
        private static Texture2D MakeCornerOrnamentTex(int s)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            Color brass = new Color(0.78f, 0.55f, 0.24f, 1f);
            Color dark  = new Color(0.32f, 0.18f, 0.08f, 1f);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                // Top-left wedge: hatched diagonal lines + central knot
                float dx = x / (float)s;
                float dy = 1f - y / (float)s;
                bool onWedge = dx + dy < 0.95f;
                bool onHatch = ((x + y) % 6 == 0) && onWedge;
                bool onKnot  = (dx < 0.30f && dy < 0.30f) && ((x - y) % 4 == 0);
                if (onKnot)        px[y * s + x] = brass;
                else if (onHatch)  px[y * s + x] = brass;
                else if (onWedge && dx + dy < 0.18f) px[y * s + x] = dark;
                else px[y * s + x] = new Color(0, 0, 0, 0);
            }
            t.SetPixels(px); t.Apply();
            return t;
        }

        /// Eight-point compass star with N marker on top.
        private static Texture2D MakeCompassRoseTex(int s)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            Color star = new Color(0.32f, 0.18f, 0.08f, 1f);
            Color tip  = new Color(0.78f, 0.55f, 0.24f, 1f);
            float cx = s * 0.5f, cy = s * 0.5f;
            float maxR = s * 0.45f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                // 8 petals: thin near tips
                float petal = Mathf.Abs(Mathf.Sin(ang * 4f));
                float petalR = Mathf.Lerp(maxR * 0.3f, maxR, 1f - petal);
                Color c = new Color(0, 0, 0, 0);
                if (r < petalR)
                {
                    c = star;
                    if (r > petalR - 3) c = tip;
                }
                // tiny center dot
                if (r < 2.2f) c = tip;
                px[y * s + x] = c;
            }
            // N letter — just brighten a thin column at the top
            for (int y = 0; y < 8; y++)
                for (int x = (int)cx - 2; x <= (int)cx + 2; x++)
                    if (x >= 0 && x < s) px[(s - 4 - y) * s + x] = tip;
            t.SetPixels(px); t.Apply();
            return t;
        }

        /// Banner-like flag with darker shadow, for cities.
        private static Texture2D MakeCityFlagTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color cloth = new Color(0.45f, 0.18f, 0.16f);   // crimson
            Color shadow = new Color(0.22f, 0.10f, 0.08f);
            Color rim = new Color(0.78f, 0.55f, 0.24f);
            int rim_t = 2;
            int swallow = 12; // tail cut depth
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // Swallow-tail right edge
                float tailMidDist = Mathf.Abs(y - h * 0.5f);
                bool inSwallow = (x > w - swallow) && (tailMidDist < swallow - (w - x));
                if (inSwallow) { px[y * w + x] = new Color(0, 0, 0, 0); continue; }

                int dEdge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                if (dEdge < rim_t) px[y * w + x] = rim;
                else
                {
                    // soft top→bottom shadow
                    float v = 1f - (y / (float)h) * 0.45f;
                    px[y * w + x] = cloth * v;
                }
            }
            // Drop shadow underline
            for (int x = 2; x < w - 2; x++) px[1 * w + x] = shadow;
            t.SetPixels(px); t.Apply();
            return t;
        }

        /// Diamond pin for hunting zones.
        private static Texture2D MakeDiamondTex(int s)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            Color body = new Color(0.22f, 0.16f, 0.10f, 1f);
            Color edge = new Color(0.78f, 0.55f, 0.24f, 1f);
            Color hi   = new Color(1f, 0.92f, 0.65f, 1f);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float fx = (x - s * 0.5f) / (s * 0.5f);
                float fy = (y - s * 0.5f) / (s * 0.5f);
                float d = Mathf.Abs(fx) + Mathf.Abs(fy);
                if (d > 0.98f) { px[y * s + x] = new Color(0, 0, 0, 0); continue; }
                if (d > 0.78f) { px[y * s + x] = edge; continue; }
                if (fx < -0.18f && fy > 0.18f) { px[y * s + x] = hi; continue; }
                px[y * s + x] = body;
            }
            t.SetPixels(px); t.Apply();
            return t;
        }

        /// Short opaque dash, used many times to make a dotted path.
        private static Texture2D MakeDashTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            t.SetPixels(px); t.Apply();
            return t;
        }

        /// Radial gold halo, alpha falloff from center.
        private static Texture2D MakeHaloTex(int s)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color[s * s];
            float cx = s * 0.5f, cy = s * 0.5f;
            float maxR = s * 0.5f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x - cx, dy = y - cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float t01 = Mathf.Clamp01(r / maxR);
                float a = Mathf.Clamp01(1f - t01);
                a = a * a;
                px[y * s + x] = new Color(1f, 0.86f, 0.36f, a);
            }
            t.SetPixels(px); t.Apply();
            return t;
        }

        /// Title scroll — rolled ends + flat middle.
        private static Texture2D MakeScrollBannerTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color paper = new Color(0.93f, 0.82f, 0.58f);
            Color rolled = new Color(0.55f, 0.36f, 0.18f);
            Color shadow = new Color(0.30f, 0.18f, 0.08f);
            int rollW = 28;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = paper;
                // Rolled ends
                if (x < rollW || x > w - rollW - 1)
                {
                    int rx = x < rollW ? x : (w - 1 - x);
                    float v = rx / (float)rollW;
                    c = Color.Lerp(rolled, shadow, 1f - v);
                }
                // Soft shadow above and below
                if (y < 3) c *= 0.65f;
                if (y > h - 4) c *= 0.55f;
                px[y * w + x] = c;
            }
            t.SetPixels(px); t.Apply();
            return t;
        }
    }
}
