using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    /// 'F' opens a friend panel: top is an [add by name] field, below is
    /// the scrollable list of current friends with online/zone indicators
    /// and a remove button per row. Pulls everything from FriendSystem
    /// (DDOL); the panel itself rebuilds rows on every update event so we
    /// don't have to keep handles to individual row widgets.
    public class FriendsUI : MonoBehaviour
    {
        public static FriendsUI Instance { get; private set; }

        [SerializeField] private GameObject panel;
        [SerializeField] private InputField nameField;
        [SerializeField] private Button addButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform listContainer;
        [SerializeField] private Text statusText;

        // Row prefab is generated at runtime by the ProjectSetup builder;
        // we keep one hidden template and clone it.
        [SerializeField] private RectTransform rowTemplate;
        // Optional — section header for 'received requests' and its own
        // row template (accept ✓ / reject × buttons). The builder fills
        // these in; if absent (older build), we silently skip the section.
        [SerializeField] private RectTransform requestsHeader;
        [SerializeField] private RectTransform requestRowTemplate;

        public bool IsOpen => panel != null && panel.activeSelf;

        private readonly List<GameObject> _liveRows = new();

        private void Awake()
        {
            Instance = this;
            if (panel) panel.SetActive(false);
            if (rowTemplate) rowTemplate.gameObject.SetActive(false);
            if (requestRowTemplate) requestRowTemplate.gameObject.SetActive(false);
            if (requestsHeader) requestsHeader.gameObject.SetActive(false);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            if (addButton)   addButton.onClick.AddListener(OnAddClicked);
            if (closeButton) closeButton.onClick.AddListener(Close);
            if (FriendSystem.Instance != null)
            {
                FriendSystem.Instance.OnFriendListUpdated += Rebuild;
                FriendSystem.Instance.OnFriendError       += ShowStatus;
                FriendSystem.Instance.OnAddedBy           += OnAddedBy;
                FriendSystem.Instance.OnRequestFrom       += OnRequestFrom;
            }
        }

        private void OnRequestFrom(string from) { ShowStatus($"{from} 님으로부터 친구 요청이 도착했습니다."); }

        private void Update()
        {
            if (IsTextInputFocused()) return;
            // F toggles the panel. Same modal-aware gating the inventory /
            // skill window use elsewhere — don't fight with other open UIs.
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (IsOpen) Close();
                else if (!AnyOtherModalOpen()) Open();
            }
            else if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public void Open()
        {
            if (panel) panel.SetActive(true);
            ShowStatus("");
            FriendSystem.Instance?.RequestList();
        }
        public void Close() { if (panel) panel.SetActive(false); }

        private void OnAddClicked()
        {
            if (nameField == null) return;
            string n = nameField.text?.Trim();
            if (string.IsNullOrEmpty(n)) return;
            FriendSystem.Instance?.Add(n);
            nameField.text = "";
            // Server will echo a fresh list on success; on failure we get
            // a FriendError that flows into ShowStatus.
        }

        private void OnAddedBy(string by) { ShowStatus($"{by} 님이 친구로 추가했습니다."); }

        private void ShowStatus(string s) { if (statusText) statusText.text = s; }

        private void Rebuild()
        {
            // Drop any prior rows. Linear in friend count; max 50.
            foreach (var go in _liveRows) if (go != null) Destroy(go);
            _liveRows.Clear();

            if (FriendSystem.Instance == null || listContainer == null || rowTemplate == null) return;

            float y = 0f;

            // ── Incoming requests section (rendered only when there are any) ──
            if (requestRowTemplate != null && FriendSystem.Instance.Incoming.Count > 0)
            {
                if (requestsHeader != null)
                {
                    var hdr = Instantiate(requestsHeader.gameObject, listContainer);
                    hdr.SetActive(true);
                    var hRt = hdr.GetComponent<RectTransform>();
                    hRt.anchoredPosition = new Vector2(0, -y);
                    y += 22f;
                    _liveRows.Add(hdr);
                }

                foreach (var from in FriendSystem.Instance.Incoming)
                {
                    var go = Instantiate(requestRowTemplate.gameObject, listContainer);
                    go.SetActive(true);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0, -y);
                    y += 36f;

                    var nameT = go.transform.Find("Name")?.GetComponent<Text>();
                    if (nameT) nameT.text = from;

                    string capture = from;
                    var acceptB = go.transform.Find("AcceptB")?.GetComponent<Button>();
                    if (acceptB)
                    {
                        acceptB.onClick.RemoveAllListeners();
                        acceptB.onClick.AddListener(() => FriendSystem.Instance?.Accept(capture));
                    }
                    var rejectB = go.transform.Find("RejectB")?.GetComponent<Button>();
                    if (rejectB)
                    {
                        rejectB.onClick.RemoveAllListeners();
                        rejectB.onClick.AddListener(() => FriendSystem.Instance?.Reject(capture));
                    }

                    _liveRows.Add(go);
                }

                // Small spacer before the friend list proper.
                y += 6f;
            }

            // ── Friends section ──
            foreach (var f in FriendSystem.Instance.Friends)
            {
                var go = Instantiate(rowTemplate.gameObject, listContainer);
                go.SetActive(true);
                var rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, -y);
                y += 36f;

                // Row template structure (built in ProjectSetup):
                //   Row
                //     Dot     (Image — green when online, grey when offline)
                //     Name    (Text)
                //     Zone    (Text)
                //     RemoveB (Button)
                var dot = go.transform.Find("Dot")?.GetComponent<Image>();
                if (dot)
                    dot.color = f.online ? new Color(0.40f, 0.85f, 0.40f)
                                         : new Color(0.45f, 0.42f, 0.36f);

                var nameT = go.transform.Find("Name")?.GetComponent<Text>();
                if (nameT) nameT.text = f.name;

                var zoneT = go.transform.Find("Zone")?.GetComponent<Text>();
                if (zoneT) zoneT.text = f.online ? PrettyZone(f.zone) : "(offline)";

                string capture = f.name;
                var removeBtn = go.transform.Find("RemoveB")?.GetComponent<Button>();
                if (removeBtn)
                {
                    removeBtn.onClick.RemoveAllListeners();
                    removeBtn.onClick.AddListener(() => FriendSystem.Instance?.Remove(capture));
                }
                // ✎ shortcut: close the friend panel and put '/w <name> ' into
                // the chat input. Cheaper than learning the command and the
                // expected flow once you've added someone to chat with.
                var whisperBtn = go.transform.Find("WhisperB")?.GetComponent<Button>();
                if (whisperBtn)
                {
                    whisperBtn.onClick.RemoveAllListeners();
                    whisperBtn.onClick.AddListener(() => {
                        Close();
                        GameHUD.Instance?.FocusChatWith($"/w {capture} ");
                    });
                }

                _liveRows.Add(go);
            }

            // Resize the container so the scrollview can scroll the whole list.
            var contRect = listContainer;
            contRect.sizeDelta = new Vector2(contRect.sizeDelta.x, Mathf.Max(y, 80f));

            if (FriendSystem.Instance != null && FriendSystem.Instance.Outgoing.Count > 0)
            {
                // Compact info line: 'pending: A, B, C — click 추가 again to cancel'
                if (statusText)
                {
                    var pending = string.Join(", ", FriendSystem.Instance.Outgoing);
                    statusText.text = $"보낸 요청: {pending}";
                }
            }
            else if (statusText && _liveRows.Count == 0)
            {
                statusText.text = "친구가 없습니다. 위 입력란에 닉네임을 입력하세요.";
            }
        }

        private static string PrettyZone(string zone)
        {
            if (string.IsNullOrEmpty(zone)) return "";
            // Sample mappings — full table lives in WorldMapData but we just
            // need quick humanisation here.
            switch (zone)
            {
                case "beacon_of_winds":   return "Beacon of Winds";
                case "solaria":           return "Solaria";
                case "pyresummit":        return "Pyresummit";
                case "verdaglen":         return "Verdaglen";
                case "nightport":         return "Nightport";
                case "tidehaven":         return "Tidehaven";
                case "forgotten_woods":   return "Forgotten Woods";
                case "citadel_of_dawn":   return "Citadel of Dawn";
                default: return zone.Replace('_', ' ');
            }
        }

        private bool IsTextInputFocused()
        {
            if (EventSystem.current == null) return false;
            var sel = EventSystem.current.currentSelectedGameObject;
            return sel != null && sel.GetComponent<InputField>() != null;
        }

        private bool AnyOtherModalOpen()
        {
            var sys = SystemMenuUI.Instance;       if (sys != null && sys.IsOpen) return true;
            var inv = Object.FindObjectOfType<InventoryUI>(); if (inv != null && inv.IsOpen()) return true;
            var ci  = Object.FindObjectOfType<CharacterInfoUI>(); if (ci != null && ci.IsOpen()) return true;
            var sw  = Object.FindObjectOfType<SkillWindowUI>();   if (sw != null && sw.IsOpen()) return true;
            var shop = ShopUI.Instance;             if (shop != null && shop.IsOpen) return true;
            var ql  = QuestLogUI.Instance;          if (ql != null && ql.IsOpen) return true;
            return false;
        }
    }
}
