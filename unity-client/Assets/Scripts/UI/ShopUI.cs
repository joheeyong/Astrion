using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Astrion.Game;

namespace Astrion.UI
{
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }

        [System.Serializable]
        public class ShopEntry
        {
            public string itemId;
            public int price;
        }

        [SerializeField] private GameObject panel;
        [SerializeField] private Text headerText;
        [SerializeField] private Text goldText;
        [SerializeField] private Transform rowsRoot;
        [SerializeField] private Button closeButton;

        // Sell tab
        [SerializeField] private Button buyTabButton;
        [SerializeField] private Button sellTabButton;
        [SerializeField] private Transform sellGridRoot;

        // Default catalogue
        [SerializeField] private List<ShopEntry> defaultCatalog = new List<ShopEntry>();

        public bool IsOpen => panel != null && panel.activeSelf;

        private bool _sellMode;
        private Image[] _sellIcons;
        private Text[] _sellLetters;
        private Text[] _sellQtys;
        private Text[] _sellPrices;
        private ItemSlotRef[] _sellRefs;

        private void Awake()
        {
            Instance = this;
            if (panel) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnChanged += RefreshGold;
            if (InventorySystem.Instance != null) InventorySystem.Instance.OnChanged += RefreshSellGrid;

            if (buyTabButton != null) buyTabButton.onClick.AddListener(() => SetTab(false));
            if (sellTabButton != null) sellTabButton.onClick.AddListener(() => SetTab(true));

            if (defaultCatalog == null || defaultCatalog.Count == 0)
            {
                defaultCatalog = new List<ShopEntry>
                {
                    new ShopEntry { itemId = "bread",          price = 15 },
                    new ShopEntry { itemId = "elixir",         price = 30 },
                    new ShopEntry { itemId = "bronze_dagger",  price = 80 },
                    new ShopEntry { itemId = "leather_helmet", price = 150 },
                    new ShopEntry { itemId = "iron_dagger",    price = 300 },
                };
            }
            WireRows();
            CacheSellSlots();
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        public void Open(string vendorName)
        {
            if (headerText) headerText.text = $"상점  ·  {vendorName}";
            if (panel) panel.SetActive(true);
            SetTab(false); // always start on Buy
            RefreshGold();
            RefreshRows();
        }

        public void Close()
        {
            if (panel) panel.SetActive(false);
        }

        private void SetTab(bool sellMode)
        {
            _sellMode = sellMode;
            if (rowsRoot != null) rowsRoot.gameObject.SetActive(!sellMode);
            if (sellGridRoot != null) sellGridRoot.gameObject.SetActive(sellMode);
            UpdateTabVisual();
            if (sellMode) RefreshSellGrid();
        }

        private void UpdateTabVisual()
        {
            if (buyTabButton != null)
            {
                var img = buyTabButton.GetComponent<Image>();
                if (img != null) img.color = _sellMode ? new Color(0.30f, 0.24f, 0.16f, 1f) : new Color(0.85f, 0.65f, 0.22f, 1f);
            }
            if (sellTabButton != null)
            {
                var img = sellTabButton.GetComponent<Image>();
                if (img != null) img.color = _sellMode ? new Color(0.85f, 0.65f, 0.22f, 1f) : new Color(0.30f, 0.24f, 0.16f, 1f);
            }
        }

        private void RefreshGold()
        {
            if (goldText == null) return;
            int g = PlayerStats.Instance != null ? PlayerStats.Instance.Gold : 0;
            goldText.text = $"보유  ◆ {g:N0} G";
        }

        private void WireRows()
        {
            if (rowsRoot == null) return;
            for (int i = 0; i < defaultCatalog.Count; i++)
            {
                var row = rowsRoot.Find($"Row_{i}");
                if (row == null) continue;
                var buyBtn = row.Find("BuyBtn")?.GetComponent<Button>();
                if (buyBtn != null)
                {
                    int captured = i;
                    buyBtn.onClick.RemoveAllListeners();
                    buyBtn.onClick.AddListener(() => OnBuyClicked(captured));
                }
            }
        }

        private void RefreshRows()
        {
            if (rowsRoot == null) return;
            for (int i = 0; i < defaultCatalog.Count; i++)
            {
                var row = rowsRoot.Find($"Row_{i}");
                if (row == null) continue;
                var entry = defaultCatalog[i];
                var def = ItemDatabase.Get(entry.itemId);

                var nameT = row.Find("Name")?.GetComponent<Text>();
                if (nameT != null) nameT.text = def != null ? def.displayName : entry.itemId;

                var descT = row.Find("Desc")?.GetComponent<Text>();
                if (descT != null)
                {
                    if (def != null)
                    {
                        if (def.baseDamage > 0) descT.text = $"무기 · 공격력 +{def.baseDamage}";
                        else if (def.healAmount > 0) descT.text = $"소비 · HP +{def.healAmount}";
                        else if (def.manaAmount > 0) descT.text = $"소비 · MP +{def.manaAmount}";
                        else descT.text = def.itemType ?? "";
                    }
                    else descT.text = "";
                }

                var iconImg = row.Find("Icon")?.GetComponent<Image>();
                var iconLetter = row.Find("Icon/Letter")?.GetComponent<Text>();
                if (def != null)
                {
                    if (iconImg != null) iconImg.color = def.iconColor;
                    if (iconLetter != null) iconLetter.text = def.iconLetter;
                }

                var priceT = row.Find("Price")?.GetComponent<Text>();
                if (priceT != null) priceT.text = $"{entry.price:N0} G";

                var buyBtn = row.Find("BuyBtn")?.GetComponent<Button>();
                if (buyBtn != null)
                {
                    bool canAfford = PlayerStats.Instance != null && PlayerStats.Instance.Gold >= entry.price;
                    buyBtn.interactable = canAfford;
                }

                var iconRef = row.Find("Icon")?.GetComponent<ItemSlotRef>();
                if (iconRef != null) iconRef.itemId = entry.itemId;
            }
        }

        private void OnBuyClicked(int idx)
        {
            if (idx < 0 || idx >= defaultCatalog.Count) return;
            var entry = defaultCatalog[idx];
            var stats = PlayerStats.Instance;
            var inv = InventorySystem.Instance;
            if (stats == null || inv == null) return;

            if (stats.Gold < entry.price)
            {
                ToastUI.Instance?.Show("골드가 부족합니다.", new Color(0.95f, 0.30f, 0.30f));
                return;
            }
            // Find a free inventory slot before charging
            if (!inv.HasFreeSlotFor(entry.itemId))
            {
                ToastUI.Instance?.Show("인벤토리가 가득 찼습니다.", new Color(0.95f, 0.55f, 0.30f));
                return;
            }
            stats.SpendGold(entry.price);
            inv.Add(entry.itemId, 1);
            var def = ItemDatabase.Get(entry.itemId);
            string name = def != null ? def.displayName : entry.itemId;
            Color tint = def != null ? ItemDatabase.RarityColor(def.rarity) : Color.white;
            ToastUI.Instance?.Show($"[구매]  {name}  -{entry.price:N0} G", tint);
            RefreshGold();
            RefreshRows();
        }

        // === Sell tab ===

        private void CacheSellSlots()
        {
            if (sellGridRoot == null) return;
            int count = InventorySystem.SLOT_COUNT;
            _sellIcons = new Image[count];
            _sellLetters = new Text[count];
            _sellQtys = new Text[count];
            _sellPrices = new Text[count];
            _sellRefs = new ItemSlotRef[count];
            for (int i = 0; i < count; i++)
            {
                var slot = sellGridRoot.Find($"SellSlot_{i}");
                if (slot == null) continue;
                _sellIcons[i] = slot.Find("Icon")?.GetComponent<Image>();
                _sellLetters[i] = slot.Find("Icon/Letter")?.GetComponent<Text>();
                _sellQtys[i] = slot.Find("Qty")?.GetComponent<Text>();
                _sellPrices[i] = slot.Find("Price")?.GetComponent<Text>();
                _sellRefs[i] = slot.GetComponent<ItemSlotRef>();
                var btn = slot.GetComponent<Button>();
                if (btn == null) btn = slot.gameObject.AddComponent<Button>();
                int idx = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSellClicked(idx));
            }
        }

        private void RefreshSellGrid()
        {
            if (!_sellMode || sellGridRoot == null) return;
            var inv = InventorySystem.Instance;
            if (inv == null || _sellIcons == null) return;
            for (int i = 0; i < InventorySystem.SLOT_COUNT; i++)
            {
                if (_sellIcons[i] == null) continue;
                var s = inv.Slots[i];
                if (_sellRefs[i] != null) _sellRefs[i].itemId = s.IsEmpty ? "" : s.itemId;
                if (s.IsEmpty)
                {
                    _sellIcons[i].gameObject.SetActive(false);
                    if (_sellQtys[i] != null) _sellQtys[i].text = "";
                    if (_sellPrices[i] != null) _sellPrices[i].text = "";
                }
                else
                {
                    var def = ItemDatabase.Get(s.itemId);
                    _sellIcons[i].gameObject.SetActive(true);
                    if (def != null)
                    {
                        _sellIcons[i].color = def.iconColor;
                        if (_sellLetters[i] != null) _sellLetters[i].text = def.iconLetter;
                        if (_sellPrices[i] != null) _sellPrices[i].text = $"{def.sellPrice}G";
                    }
                    if (_sellQtys[i] != null) _sellQtys[i].text = s.qty > 1 ? s.qty.ToString() : "";
                }
            }
        }

        private void OnSellClicked(int slotIdx)
        {
            var inv = InventorySystem.Instance;
            var stats = PlayerStats.Instance;
            if (inv == null || stats == null) return;
            if (slotIdx < 0 || slotIdx >= InventorySystem.SLOT_COUNT) return;
            var s = inv.Slots[slotIdx];
            if (s.IsEmpty) return;
            var def = ItemDatabase.Get(s.itemId);
            if (def != null && def.untradable)
            {
                ToastUI.Instance?.Show("교환·판매 불가 아이템입니다.", new Color(0.95f, 0.55f, 0.30f));
                return;
            }
            int unitPrice = def != null ? def.sellPrice : 1;
            string name = def != null ? def.displayName : s.itemId;
            Color tint = def != null ? ItemDatabase.RarityColor(def.rarity) : Color.white;

            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (shift)
            {
                // Sell every unit of this item across all slots
                int totalQty = inv.RemoveAllOfItem(s.itemId);
                if (totalQty <= 0) return;
                int totalGold = unitPrice * totalQty;
                stats.AddGold(totalGold);
                ToastUI.Instance?.Show($"[판매]  {name} × {totalQty}  +{totalGold:N0} G", tint);
            }
            else
            {
                if (!inv.RemoveOneFromSlot(slotIdx)) return;
                stats.AddGold(unitPrice);
                ToastUI.Instance?.Show($"[판매]  {name}  +{unitPrice:N0} G", tint);
            }
            RefreshGold();
        }
    }
}
