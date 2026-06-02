using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Astrion.Game;

namespace Astrion.UI
{
    /// Simple list of currently-blocked usernames + an [unblock] button per
    /// row. Opens with B (mnemonic: Block). DDOL singleton, built once on
    /// the login scene.
    public class BlockListUI : MonoBehaviour
    {
        public static BlockListUI Instance { get; private set; }

        private static readonly Color PanelBg     = new Color(0.10f, 0.08f, 0.06f, 0.96f);
        private static readonly Color PanelInner  = new Color(0.13f, 0.10f, 0.07f, 0.85f);
        private static readonly Color AccentGold  = new Color(0.85f, 0.65f, 0.22f, 1f);
        private static readonly Color AccentDim   = new Color(0.85f, 0.65f, 0.22f, 0.30f);
        private static readonly Color TextLight   = new Color(0.94f, 0.88f, 0.74f);
        private static readonly Color TextMuted   = new Color(0.62f, 0.55f, 0.42f);
        private static readonly Color RowBg       = new Color(0.16f, 0.13f, 0.10f, 1f);

        private CanvasGroup _group;
        private bool _open;
        private RectTransform _listRoot;
        private Text _status;
        private InputField _addInput;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
        }

        private void Start()
        {
            if (BlockSystem.Instance != null)
                BlockSystem.Instance.OnUpdated += Refresh;
        }

        private void OnDestroy()
        {
            if (BlockSystem.Instance != null)
                BlockSystem.Instance.OnUpdated -= Refresh;
            if (Instance == this) Instance = null;
        }

        public void Toggle()
        {
            if (_open) Close(); else Open();
        }

        public void Open()
        {
            _open = true;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            BlockSystem.Instance?.RequestList();
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
            // B opens/closes. The kbd lives on networkGo's singleton so it
            // works from any scene. Suppressed while text input is focused.
            if (Input.GetKeyDown(KeyCode.B)) Toggle();
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

            var canvasGo = new GameObject("BlockCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9080;
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
            panelRt.sizeDelta = new Vector2(500, 540);
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

            AddText(panelGo.transform, "Eyebrow", "—  B L O C K   L I S T  —",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, AccentGold,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -22), new Vector2(420, 22));
            AddText(panelGo.transform, "Title", "차단 목록",
                font, 22, FontStyle.Bold, TextAnchor.MiddleCenter, TextLight,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -52), new Vector2(420, 32));

            // Add field
            var addRow = new GameObject("Add");
            addRow.transform.SetParent(panelGo.transform, false);
            var addRt = addRow.AddComponent<RectTransform>();
            addRt.anchorMin = new Vector2(0, 1); addRt.anchorMax = new Vector2(1, 1);
            addRt.pivot = new Vector2(0.5f, 1);
            addRt.offsetMin = new Vector2(30, 0); addRt.offsetMax = new Vector2(-30, 0);
            addRt.sizeDelta = new Vector2(0, 36);
            addRt.anchoredPosition = new Vector2(0, -100);

