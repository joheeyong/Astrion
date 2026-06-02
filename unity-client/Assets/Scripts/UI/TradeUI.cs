using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Astrion.Game;

namespace Astrion.UI
{
    /// Two-sided trade modal. Opens automatically on TRADE_OPEN, rebuilds
    /// on every TRADE_STATE, closes on TRADE_RESULT.
    /// Interaction model:
    ///   • Click an inventory slot while trade is open → that stack goes
    ///     into your first empty trade slot.
    ///   • Click a trade slot you own → server clears it.
    ///   • Type a number in the gold field → server sets it (debounced via
    ///     OnEndEdit so we don't blast a packet per keystroke).
    ///   • [잠금] / [확정] / [취소] buttons mirror the wire packets.
    /// DDOL singleton — sits with the other persistent UI on networkGo.
    public class TradeUI : MonoBehaviour
    {
        public static TradeUI Instance { get; private set; }

        // Reuse the medieval palette.
        private static readonly Color PanelBg     = new Color(0.10f, 0.08f, 0.06f, 0.97f);
        private static readonly Color PanelInner  = new Color(0.13f, 0.10f, 0.07f, 0.85f);
        private static readonly Color AccentGold  = new Color(0.85f, 0.65f, 0.22f, 1f);
        private static readonly Color AccentDim   = new Color(0.85f, 0.65f, 0.22f, 0.30f);
        private static readonly Color TextLight   = new Color(0.94f, 0.88f, 0.74f);
        private static readonly Color TextMuted   = new Color(0.62f, 0.55f, 0.42f);
        private static readonly Color SlotBg      = new Color(0.20f, 0.16f, 0.12f, 1f);
        private static readonly Color LockOk      = new Color(0.45f, 0.85f, 0.45f);
        private static readonly Color LockOff     = new Color(0.85f, 0.45f, 0.30f);

        private const int SLOTS_PER_SIDE = 4;

        // Window root + state
        private CanvasGroup _group;
        private bool _open;

        // Modal — invite from partner
        private CanvasGroup _inviteGroup;
        private Text _inviteText;

        // Header
        private Text _partnerName;
        private Text _statusText;

        // Per-side widgets
        private Text _myLockBadge, _theirLockBadge;
        private InputField _myGoldInput;
        private Text _theirGoldText;
        private Image[] _mySlotBg = new Image[SLOTS_PER_SIDE];
        private Text[]  _mySlotLabel = new Text[SLOTS_PER_SIDE];
        private Text[]  _mySlotQty = new Text[SLOTS_PER_SIDE];
        private Image[] _theirSlotBg = new Image[SLOTS_PER_SIDE];
        private Text[]  _theirSlotLabel = new Text[SLOTS_PER_SIDE];
        private Text[]  _theirSlotQty = new Text[SLOTS_PER_SIDE];

        // Bottom buttons
        private Text _lockBtnLabel;
        private Button _lockBtn, _confirmBtn, _cancelBtn;
        private Image _confirmBtnImg;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
        }

        private void Start()
        {
            var ts = TradeSystem.Instance;
            if (ts != null)
            {
                ts.OnTradeOpen    += OnTradeOpen;
                ts.OnTradeUpdated += Refresh;
                ts.OnTradeClosed  += OnTradeClosed;
                ts.OnRequestFrom  += OnInviteFrom;
                ts.OnError        += OnError;
                ts.OnResult       += OnResult;
            }
        }

        private void OnDestroy()
        {
            var ts = TradeSystem.Instance;
            if (ts != null)
            {
                ts.OnTradeOpen    -= OnTradeOpen;
                ts.OnTradeUpdated -= Refresh;
                ts.OnTradeClosed  -= OnTradeClosed;
                ts.OnRequestFrom  -= OnInviteFrom;
                ts.OnError        -= OnError;
                ts.OnResult       -= OnResult;
            }
            if (Instance == this) Instance = null;
        }

        public bool IsOpen => _open;

