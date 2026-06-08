using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Astrion.Game;

namespace Astrion.UI
{
    /// Global asynchronous auction house — opens at any city's Auctioneer
    /// NPC. Browse pane shows recent listings (max 50, sorted newest first).
    /// 'My listings' is interleaved with a 취소 button instead of 구매.
    /// Register flow: pick from inventory → enter price → server registers.
    ///
    /// DDOL singleton (sortingOrder 9130). Refreshes on AuctionSystem
    /// OnListUpdated; pulls fresh on Open() and after every action.
    public class AuctionUI : MonoBehaviour
    {
        public static AuctionUI Instance { get; private set; }

        private static readonly Color PanelBg     = new Color(0.10f, 0.08f, 0.06f, 0.97f);
        private static readonly Color PanelInner  = new Color(0.13f, 0.10f, 0.07f, 0.85f);
        private static readonly Color AccentGold  = new Color(0.85f, 0.65f, 0.22f, 1f);
        private static readonly Color AccentDim   = new Color(0.85f, 0.65f, 0.22f, 0.30f);
        private static readonly Color TextLight   = new Color(0.94f, 0.88f, 0.74f);
        private static readonly Color TextMuted   = new Color(0.62f, 0.55f, 0.42f);
        private static readonly Color RowBg       = new Color(0.16f, 0.13f, 0.10f, 1f);
        private static readonly Color RowBgMine   = new Color(0.22f, 0.18f, 0.10f, 1f);
        private static readonly Color BtnBuy      = new Color(0.32f, 0.40f, 0.18f, 1f);
        private static readonly Color BtnCancel   = new Color(0.45f, 0.18f, 0.18f, 1f);
        private static readonly Color BtnRegister = new Color(0.42f, 0.32f, 0.18f, 1f);

        private CanvasGroup _group;
        private bool _open;
        private RectTransform _listRoot;
        private Text _statusText;
        private Text _walletText;

        // Register modal state
        private CanvasGroup _registerGroup;
        private bool _registerOpen;
        private Text _selectedNameText;
        private InputField _qtyInput;
        private InputField _priceInput;
        private string _selectedItemId = "";
        private int _selectedMaxQty = 0;
        private RectTransform _inventoryGrid;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
        }

        private void Start()
        {
            if (AuctionSystem.Instance != null)
                AuctionSystem.Instance.OnListUpdated += RefreshList;
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.OnChanged += RefreshWallet;
        }

        private void OnDestroy()
        {
            if (AuctionSystem.Instance != null)
                AuctionSystem.Instance.OnListUpdated -= RefreshList;
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.OnChanged -= RefreshWallet;
            if (Instance == this) Instance = null;
        }

        public void Open()
        {
            _open = true;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            AuctionSystem.Instance?.RequestList();
            RefreshList();
            RefreshWallet();
        }

        public void Close()
        {
            _open = false;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            CloseRegister();
        }

