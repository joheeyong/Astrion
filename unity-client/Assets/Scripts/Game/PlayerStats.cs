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

        // Star Sage imbue tiers (count of upgrades purchased). Each tier
        // grants a fixed bonus; caps scale with character level so the
        // ceiling moves with progression.
        public int ImbueAtkLv  { get; private set; } = 0;  // +1 atk / tier
        public int ImbueHpLv   { get; private set; } = 0;  // +20 maxHp / tier (already folded into MaxHp)
        public int ImbueMpLv   { get; private set; } = 0;  // +10 maxMp / tier (already folded into MaxMp)
        public int ImbueCritLv { get; private set; } = 0;  // +1% crit / tier

        // Per-(itemId) enhancement level via Smith NPC. v1 only tracks
        // weapons; other equipment ids stay 0. ComputeBoltDamage reads
        // this for the equipped weapon.
        private readonly System.Collections.Generic.Dictionary<string, int> _enhance = new();
        public int GetEnhanceLv(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            return _enhance.TryGetValue(itemId, out var v) ? v : 0;
        }
        public const int MAX_ENHANCE_LV = 5;

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
            ImbueAtkLv  = Mathf.Max(0, s.imbueAtkLv);
            ImbueHpLv   = Mathf.Max(0, s.imbueHpLv);
            ImbueMpLv   = Mathf.Max(0, s.imbueMpLv);
            ImbueCritLv = Mathf.Max(0, s.imbueCritLv);
            _enhance.Clear();
            if (s.enhanceItemIds != null && s.enhanceLevels != null)
            {
                int n = Mathf.Min(s.enhanceItemIds.Length, s.enhanceLevels.Length);
                for (int i = 0; i < n; i++)
                {
                    string id = s.enhanceItemIds[i];
                    int lv = Mathf.Clamp(s.enhanceLevels[i], 0, MAX_ENHANCE_LV);
                    if (!string.IsNullOrEmpty(id) && lv > 0) _enhance[id] = lv;
                }
            }
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
            // Weapon enhancement bonus — +3 atk per upgrade tier (max +5
            // means +15 from a full enhance). Stacks with imbue and skill.
            int weaponEnhanceLv = GetEnhanceLv(EquippedWeaponId);
            float enhanceBonus = weaponEnhanceLv * 3f;
            float baseD = 5f + Intel * 2f + Level * 3f + weaponDmg + skillBonus + ImbueAtkLv + enhanceBonus;
            float variance = baseD * 0.2f;
            int dmg = Mathf.Max(1, Mathf.RoundToInt(baseD + UnityEngine.Random.Range(-variance, variance)));
            isCritical = RollCritical();
            if (isCritical) dmg = Mathf.RoundToInt(dmg * 1.7f);
            return dmg;
        }

        /// LUK-based critical: 2.5%% at LUK 5, 5%% at LUK 10, 10%% at LUK 20.
        /// Star Sage imbue stacks 1%% per tier on top.
        public bool RollCritical()
        {
            float chance = Luk * 0.005f + ImbueCritLv * 0.01f;
            return UnityEngine.Random.value < chance;
        }

        // ── Weapon enhancement (Smith NPC) ────────────────────────────────
        /// Stardust cost per tier — flat 30 / 60 / 90 / 120 / 150.
        public static int EnhanceCost(int currentLv) => 30 + currentLv * 30;
        /// Probability of success at the current tier — getting easier to
        /// fail as the +N climbs. Failure consumes stardust but doesn't
        /// drop the existing level (no break risk in v1).
        public static float EnhanceSuccessRate(int currentLv)
        {
            switch (currentLv)
            {
                case 0: return 1.00f;
                case 1: return 0.95f;
                case 2: return 0.80f;
                case 3: return 0.60f;
                case 4: return 0.40f;
                default: return 0f;
            }
        }
        /// Attempt to enhance the currently-equipped weapon. Returns true on
        /// success. Always consumes stardust if the attempt fires (success
        /// or not); refuses without cost when ineligible (no weapon, max
        /// tier, etc.) and surfaces a toast.
        public bool TryEnhanceWeapon()
        {
            if (string.IsNullOrEmpty(EquippedWeaponId))
            {
                Astrion.UI.ToastUI.Instance?.Show("무기를 장착하세요.",
                    new Color(0.95f, 0.55f, 0.30f));
                return false;
            }
            var def = ItemDatabase.Get(EquippedWeaponId);
            if (def == null || def.baseDamage <= 0)
            {
                Astrion.UI.ToastUI.Instance?.Show("강화할 수 있는 무기가 아닙니다.",
                    new Color(0.95f, 0.55f, 0.30f));
                return false;
            }
            int curLv = GetEnhanceLv(EquippedWeaponId);
            if (curLv >= MAX_ENHANCE_LV)
            {
                Astrion.UI.ToastUI.Instance?.Show($"이미 최대 강화 +{MAX_ENHANCE_LV} 입니다.",
                    new Color(0.95f, 0.55f, 0.30f));
                return false;
            }
            int cost = EnhanceCost(curLv);
            var inv = InventorySystem.Instance;
            if (inv == null) return false;
            if (inv.CountOf("stardust") < cost)
            {
                Astrion.UI.ToastUI.Instance?.Show($"별 가루 {cost}개가 필요합니다.",
                    new Color(0.95f, 0.55f, 0.30f));
                return false;
            }
            if (!inv.ConsumeAmount("stardust", cost)) return false;

            float rate = EnhanceSuccessRate(curLv);
            bool ok = UnityEngine.Random.value < rate;
            if (ok)
            {
                _enhance[EquippedWeaponId] = curLv + 1;
                SaveAttributes();
                OnChanged?.Invoke();
                Astrion.UI.ToastUI.Instance?.Show(
                    $"★ 강화 성공  ·  {def.displayName}  +{curLv + 1}",
                    new Color(0.95f, 0.82f, 0.35f));
                return true;
            }
            else
            {
                SaveAttributes();
                Astrion.UI.ToastUI.Instance?.Show(
                    $"강화 실패  ·  {def.displayName}  +{curLv} (유지)",
                    new Color(0.85f, 0.45f, 0.30f));
                return false;
            }
        }

        /// Per-kind cap helpers — used by AstralImbueUI to disable buttons
        /// and gray rows that are already maxed for the current level.
        public int ImbueCap(string kind)
        {
            switch (kind)
            {
                case "atk":  return Mathf.Max(1, Level);
                case "hp":   return Mathf.Max(1, Level / 2);
                case "mp":   return Mathf.Max(1, Level / 2);
                case "crit": return Mathf.Max(1, Level / 3);
                default:     return 0;
            }
        }

        public int ImbueCurrent(string kind)
        {
            switch (kind)
            {
                case "atk":  return ImbueAtkLv;
                case "hp":   return ImbueHpLv;
                case "mp":   return ImbueMpLv;
                case "crit": return ImbueCritLv;
                default:     return 0;
            }
        }

        public int ImbueCost(string kind)
        {
            switch (kind)
            {
                case "atk":  return 20;
                case "hp":   return 15;
                case "mp":   return 15;
                case "crit": return 50;
                default:     return 0;
            }
        }

        /// Consume stardust + bump the matching imbue tier. Caller (NPC UI)
        /// is responsible for showing a refresh after this completes. All
        /// failure paths surface a toast so the player gets feedback no
        /// matter which precondition failed.
        public bool TryAstralImbue(string kind)
        {
            int cost = ImbueCost(kind);
            int cap  = ImbueCap(kind);
            int cur  = ImbueCurrent(kind);
            if (cost <= 0) return false;

            if (cur >= cap)
            {
                Astrion.UI.ToastUI.Instance?.Show(
                    "이미 최대치 — 레벨업이 필요합니다.",
                    new Color(0.95f, 0.55f, 0.30f));
                return false;
            }

            var inv = InventorySystem.Instance;
            if (inv == null) return false;
            if (inv.CountOf("stardust") < cost)
            {
                Astrion.UI.ToastUI.Instance?.Show(
                    $"별 가루가 부족합니다. ({cost}개 필요)",
                    new Color(0.95f, 0.55f, 0.30f));
                return false;
            }
            if (!inv.ConsumeAmount("stardust", cost)) return false;

            string label;
            switch (kind)
            {
                case "atk":  ImbueAtkLv++;  label = "공격력 +1";       break;
                case "hp":   ImbueHpLv++;   MaxHp += 20; Hp = Mathf.Min(Hp + 20, MaxHp); label = "활력 +20";   break;
                case "mp":   ImbueMpLv++;   MaxMp += 10; Mp = Mathf.Min(Mp + 10, MaxMp); label = "정신력 +10"; break;
                case "crit": ImbueCritLv++; label = "별의 가호 +1%";  break;
                default: return false;
            }

            _dirty = true;
            SaveAttributes();
            FlushSave();
            OnChanged?.Invoke();
            Astrion.UI.ToastUI.Instance?.Show(
                $"★ 별빛 각인  ·  {label}",
                new Color(0.85f, 0.55f, 0.95f));
            return true;
        }

        private void SaveAttributes()
        {
            var psm = PlayerStateManager.Instance;
            if (psm == null) return;
            psm.UpdateAttributes(Level, Exp, Str, Dex, Intel, Luk, StatPoints, EquippedWeaponId);
            psm.UpdateGold(Gold);
            psm.UpdateEquipment(EquippedWeaponId, EquippedHelmetId, EquippedArmorId, EquippedRingId);
            psm.UpdateImbue(ImbueAtkLv, ImbueHpLv, ImbueMpLv, ImbueCritLv);
            psm.UpdateEnhance(_enhance);
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
                    string cls = UnityEngine.PlayerPrefs.GetString("characterClass", "");
                    string json =
                        "{\"hp\":" + Hp +
                        ",\"maxHp\":" + MaxHp +
                        ",\"level\":" + Level +
                        ",\"intStat\":" + Intel +
                        ",\"weaponDmg\":" + wpn +
                        ",\"starboltLv\":" + starLv +
                        ",\"className\":\"" + EscapeJson(cls) + "\"" +
                        ",\"equippedWeaponId\":\"" + EscapeJson(EquippedWeaponId) + "\"" +
                        ",\"equippedHelmetId\":\"" + EscapeJson(EquippedHelmetId) + "\"" +
                        ",\"equippedArmorId\":\""  + EscapeJson(EquippedArmorId)  + "\"" +
                        ",\"equippedRingId\":\""   + EscapeJson(EquippedRingId)   + "\"" +
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

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
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
