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

        private bool OpenBox(int slotIndex, ItemDatabase.ItemDef boxDef)
        {
            string rewardId = ResolveBoxReward(boxDef.id);
            if (string.IsNullOrEmpty(rewardId)) return false;
            // Make sure the reward will fit before consuming the box
            if (!HasFreeSlotFor(rewardId))
            {
                Astrion.UI.ToastUI.Instance?.Show("인벤토리가 가득 찼습니다.",
                    new Color(0.95f, 0.55f, 0.30f));
                return false;
            }
            // Decrement the box first, then Add the reward (so a stackable reward
            // can land in the just-freed slot if needed)
            var s = Slots[slotIndex];
            Slots[slotIndex] = new Slot { itemId = s.itemId, qty = s.qty - 1 };
            if (Slots[slotIndex].qty <= 0) Slots[slotIndex] = new Slot();
            Add(rewardId, 1);
            var rdef = ItemDatabase.Get(rewardId);
            string name = rdef != null ? rdef.displayName : rewardId;
            Color tint = rdef != null ? ItemDatabase.RarityColor(rdef.rarity) : Color.white;
            Astrion.UI.ToastUI.Instance?.Show($"[상자]  {name} 획득!", tint);
            SaveToState();
            OnChanged?.Invoke();
            return true;
        }

        private static string ResolveBoxReward(string boxId)
        {
            string cls = UnityEngine.PlayerPrefs.GetString("characterClass", "");
            switch (boxId)
            {
                case "weapon_box":
                    switch (cls)
                    {
                        case "Archer": return "star_bow";
                        default:       return "bronze_dagger";
                    }
                case "helmet_box": return "leather_helmet";
                case "armor_box":  return "chain_armor";
                case "ring_box":   return "stardust_ring";
            }
            return "";
        }

        /// <summary>True if at least one of itemId×1 can be added (existing stack room or any empty slot).</summary>
        public bool HasFreeSlotFor(string itemId)
        {
            var def = ItemDatabase.Get(itemId);
            if (def == null) return false;
            if (def.maxStack > 1)
            {
                for (int i = 0; i < SLOT_COUNT; i++)
                    if (Slots[i].itemId == itemId && Slots[i].qty < def.maxStack) return true;
            }
            for (int i = 0; i < SLOT_COUNT; i++)
                if (Slots[i].IsEmpty) return true;
            return false;
        }

        /// <summary>Remove one unit from a slot. Returns true if something was removed.</summary>
        public bool RemoveOneFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SLOT_COUNT) return false;
            var s = Slots[slotIndex];
            if (s.IsEmpty) return false;
            Slots[slotIndex] = new Slot { itemId = s.itemId, qty = s.qty - 1 };
            if (Slots[slotIndex].qty <= 0) Slots[slotIndex] = new Slot();
            SaveToState();
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Drag-drop handler. Same item id with stack room → merge into target;
        /// otherwise just swap.
        /// </summary>
        public void SwapSlots(int a, int b)
        {
            if (a < 0 || a >= SLOT_COUNT || b < 0 || b >= SLOT_COUNT) return;
            if (a == b) return;
            var sa = Slots[a];
            var sb = Slots[b];

            if (!sa.IsEmpty && !sb.IsEmpty && sa.itemId == sb.itemId)
            {
                var def = ItemDatabase.Get(sa.itemId);
                int stackMax = def != null ? def.maxStack : 1;
                if (stackMax > 1)
                {
                    int canMove = Mathf.Min(sa.qty, stackMax - sb.qty);
                    if (canMove > 0)
                    {
                        Slots[b] = new Slot { itemId = sb.itemId, qty = sb.qty + canMove };
                        int left = sa.qty - canMove;
                        Slots[a] = left > 0
                            ? new Slot { itemId = sa.itemId, qty = left }
                            : new Slot();
                        SaveToState();
                        OnChanged?.Invoke();
                        return;
                    }
                }
            }

            Slots[a] = sb;
            Slots[b] = sa;
            SaveToState();
            OnChanged?.Invoke();
        }

        /// <summary>Compact inventory: merge stacks of the same item, then push everything to the front.</summary>
        public void Compact()
        {
            var totals = new Dictionary<string, int>();
            var order = new List<string>(); // preserve first-seen order
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                var s = Slots[i];
                if (s.IsEmpty) continue;
                if (totals.ContainsKey(s.itemId)) totals[s.itemId] += s.qty;
                else { totals[s.itemId] = s.qty; order.Add(s.itemId); }
            }
            for (int i = 0; i < SLOT_COUNT; i++) Slots[i] = new Slot();
            int idx = 0;
            foreach (var id in order)
            {
                var def = ItemDatabase.Get(id);
                int stackMax = def != null ? def.maxStack : 1;
                int left = totals[id];
                while (left > 0 && idx < SLOT_COUNT)
                {
                    int amt = Mathf.Min(left, stackMax);
                    Slots[idx++] = new Slot { itemId = id, qty = amt };
                    left -= amt;
                }
            }
            SaveToState();
            OnChanged?.Invoke();
        }

        /// <summary>Use the first consumable that restores HP (forHp=true) or MP (forHp=false).</summary>
        public bool UseFirstConsumable(bool forHp)
        {
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                var s = Slots[i];
                if (s.IsEmpty) continue;
                var def = ItemDatabase.Get(s.itemId);
                if (def == null || def.itemType != "소비") continue;
                bool match = forHp ? def.healAmount > 0 : def.manaAmount > 0;
                if (!match) continue;
                if (UseSlot(i)) return true;
            }
            return false;
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
                case "상자":
                    return OpenBox(slotIndex, def);
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
                    int gainedHp = 0, gainedMp = 0;
                    if (def.healAmount > 0 && stats.Hp < stats.MaxHp)
                    {
                        gainedHp = Mathf.Min(def.healAmount, stats.MaxHp - stats.Hp);
                        stats.Heal(def.healAmount); used = true;
                    }
                    if (def.manaAmount > 0 && stats.Mp < stats.MaxMp)
                    {
                        gainedMp = Mathf.Min(def.manaAmount, stats.MaxMp - stats.Mp);
                        stats.RestoreMp(def.manaAmount); used = true;
                    }
                    if (used)
                    {
                        // Decrement
                        Slots[slotIndex] = new Slot { itemId = s.itemId, qty = s.qty - 1 };
                        if (Slots[slotIndex].qty <= 0) Slots[slotIndex] = new Slot();
                        SaveToState();
                        OnChanged?.Invoke();

                        string gainStr = "";
                        if (gainedHp > 0 && gainedMp > 0) gainStr = $"HP +{gainedHp}  MP +{gainedMp}";
                        else if (gainedHp > 0) gainStr = $"HP +{gainedHp}";
                        else if (gainedMp > 0) gainStr = $"MP +{gainedMp}";
                        Astrion.UI.ToastUI.Instance?.Show($"[{def.displayName}]  {gainStr}", def.iconColor);
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