        // ── External hook used by InventoryUI: route a click from the
        // inventory into the first empty trade slot.
        public void TryOfferFromInventory(int inventoryIndex)
        {
            if (!_open) return;
            var inv = InventorySystem.Instance;
            var ts = TradeSystem.Instance;
            if (inv == null || ts == null || ts.State == null) return;
            var s = inv.Slots[inventoryIndex];
            if (s.IsEmpty) return;

            // Find next empty slot in my offer side.
            var mySide = ts.IsSideA ? ts.State.aOffer : ts.State.bOffer;
            int target = -1;
            for (int i = 0; i < SLOTS_PER_SIDE; i++)
            {
                if (mySide == null || i >= mySide.Length) { target = i; break; }
                if (mySide[i] == null || string.IsNullOrEmpty(mySide[i].itemId)) { target = i; break; }
            }
            if (target < 0)
            {
                ToastUI.Instance?.Show("거래 슬롯이 가득 찼습니다.", new Color(0.95f, 0.55f, 0.30f));
                return;
            }
            ts.Offer(target, s.itemId, s.qty);
        }

        // ──────────────────────── BUILD ────────────────────────

        private void Build()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("TradeCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9150;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            BuildWindow(canvasGo.transform, font);
            BuildInviteModal(canvasGo.transform, font);
        }

        private void BuildWindow(Transform parent, Font font)
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
            panelRt.sizeDelta = new Vector2(880, 560);
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

