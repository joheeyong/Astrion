using System.Collections.Generic;
using UnityEngine;

namespace Astrion.Game
{
    public static class SkillDatabase
    {
        public class SkillDef
        {
            public string id;
            public string displayName;
            public string description;
            public Color iconColor;
            public string iconLetter; // placeholder
            public int maxLevel;
            public int spCostPerLevel = 1;
            public int unlockLevel;     // player level required to learn first time
            public bool isActive;       // active vs passive
            public int mpCost;          // for active skills
            public float cooldown;      // seconds
            public bool isStarter;      // auto-granted at Lv 1
        }

        private static Dictionary<string, SkillDef> _skills;
        private static bool _initialized;

        public static void EnsureInit()
        {
            if (_initialized) return;
            _skills = new Dictionary<string, SkillDef>
            {
                ["starbolt"] = new SkillDef
                {
                    id = "starbolt",
                    displayName = "별빛 투사체",
                    description = "정면으로 별빛을 발사. 가까운 적을 자동 추적.\n레벨당 데미지 +5",
                    iconColor = new Color(1f, 0.85f, 0.30f),
                    iconLetter = "★",
                    maxLevel = 5, unlockLevel = 1,
                    isActive = true, mpCost = 3, cooldown = 0.45f,
                    isStarter = true,
                },
                ["meteor"] = new SkillDef
                {
                    id = "meteor",
                    displayName = "유성 낙하",
                    description = "하늘에서 별이 떨어져 범위 피해. (구현 예정)",
                    iconColor = new Color(0.95f, 0.45f, 0.20f),
                    iconLetter = "☄",
                    maxLevel = 5, unlockLevel = 3,
                    isActive = true, mpCost = 10, cooldown = 4f,
                    isStarter = false,
                },
                ["stellar_heal"] = new SkillDef
                {
                    id = "stellar_heal",
                    displayName = "별빛 회복",
                    description = "별빛으로 자신의 HP를 회복.\n레벨당 회복량 +10",
                    iconColor = new Color(0.55f, 1f, 0.55f),
                    iconLetter = "♥",
                    maxLevel = 5, unlockLevel = 5,
                    isActive = true, mpCost = 8, cooldown = 6f,
                    isStarter = false,
                },
                ["sword_slash"] = new SkillDef
                {
                    id = "sword_slash",
                    displayName = "베기",
                    description = "전방을 검으로 베어 근접 적에게 피해.\n레벨당 데미지 +5",
                    iconColor = new Color(0.85f, 0.85f, 0.90f),
                    iconLetter = "⚔",
                    maxLevel = 5, unlockLevel = 1,
                    isActive = true, mpCost = 2, cooldown = 0.40f,
                    isStarter = false,
                },
            };
            _initialized = true;
        }

        public static SkillDef Get(string id)
        {
            EnsureInit();
            if (string.IsNullOrEmpty(id)) return null;
            return _skills.TryGetValue(id, out var def) ? def : null;
        }

        public static IEnumerable<SkillDef> All()
        {
            EnsureInit();
            return _skills.Values;
        }
    }
}
