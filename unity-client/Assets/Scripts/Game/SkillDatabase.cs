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
            public string ownerClass;   // "Warrior" / "Mage" / "Archer" / "Thief" / "" (any)
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
                    ownerClass = "Warrior",
                },
                ["warrior_dash"] = new SkillDef
                {
                    id = "warrior_dash",
                    displayName = "돌진",
                    description = "전방으로 빠르게 돌진.\n레벨당 거리 +1u\nWarrior 전용",
                    iconColor = new Color(0.95f, 0.45f, 0.25f),
                    iconLetter = "→",
                    maxLevel = 3, unlockLevel = 3,
                    isActive = true, mpCost = 5, cooldown = 2f,
                    isStarter = false,
                    ownerClass = "Warrior",
                },
                ["teleport"] = new SkillDef
                {
                    id = "teleport",
                    displayName = "텔레포트",
                    description = "전방으로 순간이동.\n레벨당 거리 +1u\nMage 전용",
                    iconColor = new Color(0.55f, 0.40f, 0.95f),
                    iconLetter = "✦",
                    maxLevel = 3, unlockLevel = 3,
                    isActive = true, mpCost = 15, cooldown = 3f,
                    isStarter = false,
                    ownerClass = "Mage",
                },
                ["double_jump"] = new SkillDef
                {
                    id = "double_jump",
                    displayName = "더블 점프",
                    description = "공중에서 한 번 더 점프.\n레벨당 추가 점프 +1회\nArcher 전용",
                    iconColor = new Color(0.40f, 0.85f, 0.60f),
                    iconLetter = "↑↑",
                    maxLevel = 3, unlockLevel = 3,
                    isActive = true, mpCost = 0, cooldown = 0.3f,
                    isStarter = false,
                    ownerClass = "Archer",
                },
                // ── Basic attack (every class, MapleStory-style 'Ctrl key' melee) ──
                // No MP cost, short cooldown, modest damage. Functions as a
                // spam-friendly fallback so the player isn't standing around
                // waiting on Q's longer cooldown.
                ["basic_attack"] = new SkillDef
                {
                    id = "basic_attack",
                    displayName = "기본 공격",
                    description = "가까이 있는 적에게 약한 일격.\nMP 없음, 짧은 쿨다운.",
                    iconColor = new Color(0.75f, 0.75f, 0.75f),
                    iconLetter = "·",
                    maxLevel = 1, unlockLevel = 1,
                    isActive = true, mpCost = 0, cooldown = 0.35f,
                    isStarter = true,
                    ownerClass = "",  // any class
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
