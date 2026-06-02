using UnityEngine;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    /// Star Ledger — modal leaderboard panel opened by the Herald NPC in
    /// Solaria. Three tabs (Level / Gold / Kills) on the same dataset; each
    /// pulls fresh data from the server on tab switch. Self rank is pinned
    /// at the bottom so a player low on the chart still sees their position.
    ///
    /// DDOL singleton — built once on the login scene; opens via
    /// HeraldNPC2D.OnDialogueClosed.
    public class RankingUI : MonoBehaviour
    {
        public static RankingUI Instance { get; private set; }

        // Same medieval palette as LoginPanel / WorldMap / AstralImbue.
        private static readonly Color PanelBg       = new Color(0.10f, 0.08f, 0.06f, 0.96f);
        private static readonly Color PanelInner    = new Color(0.13f, 0.10f, 0.07f, 0.85f);
        private static readonly Color AccentGold    = new Color(0.85f, 0.65f, 0.22f, 1f);
        private static readonly Color AccentGoldDim = new Color(0.85f, 0.65f, 0.22f, 0.30f);
        private static readonly Color TextLight     = new Color(0.94f, 0.88f, 0.74f);
        private static readonly Color TextMuted     = new Color(0.62f, 0.55f, 0.42f);
        private static readonly Color TabActive     = new Color(0.32f, 0.24f, 0.14f, 1f);
        private static readonly Color TabInactive   = new Color(0.16f, 0.13f, 0.10f, 1f);
        private static readonly Color RowAlt        = new Color(0.15f, 0.12f, 0.09f, 0.85f);
        private static readonly Color RowSelf       = new Color(0.30f, 0.22f, 0.10f, 0.92f);

        private const int VISIBLE_ROWS = 20;

        private CanvasGroup _group;
        private bool _open;

        private Image[] _tabBgs;
        private Text[]  _tabLabels;
        private readonly string[] _tabCategories = { "level", "gold", "kills" };
        private readonly string[] _tabTitles     = { "LEVEL", "GOLD", "KILLS" };
        private int _currentTab = 0;

        // Row pool — fixed N entries, reused on every refresh.
        private RectTransform _rowsRoot;
        private GameObject[] _rowGo;
        private Text[] _rowRank;
        private Text[] _rowName;
        private Text[] _rowScore;
        private Image[] _rowBg;

        private Text _selfRankText;
        private Text _emptyText;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
            if (RankingSystem.Instance != null)
                RankingSystem.Instance.OnUpdated += Refresh;
        }

        private void OnDestroy()
        {
            if (RankingSystem.Instance != null)
                RankingSystem.Instance.OnUpdated -= Refresh;
            if (Instance == this) Instance = null;
        }

        public void Open()
        {
            _open = true;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            SelectTab(_currentTab);
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

        // ──────────────────────── BUILD ────────────────────────

        private void Build()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("RankingCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9120;
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
            panelRt.sizeDelta = new Vector2(720, 640);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = PanelBg;
            var panelOl = panelGo.AddComponent<Outline>();
            panelOl.effectColor = AccentGoldDim;
            panelOl.effectDistance = new Vector2(1, 1);

            // Inner inset
            var innerGo = new GameObject("Inner");
            innerGo.transform.SetParent(panelGo.transform, false);
            var innerRt = innerGo.AddComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
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

            // Eyebrow + title
            AddText(panelGo.transform, "Eyebrow", "—  S T A R   L E D G E R  —",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, AccentGold,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -22), new Vector2(420, 22));

            AddText(panelGo.transform, "Title", "★  랭킹",
                font, 26, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -50), new Vector2(560, 38));

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

            // Tabs
            _tabBgs = new Image[3];
            _tabLabels = new Text[3];
            float tabW = 140f, tabH = 32f, tabGap = 8f;
            float tabTotal = tabW * 3 + tabGap * 2;
            float tabY = -100f;
            for (int i = 0; i < 3; i++)
            {
                float x = -tabTotal * 0.5f + i * (tabW + tabGap);
                BuildTab(panelGo.transform, font, i, new Vector2(x, tabY), new Vector2(tabW, tabH));
            }

            // Column header
            float listTop = -150f;
            BuildColumnHeader(panelGo.transform, font, listTop);

            // Row pool
            BuildRows(panelGo.transform, font, listTop - 24f);

            // Self rank pinned at bottom
            var selfBg = new GameObject("SelfBg");
            selfBg.transform.SetParent(panelGo.transform, false);
            var selfBgRt = selfBg.AddComponent<RectTransform>();
            selfBgRt.anchorMin = new Vector2(0, 0); selfBgRt.anchorMax = new Vector2(1, 0);
            selfBgRt.pivot = new Vector2(0.5f, 0);
            selfBgRt.offsetMin = new Vector2(30, 0); selfBgRt.offsetMax = new Vector2(-30, 0);
            selfBgRt.sizeDelta = new Vector2(0, 32);
            selfBgRt.anchoredPosition = new Vector2(0, 60);
            var selfBgImg = selfBg.AddComponent<Image>();
            selfBgImg.color = RowSelf;
            var selfBgOl = selfBg.AddComponent<Outline>();
            selfBgOl.effectColor = AccentGoldDim;
            selfBgOl.effectDistance = new Vector2(1, 1);

            _selfRankText = AddText(selfBg.transform, "Lbl", "내 순위  —",
                font, 13, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            // Empty-state text (centered over the row area)
            _emptyText = AddText(panelGo.transform, "Empty", "기록이 없습니다.",
                font, 13, FontStyle.Normal, TextAnchor.MiddleCenter, TextMuted,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -300), new Vector2(400, 22));
            _emptyText.gameObject.SetActive(false);

            BuildCloseButton(panelGo.transform, font);
        }

        private void BuildTab(Transform parent, Font font, int index, Vector2 pos, Vector2 size)
        {
            var go = new GameObject($"Tab_{index}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var bg = go.AddComponent<Image>();
            bg.color = index == 0 ? TabActive : TabInactive;
            _tabBgs[index] = bg;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = AccentGoldDim;
            ol.effectDistance = new Vector2(1, 1);
            var btn = go.AddComponent<Button>();
            int capture = index;
            btn.onClick.AddListener(() => SelectTab(capture));
            var lbl = AddText(go.transform, "L", _tabTitles[index],
                font, 13, FontStyle.Bold, TextAnchor.MiddleCenter, AccentGold,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            _tabLabels[index] = lbl;
        }

        private void BuildColumnHeader(Transform parent, Font font, float y)
        {
            var go = new GameObject("Header");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(40, 0); rt.offsetMax = new Vector2(-40, 0);
            rt.sizeDelta = new Vector2(0, 20);
            rt.anchoredPosition = new Vector2(0, y);

            AddText(go.transform, "RankH", "순위",
                font, 11, FontStyle.Bold, TextAnchor.MiddleLeft, AccentGold,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(8, 0), new Vector2(80, 20));
            AddText(go.transform, "NameH", "이름",
                font, 11, FontStyle.Bold, TextAnchor.MiddleLeft, AccentGold,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(110, 0), new Vector2(320, 20));
            AddText(go.transform, "ScoreH", "점수",
                font, 11, FontStyle.Bold, TextAnchor.MiddleRight, AccentGold,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-8, 0), new Vector2(160, 20));
        }

        private void BuildRows(Transform parent, Font font, float topY)
        {
            _rowsRoot = new GameObject("Rows").AddComponent<RectTransform>();
            _rowsRoot.transform.SetParent(parent, false);
            _rowsRoot.anchorMin = new Vector2(0, 1); _rowsRoot.anchorMax = new Vector2(1, 1);
            _rowsRoot.pivot = new Vector2(0.5f, 1);
            _rowsRoot.offsetMin = new Vector2(40, 0); _rowsRoot.offsetMax = new Vector2(-40, 0);
            _rowsRoot.sizeDelta = new Vector2(0, VISIBLE_ROWS * 18);
            _rowsRoot.anchoredPosition = new Vector2(0, topY);

            _rowGo = new GameObject[VISIBLE_ROWS];
            _rowRank = new Text[VISIBLE_ROWS];
            _rowName = new Text[VISIBLE_ROWS];
            _rowScore = new Text[VISIBLE_ROWS];
            _rowBg = new Image[VISIBLE_ROWS];

            for (int i = 0; i < VISIBLE_ROWS; i++)
            {
                var rgo = new GameObject($"Row_{i}");
                rgo.transform.SetParent(_rowsRoot, false);
                var rrt = rgo.AddComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(1, 1);
                rrt.pivot = new Vector2(0.5f, 1);
                rrt.offsetMin = new Vector2(0, 0); rrt.offsetMax = new Vector2(0, 0);
                rrt.sizeDelta = new Vector2(0, 17);
                rrt.anchoredPosition = new Vector2(0, -i * 18);
                var bg = rgo.AddComponent<Image>();
                bg.color = (i % 2 == 0) ? RowAlt : new Color(0, 0, 0, 0);
                bg.raycastTarget = false;
                _rowBg[i] = bg;

                _rowRank[i] = AddText(rgo.transform, "Rank", "",
                    font, 12, FontStyle.Bold, TextAnchor.MiddleLeft, TextLight,
                    new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                    new Vector2(8, 0), new Vector2(80, 17));
                _rowName[i] = AddText(rgo.transform, "Name", "",
                    font, 12, FontStyle.Normal, TextAnchor.MiddleLeft, TextLight,
                    new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                    new Vector2(110, 0), new Vector2(320, 17));
                _rowScore[i] = AddText(rgo.transform, "Score", "",
                    font, 12, FontStyle.Bold, TextAnchor.MiddleRight, AccentGold,
                    new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                    new Vector2(-8, 0), new Vector2(160, 17));

                _rowGo[i] = rgo;
                rgo.SetActive(false);
            }
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
            ol.effectColor = AccentGoldDim;
            ol.effectDistance = new Vector2(1, 1);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(Close);
            AddText(go.transform, "L", "닫기  [Esc]",
                font, 12, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
        }

        // ──────────────────────── DATA ────────────────────────

        private void SelectTab(int idx)
        {
            _currentTab = Mathf.Clamp(idx, 0, 2);
            for (int i = 0; i < 3; i++)
            {
                if (_tabBgs[i] != null) _tabBgs[i].color = (i == _currentTab) ? TabActive : TabInactive;
                if (_tabLabels[i] != null) _tabLabels[i].color = (i == _currentTab) ? AccentGold : TextMuted;
            }
            RankingSystem.Instance?.Request(_tabCategories[_currentTab]);
        }

        private void Refresh()
        {
            var rs = RankingSystem.Instance;
            if (rs == null) return;
            // Discard data if it doesn't match the active tab — we may have
            // requested 'gold' and a stale 'level' response arrived first.
            if (rs.CurrentCategory != _tabCategories[_currentTab]) return;

            int n = Mathf.Min(rs.Entries.Count, VISIBLE_ROWS);
            for (int i = 0; i < VISIBLE_ROWS; i++)
            {
                bool active = i < n;
                _rowGo[i].SetActive(active);
                if (!active) continue;
                var e = rs.Entries[i];
                _rowRank[i].text = RankLabel(e.rank);
                _rowName[i].text = e.name;
                _rowScore[i].text = FormatScore(_tabCategories[_currentTab], e.score);
                // Top 3 wear a gold tint; everyone else stays parchment.
                _rowName[i].color = e.rank <= 3 ? AccentGold : TextLight;
            }
            if (_emptyText != null) _emptyText.gameObject.SetActive(n == 0);

            if (_selfRankText != null)
            {
                if (rs.SelfRank > 0)
                    _selfRankText.text = $"내 순위  ·  {RankLabel(rs.SelfRank)}     점수  {FormatScore(_tabCategories[_currentTab], rs.SelfScore)}";
                else
                    _selfRankText.text = "내 순위  ·  랭킹에 들지 못함";
            }
        }

        private static string RankLabel(int r)
        {
            return r switch
            {
                1 => "🥇 1",
                2 => "🥈 2",
                3 => "🥉 3",
                _ => r.ToString(),
            };
        }

        private static string FormatScore(string category, long s)
        {
            return category switch
            {
                "level" => $"Lv. {s}",
                "gold"  => $"{s:N0} G",
                "kills" => $"{s:N0} 처치",
                _       => s.ToString(),
            };
        }

        // ──────────────────── PRIMITIVE HELPERS ────────────────────

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
