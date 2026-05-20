using System;
using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        public int Hp { get; private set; } = 100;
        public int MaxHp { get; private set; } = 100;
        public int Mp { get; private set; } = 50;
        public int MaxMp { get; private set; } = 50;

        public int Level { get; private set; } = 1;
        public int Exp { get; private set; } = 0;
        public int Str { get; private set; } = 5;
        public int Dex { get; private set; } = 5;
        public int Intel { get; private set; } = 5;
        public int Luk { get; private set; } = 5;
        public int StatPoints { get; private set; } = 5;
        public int SkillPoints { get; private set; } = 0;
        public int Gold { get; private set; } = 0;
        public string EquippedWeaponId { get; private set; } = "";
        public string EquippedHelmetId { get; private set; } = "";
        public string EquippedArmorId  { get; private set; } = "";
        public string EquippedRingId   { get; private set; } = "";

        public event Action OnChanged;
        public event Action OnDied;
        public event Action OnLeveledUp;

        public bool IsDead { get; private set; }

        [SerializeField] private float regenTickSeconds = 2f;
        [SerializeField] private int regenHpPerTick = 1;
        [SerializeField] private int regenMpPerTick = 1;
        [SerializeField] private float saveDebounceSeconds = 3f;

        private float _regenTimer;
        private float _saveTimer;
        private bool _dirty;
        private int _lastSentHp = -1;
        private int _lastSentMaxHp = -1;
        private float _lastStatusSentAt;

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
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived += HandlePacket;
        }

        private void OnDestroy()
        {
            var psm = PlayerStateManager.Instance;
            if (psm != null) psm.OnLoaded -= RestoreFromState;
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived -= HandlePacket;
            if (Instance == this) Instance = null;
        }

        private void HandlePacket(GamePacket packet)
        {
            if (packet.Type != PacketType.ExpGained) return;
            try
            {
                var d = JsonUtility.FromJson<ExpPayload>(packet.Payload);
                if (d != null)
                {
                    if (d.exp > 0) AddExp(d.exp);
                    if (d.gold > 0) AddGold(d.gold);
                }
            }
            catch (System.Exception e) { Debug.LogWarning($"[PlayerStats] EXP parse error: {e.Message}"); }
        }

        [System.Serializable] private class ExpPayload { public int exp; public int gold; }

        public int ExpForNextLevel(int level) => 50 * Mathf.Max(1, level);

        public void AddExp(int amount)
        {
            if (amount <= 0) return;
            int before = Exp;
            int prevLevel = Level;
            Exp += amount;
            while (Exp >= ExpForNextLevel(Level))
            {
                Exp -= ExpForNextLevel(Level);
                LevelUp();
            }
            _dirty = true;
            OnChanged?.Invoke();
            // Persist immediately on EVERY exp gain (not just level-up):
            // SaveAttributes covers Exp/Level/Stats, FlushSave covers HP/MP changes from level-up.
            SaveAttributes();
            FlushSave();
            // exp/level changes are visible via HUD bar + toast; no console spam

            if (Level != prevLevel)
            {
                Astrion.UI.ToastUI.Instance?.Show(
                    $"LEVEL UP!  Lv.{Level}   +5 STAT  +1 SKILL",
                    new Color(1f, 0.85f, 0.30f));
                // Important event — request a server ACK
                PlayerStateManager.Instance?.SaveImportant("레벨업");
            }
        }

        private void LevelUp()
        {
            Level++;
            StatPoints += 5;
            SkillPoints += 1; // +1 skill point per level
            MaxHp += 10;
            MaxMp += 5;
            Hp = MaxHp;   // full restore on level up
            Mp = MaxMp;
            OnLeveledUp?.Invoke();
        }

        private void RestoreFromState()
        {
            var s = PlayerStateManager.Instance?.State;
            if (s == null) return;
            // Fallback to defaults for older saves
            if (s.maxHp > 0) MaxHp = s.maxHp;
            if (s.maxMp > 0) MaxMp = s.maxMp;
            Hp = s.hp > 0 ? Mathf.Clamp(s.hp, 0, MaxHp) : MaxHp;
            Mp = s.mp > 0 ? Mathf.Clamp(s.mp, 0, MaxMp) : MaxMp;
            if (s.level > 0) Level = s.level;
            Exp = Mathf.Max(0, s.exp);
            if (s.statStr > 0) Str = s.statStr;
            if (s.statDex > 0) Dex = s.statDex;
            if (s.statInt > 0) Intel = s.statInt;
            if (s.statLuk > 0) Luk = s.statLuk;
            StatPoints = Mathf.Max(0, s.statPoints);
            SkillPoints = Mathf.Max(0, s.skillPoints);
            EquippedWeaponId = s.equippedWeaponId ?? "";
            EquippedHelmetId = s.equippedHelmetId ?? "";
            EquippedArmorId  = s.equippedArmorId  ?? "";
            EquippedRingId   = s.equippedRingId   ?? "";
            Gold = Mathf.Max(0, s.gold);
            OnChanged?.Invoke();
        }

        public bool SpendSkillPoints(int cost)
        {
            if (cost <= 0) return true;
            if (SkillPoints < cost) return false;
            SkillPoints -= cost;
            SaveAttributes();
            OnChanged?.Invoke();
            return true;
        }

        public bool SpendStatPoint(string stat)
        {
            if (StatPoints <= 0) return false;
            switch (stat)
            {
                case "STR": Str++; break;
                case "DEX": Dex++; break;
                case "INT": Intel++; break;
                case "LUK": Luk++; break;
                default: return false;
            }
            StatPoints--;
            SaveAttributes();
            OnChanged?.Invoke();
            return true;
        }

        public void EquipWeapon(string itemId)
        {
            EquippedWeaponId = itemId ?? "";
            SaveAttributes();
            OnChanged?.Invoke();
        }

        public void EquipItem(string itemId, string slot)
        {
            if (string.IsNullOrEmpty(slot)) return;
            switch (slot)
            {
                case "weapon": EquippedWeaponId = itemId ?? ""; break;
                case "helmet": EquippedHelmetId = itemId ?? ""; break;
                case "armor":  EquippedArmorId  = itemId ?? ""; break;
                case "ring":   EquippedRingId   = itemId ?? ""; break;
                default: return;
            }
            SaveAttributes();
            OnChanged?.Invoke();
        }

        public int ComputeBoltDamage() { return ComputeBoltDamage(out _); }

        public int ComputeBoltDamage(out bool isCritical)
        {
            int weaponDmg = 0;
            if (!string.IsNullOrEmpty(EquippedWeaponId))
            {
                var def = ItemDatabase.Get(EquippedWeaponId);
                if (def != null) weaponDmg = def.baseDamage;
            }
            int skillLv = SkillSystem.Instance != null ? SkillSystem.Instance.GetLevel("starbolt") : 1;
            if (skillLv < 1) skillLv = 1;
            float skillBonus = (skillLv - 1) * 5f;
            float baseD = 5f + Intel * 2f + Level * 3f + weaponDmg + skillBonus;
            float variance = baseD * 0.2f;
            int dmg = Mathf.Max(1, Mathf.RoundToInt(baseD + UnityEngine.Random.Range(-variance, variance)));
            isCritical = RollCritical();
            if (isCritical) dmg = Mathf.RoundToInt(dmg * 1.7f);
            return dmg;
        }

        /// LUK-based critical: 2.5%% at LUK 5, 5%% at LUK 10, 10%% at LUK 20.
        public bool RollCritical()
        {
            float chance = Luk * 0.005f;
            return UnityEngine.Random.value < chance;
        }

        private void SaveAttributes()
        {
            var psm = PlayerStateManager.Instance;
            if (psm == null) return;
            psm.UpdateAttributes(Level, Exp, Str, Dex, Intel, Luk, StatPoints, EquippedWeaponId);
            psm.UpdateGold(Gold);
            psm.UpdateEquipment(EquippedWeaponId, EquippedHelmetId, EquippedArmorId, EquippedRingId);
        }

        private void Update()
        {
            // Passive regen (HP + MP)
            bool needsRegen = Hp < MaxHp || Mp < MaxMp;
            if (needsRegen)
            {
                _regenTimer += Time.deltaTime;
                if (_regenTimer >= regenTickSeconds)
                {
                    _regenTimer = 0f;
                    int newHp = Mathf.Min(MaxHp, Hp + regenHpPerTick);
                    int newMp = Mathf.Min(MaxMp, Mp + regenMpPerTick);
                    if (newHp != Hp || newMp != Mp)
                    {
                        Hp = newHp; Mp = newMp;
                        _dirty = true;
                        OnChanged?.Invoke();
                    }
                }
            }
            else _regenTimer = 0f;

            // Debounced save (avoids spamming every regen tick)
            if (_dirty)
            {
                _saveTimer += Time.deltaTime;
                if (_saveTimer >= saveDebounceSeconds) FlushSave();
            }

            // Network HP/MaxHP push (throttled to ~3 Hz). Combat stats ride along
            // so the server can cap claimed damage against them (anti-cheat).
            if ((Hp != _lastSentHp || MaxHp != _lastSentMaxHp) && Time.time - _lastStatusSentAt >= 0.3f)
            {
                var nm = NetworkManager.Instance;
                if (nm != null && nm.IsConnected)
                {
                    int wpn = 0;
                    if (!string.IsNullOrEmpty(EquippedWeaponId))
                    {
                        var def = ItemDatabase.Get(EquippedWeaponId);
                        if (def != null) wpn = def.baseDamage;
                    }
                    int starLv = SkillSystem.Instance != null
                        ? Mathf.Max(1, SkillSystem.Instance.GetLevel("starbolt"))
                        : 1;
                    string json =
                        "{\"hp\":" + Hp +
                        ",\"maxHp\":" + MaxHp +
                        ",\"level\":" + Level +
                        ",\"intStat\":" + Intel +
                        ",\"weaponDmg\":" + wpn +
                        ",\"starboltLv\":" + starLv +
                        "}";
                    nm.SendPacket(PacketType.StatusUpdate, json);
                    _lastSentHp = Hp;
                    _lastSentMaxHp = MaxHp;
                    _lastStatusSentAt = Time.time;
                }
            }
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || IsDead) return;
            Hp = Mathf.Max(0, Hp - amount);
            _dirty = true;
            OnChanged?.Invoke();
            FlushSave(); // Important event — save immediately
            if (Hp <= 0)
            {
                IsDead = true;
                OnDied?.Invoke();
            }
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            SaveAttributes();
            OnChanged?.Invoke();
            Astrion.UI.ToastUI.Instance?.Show($"[+ {amount} 골드]", new Color(0.94f, 0.78f, 0.30f));
            // gold change visible via CharPanel HUD + toast
        }

        public bool SpendGold(int amount)
        {
            if (amount <= 0) return true;
            if (Gold < amount) return false;
            Gold -= amount;
            SaveAttributes();
            OnChanged?.Invoke();
            return true;
        }

        // Called by DeathSystem after the respawn delay finishes.
        public void RespawnRestore()
        {
            IsDead = false;
            Hp = MaxHp;
            Mp = MaxMp;
            _dirty = true;
            OnChanged?.Invoke();
            FlushSave();
        }

        public void Heal(int amount)
        {
            if (amount <= 0) return;
            Hp = Mathf.Min(MaxHp, Hp + amount);
            _dirty = true;
            OnChanged?.Invoke();
            FlushSave();
        }

        public bool ConsumeMp(int amount)
        {
            if (amount <= 0) return true;
            if (Mp < amount) return false;
            Mp -= amount;
            _dirty = true;
            OnChanged?.Invoke();
            return true; // Save will follow via debounce
        }

        public void RestoreMp(int amount)
        {
            if (amount <= 0) return;
            Mp = Mathf.Min(MaxMp, Mp + amount);
            _dirty = true;
            OnChanged?.Invoke();
            FlushSave();
        }

        private void OnApplicationQuit() { FlushSave(); }
        private void OnApplicationPause(bool pause) { if (pause) FlushSave(); }

        private void FlushSave()
        {
            if (!_dirty) return;
            var psm = PlayerStateManager.Instance;
            if (psm != null) psm.UpdateStats(Hp, MaxHp, Mp, MaxMp);
            _dirty = false;
            _saveTimer = 0f;
        }
    }
}
