using System.Collections.Generic;
using UnityEngine;

namespace Astrion.Game
{
    public static class ItemDatabase
    {
        public enum Rarity { Common, Uncommon, Rare, Epic, Legend, Mythic }

        public class ItemDef
        {
            public string id;
            public string displayName;
            public string description;
            public Color iconColor;
            public string iconLetter; // single-char placeholder icon
            public int maxStack;
            public Rarity rarity = Rarity.Common;
            public string itemType = ""; // 장비/소비/기타/설치/캐쉬
            public int baseDamage = 0; // for weapons
            public int healAmount = 0; // for potions (HP)
            public int manaAmount = 0; // for potions (MP)
        }

        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return new Color(0.35f, 0.78f, 0.31f);
                case Rarity.Rare:     return new Color(0.31f, 0.60f, 0.91f);
                case Rarity.Epic:     return new Color(0.78f, 0.38f, 0.91f);
                case Rarity.Legend:   return new Color(0.94f, 0.66f, 0.19f);
                case Rarity.Mythic:   return new Color(0.91f, 0.28f, 0.34f);
                default:              return new Color(0.66f, 0.66f, 0.66f); // common
            }
        }

        public static string RarityLabel(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return "UNCOMMON";
                case Rarity.Rare:     return "RARE";
                case Rarity.Epic:     return "EPIC";
                case Rarity.Legend:   return "LEGENDARY";
                case Rarity.Mythic:   return "MYTHIC";
                default:              return "COMMON";
            }
        }

        private static Dictionary<string, ItemDef> _items;
        private static bool _initialized;

        public static void EnsureInit()
        {
            if (_initialized) return;
            _items = new Dictionary<string, ItemDef>
            {
                ["bread"] = new ItemDef
                {
                    id = "bread", displayName = "빵",
                    description = "오래된 마을 빵. HP +20.",
                    iconColor = new Color(0.78f, 0.55f, 0.28f),
                    iconLetter = "빵", maxStack = 99,
                    rarity = Rarity.Common, itemType = "소비",
                    healAmount = 20,
                },
                ["elixir"] = new ItemDef
                {
                    id = "elixir", displayName = "달의 영약",
                    description = "은빛 액체. MP +15.",
                    iconColor = new Color(0.30f, 0.55f, 0.92f),
                    iconLetter = "약", maxStack = 99,
                    rarity = Rarity.Uncommon, itemType = "소비",
                    manaAmount = 15,
                },
                ["stardust"] = new ItemDef
                {
                    id = "stardust", displayName = "별 가루",
                    description = "추락한 별의 잔재. 무언가의 재료.",
                    iconColor = new Color(0.95f, 0.78f, 0.30f),
                    iconLetter = "★", maxStack = 999,
                    rarity = Rarity.Rare, itemType = "기타",
                },
                ["dagger"] = new ItemDef
                {
                    id = "dagger", displayName = "나무 단검",
                    description = "초보 모험가의 단검. 공격력 +5.",
                    iconColor = new Color(0.55f, 0.45f, 0.35f),
                    iconLetter = "검", maxStack = 1,
                    rarity = Rarity.Epic, itemType = "장비",
                    baseDamage = 5,
                },
            };
            _initialized = true;
        }

        public static ItemDef Get(string id)
        {
            EnsureInit();
            if (string.IsNullOrEmpty(id)) return null;
            return _items.TryGetValue(id, out var def) ? def : null;
        }
    }
}