            var inputGo = new GameObject("Input");
            inputGo.transform.SetParent(addRow.transform, false);
            var inputRt = inputGo.AddComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0, 0); inputRt.anchorMax = new Vector2(1, 1);
            inputRt.offsetMin = new Vector2(0, 0); inputRt.offsetMax = new Vector2(-96, 0);
            var inputBg = inputGo.AddComponent<Image>();
            inputBg.color = new Color(0.06f, 0.05f, 0.04f, 0.92f);
            var inputOl = inputGo.AddComponent<Outline>();
            inputOl.effectColor = AccentDim;
            inputOl.effectDistance = new Vector2(1, 1);
            _addInput = inputGo.AddComponent<InputField>();
            _addInput.targetGraphic = inputBg;
            var inputTextGo = new GameObject("Text");
            inputTextGo.transform.SetParent(inputGo.transform, false);
            var inputTextRt = inputTextGo.AddComponent<RectTransform>();
            inputTextRt.anchorMin = Vector2.zero; inputTextRt.anchorMax = Vector2.one;
            inputTextRt.offsetMin = new Vector2(8, 0); inputTextRt.offsetMax = new Vector2(-8, 0);
            var inputText = inputTextGo.AddComponent<Text>();
            inputText.font = font; inputText.fontSize = 13;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.color = TextLight;
            _addInput.textComponent = inputText;
            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(inputGo.transform, false);
            var pRt = placeholderGo.AddComponent<RectTransform>();
            pRt.anchorMin = Vector2.zero; pRt.anchorMax = Vector2.one;
            pRt.offsetMin = new Vector2(8, 0); pRt.offsetMax = new Vector2(-8, 0);
            var pTxt = placeholderGo.AddComponent<Text>();
            pTxt.font = font; pTxt.fontSize = 13;
            pTxt.alignment = TextAnchor.MiddleLeft;
            pTxt.color = TextMuted;
            pTxt.text = "차단할 닉네임";
            _addInput.placeholder = pTxt;

            var addBtnGo = new GameObject("AddBtn");
            addBtnGo.transform.SetParent(addRow.transform, false);
            var addBtnRt = addBtnGo.AddComponent<RectTransform>();
            addBtnRt.anchorMin = new Vector2(1, 0); addBtnRt.anchorMax = new Vector2(1, 1);
            addBtnRt.pivot = new Vector2(1, 0.5f);
            addBtnRt.offsetMin = new Vector2(-90, 0); addBtnRt.offsetMax = new Vector2(0, 0);
            addBtnRt.sizeDelta = new Vector2(90, 0);
            var addBtnImg = addBtnGo.AddComponent<Image>();
            addBtnImg.color = new Color(0.40f, 0.20f, 0.20f, 1f);
            var addBtn = addBtnGo.AddComponent<Button>();
            addBtn.onClick.AddListener(OnAddClicked);
            AddText(addBtnGo.transform, "L", "차단",
                font, 13, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.85f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // List container
            _listRoot = new GameObject("List").AddComponent<RectTransform>();
            _listRoot.transform.SetParent(panelGo.transform, false);
            _listRoot.anchorMin = new Vector2(0, 1); _listRoot.anchorMax = new Vector2(1, 1);
            _listRoot.pivot = new Vector2(0.5f, 1);
            _listRoot.offsetMin = new Vector2(30, 0); _listRoot.offsetMax = new Vector2(-30, 0);
            _listRoot.sizeDelta = new Vector2(0, 350);
            _listRoot.anchoredPosition = new Vector2(0, -150);

            _status = AddText(panelGo.transform, "Status", "",
                font, 11, FontStyle.Normal, TextAnchor.MiddleCenter, TextMuted,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 70), new Vector2(420, 18));

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

        // ──────────────────── ACTIONS ────────────────────

        private void OnAddClicked()
        {
            string name = _addInput.text?.Trim();
            if (string.IsNullOrEmpty(name)) return;
            BlockSystem.Instance?.Block(name);
            _addInput.text = "";
        }

        private void Refresh()
        {
            if (_listRoot == null) return;
            // Drop existing rows (linear in block-list size; capped at 100).
            for (int i = _listRoot.childCount - 1; i >= 0; i--)
                Destroy(_listRoot.GetChild(i).gameObject);

            var bs = BlockSystem.Instance;
            if (bs == null || bs.Blocked.Count == 0)
            {
                if (_status != null) _status.text = "차단한 사용자가 없습니다.";
                return;
            }
            if (_status != null) _status.text = $"총 {bs.Blocked.Count} 명 차단 중";

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            float y = 0f;
            foreach (var name in bs.Blocked)
            {
                var rgo = new GameObject($"Row_{name}");
                rgo.transform.SetParent(_listRoot, false);
                var rrt = rgo.AddComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(1, 1);
                rrt.pivot = new Vector2(0.5f, 1);
                rrt.offsetMin = new Vector2(0, 0); rrt.offsetMax = new Vector2(0, 0);
                rrt.sizeDelta = new Vector2(0, 32);
                rrt.anchoredPosition = new Vector2(0, -y);
                var bg = rgo.AddComponent<Image>();
                bg.color = RowBg;

                AddText(rgo.transform, "N", name,
                    font, 13, FontStyle.Bold, TextAnchor.MiddleLeft, TextLight,
                    new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                    new Vector2(12, 0), new Vector2(280, 0));

                var ubGo = new GameObject("UnblockB");
                ubGo.transform.SetParent(rgo.transform, false);
                var ubRt = ubGo.AddComponent<RectTransform>();
                ubRt.anchorMin = new Vector2(1, 0); ubRt.anchorMax = new Vector2(1, 1);
                ubRt.pivot = new Vector2(1, 0.5f);
                ubRt.offsetMin = new Vector2(-110, 4); ubRt.offsetMax = new Vector2(-6, -4);
                ubRt.sizeDelta = new Vector2(104, 0);
                var ubImg = ubGo.AddComponent<Image>();
                ubImg.color = new Color(0.32f, 0.40f, 0.20f, 1f);
                var ubBtn = ubGo.AddComponent<Button>();
                string capture = name;
                ubBtn.onClick.AddListener(() => BlockSystem.Instance?.Unblock(capture));
                AddText(ubGo.transform, "L", "차단 해제",
                    font, 12, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.85f),
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                y += 36f;
            }
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
