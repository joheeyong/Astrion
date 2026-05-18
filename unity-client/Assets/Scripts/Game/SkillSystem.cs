using System;
using System.Collections.Generic;
using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    public class SkillSystem : MonoBehaviour
    {
        public static SkillSystem Instance { get; private set; }

        public event Action OnChanged;

        // skillId → current level (0 = not learned)
        private Dictionary<string, int> _levels = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SkillDatabase.EnsureInit();
        }

        private void Start()
        {
            var psm = PlayerStateManager.Instance;
            if (psm != null)
            {
                if (psm.IsLoaded) RestoreFromState();
                else psm.OnLoaded += RestoreFromState;
            }
        }

        private void OnDestroy()
        {
            var psm = PlayerStateManager.Instance;
            if (psm != null) psm.OnLoaded -= RestoreFromState;
            if (Instance == this) Instance = null;
        }

        private void RestoreFromState()
        {
            var s = PlayerStateManager.Instance?.State;
            _levels.Clear();
            if (s != null && s.learnedSkillIds != null)
            {
                int n = Mathf.Min(s.learnedSkillIds.Length, s.learnedSkillLevels?.Length ?? 0);
                for (int i = 0; i < n; i++)
                    _levels[s.learnedSkillIds[i]] = s.learnedSkillLevels[i];
            }
            // Auto-grant starter skills (e.g., starbolt) if not already learned
            foreach (var def in SkillDatabase.All())
            {
                if (def.isStarter && GetLevel(def.id) == 0)
                {
                    _levels[def.id] = 1;
                }
            }
            SaveToState();
            OnChanged?.Invoke();
        }

        public int GetLevel(string skillId)
        {
            return _levels.TryGetValue(skillId, out var lv) ? lv : 0;
        }

        public bool IsLearned(string skillId) => GetLevel(skillId) > 0;

        public bool LearnOrLevelUp(string skillId)
        {
            var def = SkillDatabase.Get(skillId);
            if (def == null) return false;
            var stats = PlayerStats.Instance;
            if (stats == null) return false;
            int current = GetLevel(skillId);
            if (current >= def.maxLevel) return false;
            if (stats.Level < def.unlockLevel) return false;
            if (stats.SkillPoints < def.spCostPerLevel) return false;

            stats.SpendSkillPoints(def.spCostPerLevel);
            _levels[skillId] = current + 1;
            SaveToState();
            OnChanged?.Invoke();
            // skill-up visible via toast + skill window
            Astrion.UI.ToastUI.Instance?.Show(
                $"[스킬]  {def.displayName}  Lv.{current}→Lv.{current + 1}",
                def.iconColor);
            return true;
        }

        private void SaveToState()
        {
            var psm = PlayerStateManager.Instance;
            if (psm == null) return;
            var ids = new List<string>();
            var lvs = new List<int>();
            foreach (var kv in _levels)
            {
                if (kv.Value <= 0) continue;
                ids.Add(kv.Key);
                lvs.Add(kv.Value);
            }
            psm.State.learnedSkillIds = ids.ToArray();
            psm.State.learnedSkillLevels = lvs.ToArray();
            psm.Save();
        }
    }
}
