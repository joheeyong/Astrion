using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform slotsRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button sortButton;
        [SerializeField] private KeyCode toggleKey = KeyCode.I;

        private Image[] _slotIcons;
        private Text[] _slotLetters;
        private Text[] _slotQtys;
        private ItemSlotRef[] _slotRefs;
        private bool _cached;

        private void Awake()
        {
            if (panel) panel.SetActive(false);
        }

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (sortButton != null) sortButton.onClick.AddListener(OnSortClicked);
            CacheSlots();
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.OnChanged += Refresh;
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (InventorySystem.Instance != null)
                InventorySystem.Instance.OnChanged -= Refresh;
        }

        private void CacheSlots()
        {
            if (slotsRoot == null) return;
            _slotIcons = new Image[InventorySystem.SLOT_COUNT];
            _slotLetters = new Text[InventorySystem.SLOT_COUNT];
            _slotQtys = new Text[InventorySystem.SLOT_COUNT];
            _slotRefs = new ItemSlotRef[InventorySystem.SLOT_COUNT];
            for (int i = 0; i < InventorySystem.SLOT_COUNT; i++)
            {
                var slot = slotsRoot.Find($"Slot_{i}");
                if (slot == null) continue;
                _slotIcons[i] = slot.Find("Icon")?.GetComponent<Image>();
                _slotLetters[i] = slot.Find("Icon/Letter")?.GetComponent<Text>();
                _slotQtys[i] = slot.Find("Qty")?.GetComponent<Text>();
                _slotRefs[i] = slot.GetComponent<ItemSlotRef>();
                var btn = slot.GetComponent<Button>();
                if (btn == null) btn = slot.gameObject.AddComponent<Button>();
                int idx = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSlotClicked(idx));
            }
            _cached = true;
        }

        private void OnSlotClicked(int idx)
        {
            InventorySystem.Instance?.UseSlot(idx);
        }

        private void OnSortClicked()
        {
            InventorySystem.Instance?.Compact();
            ToastUI.Instance?.Show("인벤토리 정리 완료", new Color(0.85f, 0.78f, 0.55f));
        }

        private void Update()
        {
            if (IsTextInputFocused()) return;
            if (Input.GetKeyDown(toggleKey)) Toggle();
            else if (Input.GetKeyDown(KeyCode.Escape) && IsOpen()) Close();
        }

        private bool IsTextInputFocused()
        {
            if (EventSystem.current == null) return false;
            var sel = EventSystem.current.currentSelectedGameObject;
            return sel != null && sel.GetComponent<InputField>() != null;
        }

        public bool IsOpen() => panel != null && panel.activeSelf;
        public void Toggle() { if (panel) panel.SetActive(!panel.activeSelf); }
        public void Close() { if (panel) panel.SetActive(false); }

        private void Refresh()
        {
            if (!_cached) CacheSlots();
            var inv = InventorySystem.Instance;
            if (inv == null) return;

            for (int i = 0; i < InventorySystem.SLOT_COUNT; i++)
            {
                if (_slotIcons[i] == null) continue;
                var s = inv.Slots[i];
                if (_slotRefs[i] != null) _slotRefs[i].itemId = s.IsEmpty ? "" : s.itemId;
                if (s.IsEmpty)
                {
                    _slotIcons[i].gameObject.SetActive(false);
                    if (_slotQtys[i] != null) _slotQtys[i].text = "";
                }
                else
                {
                    var def = ItemDatabase.Get(s.itemId);
                    _slotIcons[i].gameObject.SetActive(true);
                    if (def != null)
                    {
                        _slotIcons[i].color = def.iconColor;
                        if (_slotLetters[i] != null) _slotLetters[i].text = def.iconLetter;
                    }
                    if (_slotQtys[i] != null) _slotQtys[i].text = s.qty > 1 ? s.qty.ToString() : "";
                }
            }
        }
    }
}