            AddText(panelGo.transform, "Eyebrow", "—  T R A D E  —",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, AccentGold,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -22), new Vector2(420, 22));
            AddText(panelGo.transform, "Title", "거래",
                font, 24, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -52), new Vector2(560, 36));

            BuildSide(panelGo.transform, font, isMine: true,
                title: "내 제안",
                anchorX: 0.25f);
            BuildSide(panelGo.transform, font, isMine: false,
                title: "상대 제안",
                anchorX: 0.75f);

            // Center divider
            var div = new GameObject("Divider");
            div.transform.SetParent(panelGo.transform, false);
            var divRt = div.AddComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.5f, 0); divRt.anchorMax = new Vector2(0.5f, 1);
            divRt.pivot = new Vector2(0.5f, 0.5f);
            divRt.offsetMin = new Vector2(0, 120); divRt.offsetMax = new Vector2(0, -100);
            divRt.sizeDelta = new Vector2(1, 0);
            var divImg = div.AddComponent<Image>();
            divImg.color = AccentDim;

            // Bottom button row
            _statusText = AddText(panelGo.transform, "Status", "",
                font, 12, FontStyle.Normal, TextAnchor.MiddleCenter, TextMuted,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 78), new Vector2(800, 18));

            _lockBtn = BuildBottomButton(panelGo.transform, font, "잠금", new Vector2(-150, 30),
                new Color(0.32f, 0.40f, 0.20f, 1f), OnLockClicked, out _lockBtnLabel);
            _confirmBtn = BuildBottomButton(panelGo.transform, font, "확정", new Vector2(0, 30),
                new Color(0.40f, 0.30f, 0.16f, 1f), OnConfirmClicked, out _);
            _confirmBtnImg = _confirmBtn.GetComponent<Image>();
            _cancelBtn = BuildBottomButton(panelGo.transform, font, "취소 [Esc]", new Vector2(150, 30),
                new Color(0.45f, 0.18f, 0.18f, 1f), OnCancelClicked, out _);
        }

        private void BuildSide(Transform parent, Font font, bool isMine, string title, float anchorX)
        {
            // Title bar
            var titleGo = new GameObject(isMine ? "MyTitle" : "TheirTitle");
            titleGo.transform.SetParent(parent, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(anchorX, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = new Vector2(0, -100);
            titleRt.sizeDelta = new Vector2(380, 26);
            var nameT = titleGo.AddComponent<Text>();
            nameT.font = font;
            nameT.fontSize = 14;
            nameT.fontStyle = FontStyle.Bold;
            nameT.alignment = TextAnchor.MiddleLeft;
            nameT.color = AccentGold;
            nameT.text = title;
            if (!isMine) _partnerName = nameT;

            // Lock badge
            var badgeGo = new GameObject("Lock");
            badgeGo.transform.SetParent(titleGo.transform, false);
            var badgeRt = badgeGo.AddComponent<RectTransform>();
            badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(1, 0.5f);
            badgeRt.pivot = new Vector2(1, 0.5f);
            badgeRt.anchoredPosition = new Vector2(-4, 0);
            badgeRt.sizeDelta = new Vector2(80, 20);
            var badgeT = badgeGo.AddComponent<Text>();
            badgeT.font = font;
            badgeT.fontSize = 11;
            badgeT.fontStyle = FontStyle.Bold;
            badgeT.alignment = TextAnchor.MiddleRight;
            badgeT.color = LockOff;
            badgeT.text = "○ 미잠금";
            if (isMine) _myLockBadge = badgeT; else _theirLockBadge = badgeT;

            // Slots — 2x2 grid
            float slotSize = 76f; float gap = 8f;
            for (int i = 0; i < SLOTS_PER_SIDE; i++)
            {
                int col = i % 2; int row = i / 2;
                Vector2 pos = new Vector2(
                    -slotSize/2f - gap/2f + col * (slotSize + gap),
                    -150f - row * (slotSize + gap));
                BuildTradeSlot(parent, font, isMine, i, anchorX, pos, slotSize);
            }

            // Gold field
            float goldY = -150f - 2 * (slotSize + gap) - 10f;
            if (isMine)
            {
                var goldGo = new GameObject("MyGold");
                goldGo.transform.SetParent(parent, false);
                var goldRt = goldGo.AddComponent<RectTransform>();
                goldRt.anchorMin = goldRt.anchorMax = new Vector2(anchorX, 1);
                goldRt.pivot = new Vector2(0.5f, 0.5f);
                goldRt.anchoredPosition = new Vector2(0, goldY);
                goldRt.sizeDelta = new Vector2(280, 32);
                var goldBg = goldGo.AddComponent<Image>();
                goldBg.color = SlotBg;
                var goldOl = goldGo.AddComponent<Outline>();
                goldOl.effectColor = AccentDim;
                goldOl.effectDistance = new Vector2(1, 1);

                AddText(goldGo.transform, "L", "골드:",
                    font, 12, FontStyle.Bold, TextAnchor.MiddleLeft, AccentGold,
                    new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                    new Vector2(10, 0), new Vector2(60, 0));

                var inputGo = new GameObject("Input");
                inputGo.transform.SetParent(goldGo.transform, false);
                var inputRt = inputGo.AddComponent<RectTransform>();
                inputRt.anchorMin = new Vector2(0, 0); inputRt.anchorMax = new Vector2(1, 1);
                inputRt.offsetMin = new Vector2(72, 4); inputRt.offsetMax = new Vector2(-8, -4);
                var inputBg = inputGo.AddComponent<Image>();
                inputBg.color = new Color(0.06f, 0.05f, 0.04f, 0.92f);
                _myGoldInput = inputGo.AddComponent<InputField>();
                _myGoldInput.contentType = InputField.ContentType.IntegerNumber;
                _myGoldInput.targetGraphic = inputBg;
                var inputTextGo = new GameObject("Text");
                inputTextGo.transform.SetParent(inputGo.transform, false);
                var inputTextRt = inputTextGo.AddComponent<RectTransform>();
                inputTextRt.anchorMin = Vector2.zero; inputTextRt.anchorMax = Vector2.one;
                inputTextRt.offsetMin = new Vector2(8, 0); inputTextRt.offsetMax = new Vector2(-8, 0);
                var inputText = inputTextGo.AddComponent<Text>();
                inputText.font = font; inputText.fontSize = 13;
                inputText.alignment = TextAnchor.MiddleLeft;
                inputText.color = TextLight;
                _myGoldInput.textComponent = inputText;
                _myGoldInput.onEndEdit.AddListener(OnGoldEndEdit);
            }
            else
            {
                var goldGo = new GameObject("TheirGold");
                goldGo.transform.SetParent(parent, false);
                var goldRt = goldGo.AddComponent<RectTransform>();
                goldRt.anchorMin = goldRt.anchorMax = new Vector2(anchorX, 1);
                goldRt.pivot = new Vector2(0.5f, 0.5f);
                goldRt.anchoredPosition = new Vector2(0, goldY);
                goldRt.sizeDelta = new Vector2(280, 32);
                var goldBg = goldGo.AddComponent<Image>();
                goldBg.color = SlotBg;
                var goldOl = goldGo.AddComponent<Outline>();
                goldOl.effectColor = AccentDim;
                goldOl.effectDistance = new Vector2(1, 1);

                AddText(goldGo.transform, "L", "골드:",
                    font, 12, FontStyle.Bold, TextAnchor.MiddleLeft, AccentGold,
                    new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                    new Vector2(10, 0), new Vector2(60, 0));

                _theirGoldText = AddText(goldGo.transform, "V", "0",
                    font, 13, FontStyle.Bold, TextAnchor.MiddleLeft, TextLight,
                    new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                    new Vector2(74, 0), Vector2.zero);
            }
        }

        private void BuildTradeSlot(Transform parent, Font font, bool isMine, int index,
            float anchorX, Vector2 pos, float size)
        {
            var go = new GameObject($"{(isMine ? "My" : "Their")}Slot_{index}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(anchorX, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            var bg = go.AddComponent<Image>();
            bg.color = SlotBg;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = AccentDim;
            ol.effectDistance = new Vector2(1, 1);

            var lblT = AddText(go.transform, "Lbl", "",
                font, 18, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            var qtyT = AddText(go.transform, "Qty", "",
                font, 11, FontStyle.Bold, TextAnchor.LowerRight, AccentGold,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-4, 2), new Vector2(40, 16));

            if (isMine)
            {
                var btn = go.AddComponent<Button>();
                int capture = index;
                btn.onClick.AddListener(() => OnMySlotClicked(capture));
                _mySlotBg[index] = bg;
                _mySlotLabel[index] = lblT;
                _mySlotQty[index] = qtyT;
            }
            else
            {
                _theirSlotBg[index] = bg;
                _theirSlotLabel[index] = lblT;
                _theirSlotQty[index] = qtyT;
            }
        }

        private Button BuildBottomButton(Transform parent, Font font, string label, Vector2 pos,
            Color tint, System.Action onClick, out Text lblOut)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(130, 38);
            var img = go.AddComponent<Image>();
            img.color = tint;
            var ol = go.AddComponent<Outline>();
            ol.effectColor = AccentDim;
            ol.effectDistance = new Vector2(1, 1);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            lblOut = AddText(go.transform, "L", label,
                font, 14, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 0.95f, 0.85f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            return btn;
        }

        private void BuildInviteModal(Transform parent, Font font)
        {
            var modalGo = new GameObject("InviteModal");
            modalGo.transform.SetParent(parent, false);
            var modalRt = modalGo.AddComponent<RectTransform>();
            Stretch(modalRt);
            _inviteGroup = modalGo.AddComponent<CanvasGroup>();
            _inviteGroup.alpha = 0f;
            _inviteGroup.blocksRaycasts = false;

            var dim = modalGo.AddComponent<Image>();
            dim.color = new Color(0, 0, 0, 0.55f);

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(modalGo.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(440, 160);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = PanelBg;
            var panelOl = panelGo.AddComponent<Outline>();
            panelOl.effectColor = AccentDim;
            panelOl.effectDistance = new Vector2(1, 1);

            AddText(panelGo.transform, "Eyebrow", "—  T R A D E   I N V I T E  —",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, AccentGold,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -16), new Vector2(420, 22));

            _inviteText = AddText(panelGo.transform, "Body", "",
                font, 16, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 14), new Vector2(420, 22));

            BuildBottomButton(panelGo.transform, font, "수락 [Y]", new Vector2(-66, 22),
                new Color(0.20f, 0.45f, 0.20f, 1f), () => {
                    TradeSystem.Instance?.AcceptPending();
                    HideInvite();
                }, out _);
            BuildBottomButton(panelGo.transform, font, "거절 [N]", new Vector2(66, 22),
                new Color(0.45f, 0.18f, 0.18f, 1f), () => {
                    TradeSystem.Instance?.RejectPending();
                    HideInvite();
                }, out _);
        }

        // ──────────────────────── EVENT WIRING ────────────────────────

        private void OnTradeOpen()
        {
            _open = true;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            if (_myGoldInput != null) _myGoldInput.text = "0";
            if (_statusText != null) _statusText.text = "교환할 아이템을 인벤토리에서 클릭, 골드를 입력하시오.";
        }

        private void OnTradeClosed()
        {
            _open = false;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        private void OnInviteFrom(string from)
        {
            if (_inviteText != null)
                _inviteText.text = $"{from} 님이 거래를 신청했습니다.";
            ShowInvite();
        }

        private void OnError(string msg)
        {
            ToastUI.Instance?.Show($"[거래]  {msg}", new Color(0.95f, 0.55f, 0.30f));
        }

        private void OnResult(TradeSystem.ResultBundle r)
        {
            if (r.success)
            {
                int items = r.gained != null ? r.gained.Count : 0;
                ToastUI.Instance?.Show(
                    items > 0 || r.gainedGold > 0
                        ? $"[거래 성사]  획득: {items}종 + {r.gainedGold:N0} G"
                        : "[거래 성사]",
                    new Color(0.55f, 0.85f, 0.45f));
            }
            else
            {
                ToastUI.Instance?.Show($"[거래 취소]  {r.message}",
                    new Color(0.85f, 0.55f, 0.40f));
            }
        }

        private void ShowInvite()
        {
            _inviteGroup.alpha = 1f;
            _inviteGroup.blocksRaycasts = true;
        }

        private void HideInvite()
        {
            _inviteGroup.alpha = 0f;
            _inviteGroup.blocksRaycasts = false;
        }

        private void Update()
        {
            if (_inviteGroup != null && _inviteGroup.blocksRaycasts)
            {
                if (Input.GetKeyDown(KeyCode.Y))
                {
                    TradeSystem.Instance?.AcceptPending();
                    HideInvite();
                }
                else if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Escape))
                {
                    TradeSystem.Instance?.RejectPending();
                    HideInvite();
                }
                return;
            }
            if (_open && Input.GetKeyDown(KeyCode.Escape) && !IsAnyInputFocused())
            {
                TradeSystem.Instance?.Cancel();
            }
        }

        private bool IsAnyInputFocused()
        {
            if (EventSystem.current == null) return false;
            var sel = EventSystem.current.currentSelectedGameObject;
            return sel != null && sel.GetComponent<InputField>() != null;
        }

        // ──────────────────────── INPUT HANDLERS ────────────────────────

        private void OnMySlotClicked(int index)
        {
            TradeSystem.Instance?.ClearSlot(index);
        }

        private void OnGoldEndEdit(string txt)
        {
            if (!long.TryParse(txt, out long g)) g = 0;
            TradeSystem.Instance?.SetGold(g);
        }

        private void OnLockClicked()
        {
            var ts = TradeSystem.Instance;
            if (ts == null || ts.State == null) return;
            bool mine = ts.IsSideA ? ts.State.aLocked : ts.State.bLocked;
            if (mine) ts.Unlock(); else ts.Lock();
        }

        private void OnConfirmClicked() => TradeSystem.Instance?.Confirm();
        private void OnCancelClicked()  => TradeSystem.Instance?.Cancel();

        // ──────────────────────── REFRESH ────────────────────────

        private void Refresh()
        {
            var ts = TradeSystem.Instance;
            if (ts == null || ts.State == null) return;
            bool isA = ts.IsSideA;
            var mySide   = isA ? ts.State.aOffer : ts.State.bOffer;
            var theirSide = isA ? ts.State.bOffer : ts.State.aOffer;
            long myGold  = isA ? ts.State.aGold : ts.State.bGold;
            long theirG  = isA ? ts.State.bGold : ts.State.aGold;
            bool myLocked = isA ? ts.State.aLocked : ts.State.bLocked;
            bool theirLocked = isA ? ts.State.bLocked : ts.State.aLocked;
            bool myConfirmed = isA ? ts.State.aConfirmed : ts.State.bConfirmed;
            bool theirConfirmed = isA ? ts.State.bConfirmed : ts.State.aConfirmed;

            if (_partnerName != null) _partnerName.text = $"{ts.Partner} 의 제안";

            RenderSide(mySide, _mySlotBg, _mySlotLabel, _mySlotQty);
            RenderSide(theirSide, _theirSlotBg, _theirSlotLabel, _theirSlotQty);

            if (_theirGoldText != null) _theirGoldText.text = $"{theirG:N0}";
            if (_myGoldInput != null && !_myGoldInput.isFocused)
            {
                string want = myGold.ToString();
                if (_myGoldInput.text != want) _myGoldInput.text = want;
            }

            if (_myLockBadge != null) {
                _myLockBadge.text = myLocked ? "● 잠금" : "○ 미잠금";
                _myLockBadge.color = myLocked ? LockOk : LockOff;
            }
            if (_theirLockBadge != null) {
                _theirLockBadge.text = theirLocked ? "● 잠금" : "○ 미잠금";
                _theirLockBadge.color = theirLocked ? LockOk : LockOff;
            }

            if (_lockBtnLabel != null) _lockBtnLabel.text = myLocked ? "잠금 해제" : "잠금";

            bool canConfirm = myLocked && theirLocked;
            _confirmBtn.interactable = canConfirm;
            if (_confirmBtnImg != null)
                _confirmBtnImg.color = canConfirm
                    ? new Color(0.40f, 0.55f, 0.22f, 1f)
                    : new Color(0.22f, 0.18f, 0.14f, 1f);

            if (_statusText != null)
            {
                string s;
                if (myConfirmed && theirConfirmed)         s = "거래 진행 중...";
                else if (myConfirmed)                       s = "당신은 확정했습니다. 상대 확정 대기.";
                else if (theirConfirmed)                    s = $"{ts.Partner} 가 확정했습니다. 검토 후 확정하시오.";
                else if (myLocked && theirLocked)           s = "양쪽 잠금 완료 — 확정 버튼이 활성화되었습니다.";
                else if (myLocked)                          s = "당신은 잠금됨. 상대 잠금 대기.";
                else if (theirLocked)                       s = $"{ts.Partner} 가 잠금. 검토 후 잠금하시오.";
                else                                        s = "교환할 아이템을 인벤토리에서 클릭, 골드를 입력하시오.";
                _statusText.text = s;
            }
        }

        private void RenderSide(TradeSystem.Slot[] side, Image[] bg, Text[] lbl, Text[] qty)
        {
            for (int i = 0; i < SLOTS_PER_SIDE; i++)
            {
                bool has = side != null && i < side.Length
                    && side[i] != null && !string.IsNullOrEmpty(side[i].itemId);
                if (has)
                {
                    var def = ItemDatabase.Get(side[i].itemId);
                    if (def != null)
                    {
                        bg[i].color = new Color(def.iconColor.r * 0.4f, def.iconColor.g * 0.4f, def.iconColor.b * 0.4f, 1f);
                        lbl[i].text = def.iconLetter;
                        lbl[i].color = def.iconColor;
                    }
                    else
                    {
                        bg[i].color = SlotBg;
                        lbl[i].text = "?";
                    }
                    qty[i].text = side[i].qty > 1 ? "x" + side[i].qty : "";
                }
                else
                {
                    bg[i].color = SlotBg;
                    lbl[i].text = "";
                    qty[i].text = "";
                }
            }
        }

        // ──────────────────────── PRIMITIVES ────────────────────────

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
