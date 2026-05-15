using System;
using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    public class HotbarSystem : MonoBehaviour
    {
        public const int SLOT_COUNT = 5;

        public static HotbarSystem Instance { get; private set; }

        public event Action OnChanged;

        private readonly string[] _slots = new string[SLOT_COUNT];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
            for (int i = 0; i < SLOT_COUNT; i++) _slots[i] = "";

            if (s != null && s.hotbarSkillIds != null)
            {
                int n = Mathf.Min(s.hotbarSkillIds.Length, SLOT_COUNT);
                for (int i = 0; i < n; i++)
                    _slots[i] = s.hotbarSkillIds[i] ?? "";
            }

            // Default: slot 0 = starbolt if learned and nothing set
            bool empty = true;
            for (int i = 0; i < SLOT_COUNT; i++) if (!string.IsNullOrEmpty(_slots[i])) { empty = false; break; }
            if (empty && SkillSystem.Instance != null && SkillSystem.Instance.IsLearned("starbolt"))
            {
                _slots[0] = "starbolt";
            }

            SaveToState();
            OnChanged?.Invoke();
        }

        public string GetSkillIdAt(int slot)
        {
            if (slot < 0 || slot >= SLOT_COUNT) return "";
            return _slots[slot] ?? "";
        }

        public int GetSlotOf(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return -1;
            for (int i = 0; i < SLOT_COUNT; i++)
                if (_slots[i] == skillId) return i;
            return -1;
        }

        // Bind a skill to a slot. If it was in another slot, remove from there.
        public void Bind(int slot, string skillId)
        {
            if (slot < 0 || slot >= SLOT_COUNT) return;
            if (string.IsNullOrEmpty(skillId)) { Unbind(slot); return; }

            // Remove from any other slot first
            for (int i = 0; i < SLOT_COUNT; i++)
                if (i != slot && _slots[i] == skillId) _slots[i] = "";

            _slots[slot] = skillId;
            SaveToState();
            OnChanged?.Invoke();
        }

        public void Unbind(int slot)
        {
            if (slot < 0 || slot >= SLOT_COUNT) return;
            if (string.IsNullOrEmpty(_slots[slot])) return;
            _slots[slot] = "";
            SaveToState();
            OnChanged?.Invoke();
        }

        public bool TryTrigger(int slot)
        {
            if (slot < 0 || slot >= SLOT_COUNT) return false;
            string id = _slots[slot];
            if (string.IsNullOrEmpty(id)) return false;
            return SkillCaster.Instance != null && SkillCaster.Instance.Cast(id);
        }

        private void SaveToState()
        {
            var psm = PlayerStateManager.Instance;
            if (psm == null) return;
            var arr = new string[SLOT_COUNT];
            for (int i = 0; i < SLOT_COUNT; i++) arr[i] = _slots[i] ?? "";
            psm.State.hotbarSkillIds = arr;
            psm.Save();
        }
    }
}
