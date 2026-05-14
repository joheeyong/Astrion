using System;
using System.Collections.Generic;
using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    public class InventorySystem : MonoBehaviour
    {
        public const int SLOT_COUNT = 24;

        public static InventorySystem Instance { get; private set; }

        public struct Slot
        {
            public string itemId;
            public int qty;
            public bool IsEmpty => string.IsNullOrEmpty(itemId) || qty <= 0;
        }

        public Slot[] Slots { get; private set; } = new Slot[SLOT_COUNT];
        public event Action OnChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ItemDatabase.EnsureInit();
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
            for (int i = 0; i < SLOT_COUNT; i++) Slots[i] = new Slot();
            if (s == null || s.inventoryItemIds == null) { OnChanged?.Invoke(); return; }

            int n = Mathf.Min(s.inventoryItemIds.Length, SLOT_COUNT);
            for (int i = 0; i < n; i++)
            {
                Slots[i] = new Slot { itemId = s.inventoryItemIds[i], qty = s.inventoryQuantities[i] };
            }
            OnChanged?.Invoke();
        }

        public bool Add(string itemId, int qty)
        {
            var def = ItemDatabase.Get(itemId);
            if (def == null || qty <= 0) return false;

            int remaining = qty;

            // Merge into existing stacks
            if (def.maxStack > 1)
            {
                for (int i = 0; i < SLOT_COUNT && remaining > 0; i++)
                {
                    if (Slots[i].itemId == itemId && Slots[i].qty < def.maxStack)
                    {
                        int canAdd = Mathf.Min(remaining, def.maxStack - Slots[i].qty);
                        Slots[i] = new Slot { itemId = itemId, qty = Slots[i].qty + canAdd };
                        remaining -= canAdd;
                    }
                }
            }

            // Fill empty slots
            for (int i = 0; i < SLOT_COUNT && remaining > 0; i++)
            {
                if (Slots[i].IsEmpty)
                {
                    int amt = Mathf.Min(remaining, def.maxStack);
                    Slots[i] = new Slot { itemId = itemId, qty = amt };
                    remaining -= amt;
                }
            }

            SaveToState();
            OnChanged?.Invoke();
            return remaining == 0;
        }

        public void Clear(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SLOT_COUNT) return;
            Slots[slotIndex] = new Slot();
            SaveToState();
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Click-to-use: equip weapons, consume potions, etc.
        /// </summary>
        public bool UseSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SLOT_COUNT) return false;
            var s = Slots[slotIndex];
            if (s.IsEmpty) return false;
            var def = ItemDatabase.Get(s.itemId);
            if (def == null) return false;

            switch (def.itemType)
            {
                case "장비":
                    if (def.baseDamage > 0)
                    {
                        PlayerStats.Instance?.EquipWeapon(def.id);
                        OnChanged?.Invoke();
                        return true;
                    }
                    return false;
                case "소비":
                    var stats = PlayerStats.Instance;
                    if (stats == null) return false;
                    bool used = false;
                    if (def.healAmount > 0 && stats.Hp < stats.MaxHp)
                    {
                        stats.Heal(def.healAmount); used = true;
                    }
                    if (def.manaAmount > 0 && stats.Mp < stats.MaxMp)
                    {
                        stats.RestoreMp(def.manaAmount); used = true;
                    }
                    if (used)
                    {
                        // Decrement
                        Slots[slotIndex] = new Slot { itemId = s.itemId, qty = s.qty - 1 };
                        if (Slots[slotIndex].qty <= 0) Slots[slotIndex] = new Slot();
                        SaveToState();
                        OnChanged?.Invoke();
                    }
                    return used;
                default:
                    return false;
            }
        }

        private void SaveToState()
        {
            var psm = PlayerStateManager.Instance;
            if (psm == null) return;

            // Find last non-empty slot for compact storage
            int last = -1;
            for (int i = 0; i < SLOT_COUNT; i++) if (!Slots[i].IsEmpty) last = i;

            int size = last + 1;
            var ids = new string[size];
            var qts = new int[size];
            for (int i = 0; i < size; i++)
            {
                ids[i] = Slots[i].IsEmpty ? "" : Slots[i].itemId;
                qts[i] = Slots[i].IsEmpty ? 0 : Slots[i].qty;
            }

            psm.State.inventoryItemIds = ids;
            psm.State.inventoryQuantities = qts;
            psm.Save();
        }
    }
}
