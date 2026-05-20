using System.Collections.Generic;
using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    public class SkillCaster : MonoBehaviour
    {
        public static SkillCaster Instance { get; private set; }

        private readonly Dictionary<string, float> _lastCastAt = new Dictionary<string, float>();

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

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Cast a skill by id. Returns true if it was triggered.</summary>
        public bool Cast(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return false;
            var def = SkillDatabase.Get(skillId);
            if (def == null) return false;
            if (!def.isActive) return false;

            var skills = SkillSystem.Instance;
            int lv = skills != null ? skills.GetLevel(skillId) : 0;
            if (lv <= 0)
            {
                // Class primary attacks always usable (defends against load-order race
                // where the class skill hasn't been auto-granted yet on first entry).
                if (skillId == "starbolt" || skillId == "sword_slash") lv = 1;
                else return false;
            }

            // Cooldown
            float now = Time.time;
            if (_lastCastAt.TryGetValue(skillId, out var t) && now - t < def.cooldown) return false;

            // MP gate (consume only after dispatch succeeds so we don't drain on bad state)
            var stats = PlayerStats.Instance;
            if (stats == null) return false;
            if (stats.Mp < def.mpCost) return false;

            bool fired = false;
            switch (skillId)
            {
                case "starbolt":      fired = FireStarbolt(); break;
                case "meteor":        fired = FireMeteor(lv); break;
                case "stellar_heal":  fired = FireStellarHeal(lv); break;
                case "sword_slash":   fired = FireSwordSlash(lv); break;
                default: return false;
            }
            if (!fired) return false;

            if (def.mpCost > 0) stats.ConsumeMp(def.mpCost);
            _lastCastAt[skillId] = now;
            return true;
        }

        public float GetCooldownRemaining(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return 0f;
            var def = SkillDatabase.Get(skillId);
            if (def == null || def.cooldown <= 0f) return 0f;
            if (!_lastCastAt.TryGetValue(skillId, out var t)) return 0f;
            return Mathf.Max(0f, def.cooldown - (Time.time - t));
        }

        public float GetCooldownPct(string skillId)
        {
            var def = SkillDatabase.Get(skillId);
            if (def == null || def.cooldown <= 0f) return 0f;
            return GetCooldownRemaining(skillId) / def.cooldown;
        }

        private PlayerController2D FindPlayer()
        {
            return Object.FindObjectOfType<PlayerController2D>();
        }

        private bool FireStarbolt()
        {
            var p = FindPlayer();
            if (p == null) return false;
            return p.FireStarBoltExternal();
        }

        // Simple AoE: hit every ServerMonster2D within 6 units in front of the player
        private bool FireMeteor(int lv)
        {
            var p = FindPlayer();
            if (p == null) return false;
            var stats = PlayerStats.Instance;
            int baseDmg = 8 + stats.Intel * 3 + stats.Level * 2 + (lv - 1) * 6;
            int dmg = Mathf.Max(1, baseDmg);

            Vector2 origin = p.transform.position;
            float facing = p.FacingRight ? 1f : -1f;
            float reach = 6f;
            float halfHeight = 3f;

            var monsters = Object.FindObjectsOfType<ServerMonster2D>();
            int hit = 0;
            var nm = Astrion.Network.MonsterNetworkManager.Instance;
            foreach (var m in monsters)
            {
                if (m == null) continue;
                Vector2 to = (Vector2)m.transform.position - origin;
                if (to.x * facing < -0.5f) continue;
                if (Mathf.Abs(to.x) > reach) continue;
                if (Mathf.Abs(to.y) > halfHeight) continue;
                if (nm != null) nm.SendHit(m.Id, dmg);
                hit++;
            }

            // Visual: spawn a few starbolts as a fan-out flash (cheap placeholder)
            p.FireMeteorVisualBurst(8);

            // Broadcast SkillCast so others see it
            BroadcastSkillCast(origin, p.FacingRight ? 1 : -1, "meteor");

            // meteor results visible via damage popups
            return true;
        }

        private bool FireSwordSlash(int lv)
        {
            var p = FindPlayer();
            if (p == null)
            {
                Debug.LogWarning("[SwordSlash] player not found");
                return false;
            }
            var stats = PlayerStats.Instance;
            int weaponDmg = 0;
            if (stats != null && !string.IsNullOrEmpty(stats.EquippedWeaponId))
            {
                var def = ItemDatabase.Get(stats.EquippedWeaponId);
                if (def != null) weaponDmg = def.baseDamage;
            }
            int baseDmg = 5 + (stats != null ? stats.Str * 2 + stats.Level * 3 : 0) + weaponDmg + (lv - 1) * 5;
            float variance = baseDmg * 0.2f;
            int dmg = Mathf.Max(1, Mathf.RoundToInt(baseDmg + Random.Range(-variance, variance)));
            bool crit = stats != null && stats.RollCritical();
            if (crit) dmg = Mathf.RoundToInt(dmg * 1.7f);

            // Hit everything in a narrow front cone
            Vector2 origin = p.transform.position;
            float facing = p.FacingRight ? 1f : -1f;
            float reach = 1.7f;
            float halfHeight = 0.7f;

            var monsters = Object.FindObjectsOfType<ServerMonster2D>();
            int hits = 0;
            var nm = Astrion.Network.MonsterNetworkManager.Instance;
            foreach (var m in monsters)
            {
                if (m == null) continue;
                Vector2 to = (Vector2)m.transform.position - origin;
                if (to.x * facing < -0.1f) continue;        // behind
                if (Mathf.Abs(to.x) > reach) continue;
                if (Mathf.Abs(to.y) > halfHeight) continue;
                if (nm != null) nm.SendHit(m.Id, dmg, crit);
                hits++;
            }
            // Hit-stop fires once per swing (not per monster) — only if anything was hit
            if (hits > 0)
                Camera2D.HitStop(crit ? 0.08f : 0.05f, 0.20f, crit ? 0.55f : 0.40f);

            // Visual: large arm swing (no star projectile)
            var anim = p.GetComponent<PlayerAnimator2D>();
            if (anim != null) anim.TriggerAttackMotion(bigSwing: true);
            else Debug.LogWarning("[SwordSlash] no PlayerAnimator2D on player");

            // Tell others to play the swing visual
            BroadcastSkillCast(origin, p.FacingRight ? 1 : -1, "sword_slash");
            return true;
        }

        private bool FireStellarHeal(int lv)
        {
            var stats = PlayerStats.Instance;
            if (stats == null) return false;
            int heal = 20 + (lv - 1) * 10;
            stats.Heal(heal);
            var p = FindPlayer();
            if (p != null) p.FireHealVisualBurst();

            Vector2 origin = p != null ? (Vector2)p.transform.position : Vector2.zero;
            int dir = (p != null && p.FacingRight) ? 1 : -1;
            BroadcastSkillCast(origin, dir, "stellar_heal");
            // heal visible via HP bar jump
            return true;
        }

        private void BroadcastSkillCast(Vector2 origin, int dir, string type)
        {
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            string payload = "{\"x\":" + origin.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                + ",\"y\":" + origin.y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                + ",\"dir\":" + dir + ",\"type\":\"" + type + "\"}";
            nm.SendPacket(PacketType.SkillCast, payload);
        }
    }
}