        private void Update()
        {
            if (_registerOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape) && !IsTextInputFocused()) CloseRegister();
                return;
            }
            if (_open && Input.GetKeyDown(KeyCode.Escape) && !IsTextInputFocused()) Close();
        }

        private bool IsTextInputFocused()
        {
            if (EventSystem.current == null) return false;
            var sel = EventSystem.current.currentSelectedGameObject;
            return sel != null && sel.GetComponent<InputField>() != null;
        }

        // ──────────────────────── BUILD ────────────────────────

        private void Build()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("AuctionCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9130;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            BuildMainPanel(canvasGo.transform, font);
            BuildRegisterModal(canvasGo.transform, font);
        }

        private void BuildMainPanel(Transform parent, Font font)
        {
            var groupGo = new GameObject("Group");
            groupGo.transform.SetParent(parent, false);
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
            panelRt.sizeDelta = new Vector2(900, 700);
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

            AddText(panelGo.transform, "Eyebrow", "—  A U C T I O N   H O U S E  —",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, AccentGold,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -22), new Vector2(420, 22));
            AddText(panelGo.transform, "Title", "★  경매장",
                font, 24, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -52), new Vector2(560, 36));

            _walletText = AddText(panelGo.transform, "Wallet", "",
                font, 13, FontStyle.Bold, TextAnchor.MiddleLeft, AccentGold,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(30, -92), new Vector2(360, 20));

            // Header
            BuildHeader(panelGo.transform, font);

            // Scroll list
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0); scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(30, 110); scrollRt.offsetMax = new Vector2(-30, -150);
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = PanelInner;
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(2, 2); vpRt.offsetMax = new Vector2(-2, -2);
            var vpImg = vpGo.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.01f);
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRt;

            _listRoot = new GameObject("Content").AddComponent<RectTransform>();
            _listRoot.transform.SetParent(vpGo.transform, false);
            _listRoot.anchorMin = new Vector2(0, 1); _listRoot.anchorMax = new Vector2(1, 1);
            _listRoot.pivot = new Vector2(0.5f, 1);
            _listRoot.offsetMin = Vector2.zero; _listRoot.offsetMax = Vector2.zero;
            _listRoot.sizeDelta = new Vector2(0, 100);
            scroll.content = _listRoot;

            _statusText = AddText(panelGo.transform, "Status", "",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, TextMuted,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 70), new Vector2(700, 18));

            // Register button + Close button
            var regGo = new GameObject("RegisterBtn");
            regGo.transform.SetParent(panelGo.transform, false);
            var regRt = regGo.AddComponent<RectTransform>();
            regRt.anchorMin = regRt.anchorMax = new Vector2(0, 0);
            regRt.pivot = new Vector2(0, 0);
            regRt.anchoredPosition = new Vector2(30, 22);
            regRt.sizeDelta = new Vector2(160, 36);
            var regImg = regGo.AddComponent<Image>();
            regImg.color = BtnRegister;
            var regOl = regGo.AddComponent<Outline>();
            regOl.effectColor = AccentDim;
            regOl.effectDistance = new Vector2(1, 1);
            var regBtn = regGo.AddComponent<Button>();
            regBtn.onClick.AddListener(OpenRegister);
            AddText(regGo.transform, "L", "▶  매물 등록",
                font, 13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.85f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var refreshGo = new GameObject("RefreshBtn");
            refreshGo.transform.SetParent(panelGo.transform, false);
            var rfRt = refreshGo.AddComponent<RectTransform>();
            rfRt.anchorMin = rfRt.anchorMax = new Vector2(0, 0);
            rfRt.pivot = new Vector2(0, 0);
            rfRt.anchoredPosition = new Vector2(200, 22);
            rfRt.sizeDelta = new Vector2(120, 36);
            var rfImg = refreshGo.AddComponent<Image>();
            rfImg.color = new Color(0.20f, 0.20f, 0.16f, 1f);
            var rfOl = refreshGo.AddComponent<Outline>();
            rfOl.effectColor = AccentDim;
            rfOl.effectDistance = new Vector2(1, 1);
            var rfBtn = refreshGo.AddComponent<Button>();
            rfBtn.onClick.AddListener(() => AuctionSystem.Instance?.RequestList());
            AddText(refreshGo.transform, "L", "↻ 새로고침",
                font, 12, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            BuildCloseButton(panelGo.transform, font);
        }

        private void BuildHeader(Transform parent, Font font)
        {
            var go = new GameObject("Header");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(40, 0); rt.offsetMax = new Vector2(-40, 0);
            rt.sizeDelta = new Vector2(0, 20);
            rt.anchoredPosition = new Vector2(0, -130);

            AddText(go.transform, "I", "아이템", font, 11, FontStyle.Bold, TextAnchor.MiddleLeft, AccentGold,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(300, 20));
            AddText(go.transform, "S", "판매자", font, 11, FontStyle.Bold, TextAnchor.MiddleLeft, AccentGold,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(380, 0), new Vector2(160, 20));
            AddText(go.transform, "P", "가격", font, 11, FontStyle.Bold, TextAnchor.MiddleRight, AccentGold,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-160, 0), new Vector2(140, 20));
            AddText(go.transform, "A", "동작", font, 11, FontStyle.Bold, TextAnchor.MiddleCenter, AccentGold,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-60, 0), new Vector2(100, 20));
        }

        private void BuildCloseButton(Transform parent, Font font)
        {
            var go = new GameObject("CloseB");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-30, 22);
            rt.sizeDelta = new Vector2(120, 36);
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

        // ──────────────────── REGISTER MODAL ────────────────────

        private void BuildRegisterModal(Transform parent, Font font)
        {
            var modalGo = new GameObject("RegisterModal");
            modalGo.transform.SetParent(parent, false);
            var modalRt = modalGo.AddComponent<RectTransform>();
            Stretch(modalRt);
            _registerGroup = modalGo.AddComponent<CanvasGroup>();
            _registerGroup.alpha = 0f;
            _registerGroup.blocksRaycasts = false;

            var dim = modalGo.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.55f);

            var pGo = new GameObject("Panel");
            pGo.transform.SetParent(modalGo.transform, false);
            var pRt = pGo.AddComponent<RectTransform>();
            pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 0.5f);
            pRt.pivot = new Vector2(0.5f, 0.5f);
            pRt.sizeDelta = new Vector2(620, 540);
            var pImg = pGo.AddComponent<Image>();
            pImg.color = PanelBg;
            var pOl = pGo.AddComponent<Outline>();
            pOl.effectColor = AccentDim;
            pOl.effectDistance = new Vector2(1, 1);

            AddText(pGo.transform, "T", "─  R E G I S T E R   I T E M  —",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, AccentGold,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -16), new Vector2(420, 22));
            AddText(pGo.transform, "T2", "매물 등록",
                font, 20, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -46), new Vector2(560, 28));

            // Inventory grid (top half)
            AddText(pGo.transform, "Lbl", "인벤토리에서 아이템 선택 (귀속 X)",
                font, 11, FontStyle.Bold, TextAnchor.MiddleLeft, AccentGold,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -80), new Vector2(580, 18));

            _inventoryGrid = new GameObject("InvGrid").AddComponent<RectTransform>();
            _inventoryGrid.transform.SetParent(pGo.transform, false);
            _inventoryGrid.anchorMin = new Vector2(0, 1); _inventoryGrid.anchorMax = new Vector2(1, 1);
            _inventoryGrid.pivot = new Vector2(0.5f, 1);
            _inventoryGrid.offsetMin = new Vector2(20, 0); _inventoryGrid.offsetMax = new Vector2(-20, 0);
            _inventoryGrid.sizeDelta = new Vector2(0, 230);
            _inventoryGrid.anchoredPosition = new Vector2(0, -102);

            // Selected slot summary
            _selectedNameText = AddText(pGo.transform, "Sel", "선택된 아이템 없음",
                font, 14, FontStyle.Bold, TextAnchor.MiddleLeft, AccentGold,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -350), new Vector2(580, 22));

            // Quantity field
            AddText(pGo.transform, "QL", "수량",
                font, 12, FontStyle.Bold, TextAnchor.MiddleLeft, TextLight,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -382), new Vector2(80, 22));
            _qtyInput = BuildInputField(pGo.transform, font, new Vector2(110, -382), new Vector2(140, 30),
                "1", InputField.ContentType.IntegerNumber);

            // Price field
            AddText(pGo.transform, "PL", "가격",
                font, 12, FontStyle.Bold, TextAnchor.MiddleLeft, TextLight,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(280, -382), new Vector2(80, 22));
            _priceInput = BuildInputField(pGo.transform, font, new Vector2(360, -382), new Vector2(220, 30),
                "100", InputField.ContentType.IntegerNumber);

            AddText(pGo.transform, "Hint", "수수료 5% — 판매되면 판매자에게 가격의 95% 지급. 24h 후 만료.",
                font, 10, FontStyle.Normal, TextAnchor.MiddleLeft, TextMuted,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -424), new Vector2(580, 16));

            // Submit / Cancel
            var submitGo = new GameObject("Submit");
            submitGo.transform.SetParent(pGo.transform, false);
            var smRt = submitGo.AddComponent<RectTransform>();
            smRt.anchorMin = smRt.anchorMax = new Vector2(0.5f, 0);
            smRt.pivot = new Vector2(0.5f, 0);
            smRt.anchoredPosition = new Vector2(-80, 30);
            smRt.sizeDelta = new Vector2(140, 40);
            var smImg = submitGo.AddComponent<Image>();
            smImg.color = BtnRegister;
            var smOl = submitGo.AddComponent<Outline>();
            smOl.effectColor = AccentDim;
            smOl.effectDistance = new Vector2(1, 1);
            var smBtn = submitGo.AddComponent<Button>();
            smBtn.onClick.AddListener(OnSubmitRegister);
            AddText(submitGo.transform, "L", "등록",
                font, 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.85f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var ccGo = new GameObject("CancelBtn");
            ccGo.transform.SetParent(pGo.transform, false);
            var ccRt = ccGo.AddComponent<RectTransform>();
            ccRt.anchorMin = ccRt.anchorMax = new Vector2(0.5f, 0);
            ccRt.pivot = new Vector2(0.5f, 0);
            ccRt.anchoredPosition = new Vector2(80, 30);
            ccRt.sizeDelta = new Vector2(140, 40);
            var ccImg = ccGo.AddComponent<Image>();
            ccImg.color = new Color(0.45f, 0.18f, 0.18f, 1f);
            var ccOl = ccGo.AddComponent<Outline>();
            ccOl.effectColor = AccentDim;
            ccOl.effectDistance = new Vector2(1, 1);
            var ccBtn = ccGo.AddComponent<Button>();
            ccBtn.onClick.AddListener(CloseRegister);
            AddText(ccGo.transform, "L", "취소 [Esc]",
                font, 13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.85f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        private InputField BuildInputField(Transform parent, Font font, Vector2 pos, Vector2 size,
                                           string defaultText, InputField.ContentType contentType)
        {
            var go = new GameObject("Input");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.05f, 0.04f, 0.92f);
            var ol = go.AddComponent<Outline>();
            ol.effectColor = AccentDim;
            ol.effectDistance = new Vector2(1, 1);
            var input = go.AddComponent<InputField>();
            input.contentType = contentType;
            input.targetGraphic = bg;
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8, 0); textRt.offsetMax = new Vector2(-8, 0);
            var txt = textGo.AddComponent<Text>();
            txt.font = font; txt.fontSize = 13;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = TextLight;
            input.textComponent = txt;
            input.text = defaultText;
            return input;
        }

        // ──────────────────────── REFRESH ────────────────────────

        private void RefreshWallet()
        {
            var stats = PlayerStats.Instance;
            if (_walletText != null && stats != null)
                _walletText.text = $"보유 골드: {stats.Gold:N0} G";
        }

        private void RefreshList()
        {
            if (_listRoot == null) return;
            for (int i = _listRoot.childCount - 1; i >= 0; i--)
                Destroy(_listRoot.GetChild(i).gameObject);

            var sys = AuctionSystem.Instance;
            if (sys == null || sys.Entries.Count == 0)
            {
                if (_statusText != null) _statusText.text = "등록된 매물이 없습니다.";
                _listRoot.sizeDelta = new Vector2(0, 60);
                return;
            }
            if (_statusText != null) _statusText.text = $"활성 매물  ·  {sys.Entries.Count}";

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            float y = 6f;
            const float rowH = 44f, rowGap = 4f;
            foreach (var e in sys.Entries)
            {
                BuildAuctionRow(_listRoot, font, e, y);
                y += rowH + rowGap;
            }
            _listRoot.sizeDelta = new Vector2(0, y + 8f);
        }

        private void BuildAuctionRow(RectTransform parent, Font font, AuctionSystem.Entry e, float yOffset)
        {
            var go = new GameObject($"Row_{e.id}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(10, 0); rt.offsetMax = new Vector2(-10, 0);
            rt.sizeDelta = new Vector2(0, 44);
            rt.anchoredPosition = new Vector2(0, -yOffset);
            var bg = go.AddComponent<Image>();
            bg.color = e.mine ? RowBgMine : RowBg;

            var def = ItemDatabase.Get(e.itemId);
            string display = def != null ? def.displayName : e.itemId;

            // Item icon (colored square + letter)
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f); iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.anchoredPosition = new Vector2(10, 0);
            iconRt.sizeDelta = new Vector2(34, 34);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = def != null
                ? new Color(def.iconColor.r * 0.35f, def.iconColor.g * 0.35f, def.iconColor.b * 0.35f, 1f)
                : new Color(0.30f, 0.30f, 0.30f, 1f);
            AddText(iconGo.transform, "L", def?.iconLetter ?? "?",
                font, 16, FontStyle.Bold, TextAnchor.MiddleCenter,
                def != null ? def.iconColor : Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            AddText(go.transform, "Name",
                e.qty > 1 ? $"{display}  × {e.qty}" : display,
                font, 13, FontStyle.Bold, TextAnchor.MiddleLeft, TextLight,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(54, 0), new Vector2(310, 20));

            AddText(go.transform, "Seller", e.seller,
                font, 12, FontStyle.Normal, TextAnchor.MiddleLeft, TextMuted,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(370, 0), new Vector2(150, 20));

            AddText(go.transform, "Price", $"{e.price:N0} G",
                font, 13, FontStyle.Bold, TextAnchor.MiddleRight, AccentGold,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-128, 0), new Vector2(130, 20));

            // Action button
            var actGo = new GameObject("Act");
            actGo.transform.SetParent(go.transform, false);
            var actRt = actGo.AddComponent<RectTransform>();
            actRt.anchorMin = actRt.anchorMax = new Vector2(1, 0.5f);
            actRt.pivot = new Vector2(1, 0.5f);
            actRt.anchoredPosition = new Vector2(-8, 0);
            actRt.sizeDelta = new Vector2(100, 28);
            var actImg = actGo.AddComponent<Image>();
            actImg.color = e.mine ? BtnCancel : BtnBuy;
            var actBtn = actGo.AddComponent<Button>();
            string capture = e.id;
            if (e.mine) actBtn.onClick.AddListener(() => AuctionSystem.Instance?.Cancel(capture));
            else        actBtn.onClick.AddListener(() => AuctionSystem.Instance?.Buy(capture));
            AddText(actGo.transform, "L", e.mine ? "취소" : "구매",
                font, 12, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.85f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        // ──────────────────── REGISTER MODAL CTRL ────────────────────

        private void OpenRegister()
        {
            _registerOpen = true;
            _registerGroup.alpha = 1f;
            _registerGroup.blocksRaycasts = true;
            _selectedItemId = "";
            _selectedMaxQty = 0;
            if (_qtyInput != null) _qtyInput.text = "1";
            if (_priceInput != null) _priceInput.text = "100";
            if (_selectedNameText != null) _selectedNameText.text = "선택된 아이템 없음";
            RebuildInventoryGrid();
        }

        private void CloseRegister()
        {
            _registerOpen = false;
            _registerGroup.alpha = 0f;
            _registerGroup.blocksRaycasts = false;
        }

        private void RebuildInventoryGrid()
        {
            if (_inventoryGrid == null) return;
            for (int i = _inventoryGrid.childCount - 1; i >= 0; i--)
                Destroy(_inventoryGrid.GetChild(i).gameObject);

            var inv = InventorySystem.Instance;
            if (inv == null) return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            const int cols = 8;
            const float cell = 50f, gap = 6f;
            int idx = 0;
            for (int i = 0; i < InventorySystem.SLOT_COUNT; i++)
            {
                var s = inv.Slots[i];
                if (s.IsEmpty) continue;
                var def = ItemDatabase.Get(s.itemId);
                if (def == null) continue;
                if (def.untradable) continue; // bound items can't be sold

                int col = idx % cols;
                int row = idx / cols;
                idx++;

                var go = new GameObject($"Slot_{i}");
                go.transform.SetParent(_inventoryGrid, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(col * (cell + gap) + 4, -(row * (cell + gap) + 4));
                rt.sizeDelta = new Vector2(cell, cell);
                var img = go.AddComponent<Image>();
                img.color = new Color(def.iconColor.r * 0.35f, def.iconColor.g * 0.35f, def.iconColor.b * 0.35f, 1f);
                var ol = go.AddComponent<Outline>();
                ol.effectColor = AccentDim;
                ol.effectDistance = new Vector2(1, 1);
                var btn = go.AddComponent<Button>();
                string idCap = s.itemId; int qtyCap = s.qty; string nameCap = def.displayName;
                btn.onClick.AddListener(() => SelectInventoryItem(idCap, qtyCap, nameCap));
                AddText(go.transform, "L", def.iconLetter,
                    font, 18, FontStyle.Bold, TextAnchor.MiddleCenter, def.iconColor,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                AddText(go.transform, "Q", s.qty > 1 ? "x" + s.qty : "",
                    font, 9, FontStyle.Bold, TextAnchor.LowerRight, AccentGold,
                    new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 0),
                    new Vector2(-3, 2), new Vector2(30, 12));
            }
        }

        private void SelectInventoryItem(string id, int maxQty, string name)
        {
            _selectedItemId = id;
            _selectedMaxQty = maxQty;
            if (_selectedNameText != null)
                _selectedNameText.text = $"{name}  (최대 수량 {maxQty})";
            if (_qtyInput != null) _qtyInput.text = "1";
        }

        private void OnSubmitRegister()
        {
            if (string.IsNullOrEmpty(_selectedItemId))
            {
                ToastUI.Instance?.Show("아이템을 선택하세요.", new Color(0.95f, 0.55f, 0.30f));
                return;
            }
            if (!int.TryParse(_qtyInput.text, out int qty) || qty <= 0) qty = 1;
            qty = Mathf.Clamp(qty, 1, Mathf.Max(1, _selectedMaxQty));
            if (!long.TryParse(_priceInput.text, out long price) || price <= 0)
            {
                ToastUI.Instance?.Show("가격은 1 이상이어야 합니다.", new Color(0.95f, 0.55f, 0.30f));
                return;
            }
            AuctionSystem.Instance?.Register(_selectedItemId, qty, price);
            CloseRegister();
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
            t.font = font; t.fontSize = size; t.fontStyle = style;
            t.alignment = align; t.color = color; t.text = text;
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
            img.color = AccentGold; img.raycastTarget = false;
        }
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
