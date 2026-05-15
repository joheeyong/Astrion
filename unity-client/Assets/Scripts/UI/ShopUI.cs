using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

        // Default catalogue — set in Inspector or via SetCatalog
        [SerializeField] private List<ShopEntry> defaultCatalog = new List<ShopEntry>();

        public bool IsOpen => panel != null && panel.activeSelf;

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
            RefreshGold();
            RefreshRows();
        }

        public void Close()
        {
            if (panel) panel.SetActive(false);
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
            }
        }

        private void OnBuyClicked(int idx)
        {
            if (idx < 0 || idx >= defaultCatalog.Count) return;
            var entry = defaultCatalog[idx];
            var stats = PlayerStats.Instance;
            var inv = InventorySystem.Instance;
            if (stats == null || inv == null) return;

            if (!stats.SpendGold(entry.price))
            {
                ToastUI.Instance?.Show("골드가 부족합니다.", new Color(0.95f, 0.30f, 0.30f));
                return;
            }
            inv.Add(entry.itemId, 1);
            var def = ItemDatabase.Get(entry.itemId);
            string name = def != null ? def.displayName : entry.itemId;
            Color tint = def != null ? ItemDatabase.RarityColor(def.rarity) : Color.white;
            ToastUI.Instance?.Show($"[구매]  {name}  -{entry.price:N0} G", tint);
            RefreshGold();
            RefreshRows();
        }
    }
}
