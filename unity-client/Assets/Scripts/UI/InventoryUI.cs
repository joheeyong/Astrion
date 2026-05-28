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
        [SerializeField] private Transform tabsRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button sortButton;
        [SerializeField] private Sprite tabActiveSprite;
        [SerializeField] private Sprite tabInactiveSprite;
        [SerializeField] private KeyCode toggleKey = KeyCode.I;

        // Tab order matches ProjectSetup: 장비, 소비, 기타, 설치, 캐쉬
        private const int TAB_COUNT = 5;
        private const int TAB_EQUIP = 0;
        private const int TAB_USE = 1;
        private const int TAB_ETC = 2;
        private const int TAB_INSTALL = 3;
        private const int TAB_CASH = 4;

        private Image[] _slotIcons;
        private Text[] _slotLetters;
        private Text[] _slotQtys;
        private ItemSlotRef[] _slotRefs;
        private Image[] _tabBgs;
        private Text[] _tabLabels;
        private int _currentTab = TAB_EQUIP;
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
            CacheTabs();
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
                // Right-click routes to drop handler
                var rc = slot.gameObject.GetComponent<RightClickRouter>();
                if (rc == null) rc = slot.gameObject.AddComponent<RightClickRouter>();
                rc.slotIdx = idx;
                rc.owner = this;
            }
            _cached = true;
        }

        public void OnSlotRightClicked(int idx)
        {
            if (idx < 0 || idx >= InventorySystem.SLOT_COUNT) return;
            var inv = InventorySystem.Instance;
            if (inv == null) return;
            var s = inv.Slots[idx];
            if (s.IsEmpty) return;
            var def = ItemDatabase.Get(s.itemId);
            if (def == null) return;
            if (TabOf(def.itemType) != _currentTab) return;

            // Bound / untradable items can't be dropped — guards against
            // accidentally trashing a one-shot event grant (the compass was
            // an easy victim before, since right-click in many MMOs means
            // 'use' not 'drop').
            if (def.untradable)
            {
                ToastUI.Instance?.Show("귀속/이벤트 아이템은 버릴 수 없습니다.",
                    new Color(0.95f, 0.55f, 0.30f));
                return;
            }

            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            string name = def.displayName;
            Color tint = new Color(0.78f, 0.72f, 0.55f);
            if (shift)
            {
                int total = s.qty;
                inv.Clear(idx);
                ToastUI.Instance?.Show($"[버림]  {name} × {total}", tint);
            }
            else
            {
                inv.RemoveOneFromSlot(idx);
                ToastUI.Instance?.Show($"[버림]  {name}", tint);
            }
        }

        private class RightClickRouter : MonoBehaviour, IPointerClickHandler
        {
            public int slotIdx;
            public InventoryUI owner;
            public void OnPointerClick(PointerEventData e)
            {
                if (owner != null && e.button == PointerEventData.InputButton.Right)
                    owner.OnSlotRightClicked(slotIdx);
            }
        }

        private void CacheTabs()
        {
            if (tabsRoot == null) return;
            _tabBgs = new Image[TAB_COUNT];
            _tabLabels = new Text[TAB_COUNT];
            for (int i = 0; i < TAB_COUNT; i++)
            {
                var tab = tabsRoot.Find($"Tab_{i}");
                if (tab == null) continue;
                _tabBgs[i] = tab.GetComponent<Image>();
                _tabLabels[i] = tab.Find("Label")?.GetComponent<Text>();
                var btn = tab.GetComponent<Button>();
                if (btn == null) btn = tab.gameObject.AddComponent<Button>();
                int idx = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SetTab(idx));
            }
        }

        private void SetTab(int idx)
        {
            if (idx < 0 || idx >= TAB_COUNT) return;
            _currentTab = idx;
            RefreshTabVisuals();
            Refresh();
        }

        private void RefreshTabVisuals()
        {
            if (_tabBgs == null) return;
            for (int i = 0; i < TAB_COUNT; i++)
            {
                bool active = i == _currentTab;
                if (_tabBgs[i] != null)
                {
                    // Toggle sprite directly — color-mul over a tinted sprite hid the diff
                    if (active && tabActiveSprite != null) _tabBgs[i].sprite = tabActiveSprite;
                    else if (!active && tabInactiveSprite != null) _tabBgs[i].sprite = tabInactiveSprite;
                    _tabBgs[i].color = Color.white;
                }
                if (_tabLabels[i] != null)
                    _tabLabels[i].color = active
                        ? new Color(0.10f, 0.07f, 0.04f)
                        : new Color(0.78f, 0.72f, 0.55f);
            }
        }

        private void OnSlotClicked(int idx)
        {
            if (idx < 0 || idx >= InventorySystem.SLOT_COUNT) return;
            // Only allow use when the slot's item belongs to the active tab
            var inv = InventorySystem.Instance;
            if (inv == null) return;
            var s = inv.Slots[idx];
            if (s.IsEmpty) return;
            var def = ItemDatabase.Get(s.itemId);
            if (def == null) return;
            if (TabOf(def.itemType) != _currentTab) return;
            inv.UseSlot(idx);
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

        private static int TabOf(string itemType)
        {
            switch (itemType)
            {
                case "장비": return TAB_EQUIP;
                case "소비": return TAB_USE;
                case "기타": return TAB_ETC;
                case "상자": return TAB_ETC;   // boxes live under 기타
                case "설치": return TAB_INSTALL;
                case "캐쉬": return TAB_CASH;
                default:     return TAB_ETC;
            }
        }

        private void Refresh()
        {
            if (!_cached) CacheSlots();
            RefreshTabVisuals();
            var inv = InventorySystem.Instance;
            if (inv == null) return;

            for (int i = 0; i < InventorySystem.SLOT_COUNT; i++)
            {
                if (_slotIcons[i] == null) continue;
                var s = inv.Slots[i];
                bool visible = !s.IsEmpty;
                if (visible)
                {
                    var def = ItemDatabase.Get(s.itemId);
                    if (def != null && TabOf(def.itemType) != _currentTab) visible = false;
                }

                if (_slotRefs[i] != null) _slotRefs[i].itemId = visible ? s.itemId : "";

                if (!visible)
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
