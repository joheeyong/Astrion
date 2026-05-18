using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astrion.Network
{
    [System.Serializable] internal class AckPayload { public string saveId; }

    public class PlayerStateManager : MonoBehaviour
    {
        public static PlayerStateManager Instance { get; private set; }

        public PlayerState State { get; private set; } = new PlayerState();
        public bool IsLoaded { get; private set; }
        // Set true only after we receive STATE_DATA — proves the server can handle state packets.
        // Until then, Save() is a no-op so we don't kill the connection on old servers.
        public bool ServerSupportsState { get; private set; }
        public event Action OnLoaded;

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
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived += HandlePacket;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived -= HandlePacket;
            if (Instance == this) Instance = null;
        }

        public void RequestLoad()
        {
            IsLoaded = false;
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected)
            {
                Debug.LogWarning("[PlayerStateManager] Cannot request load — not connected");
                return;
            }
            NetworkManager.Instance.SendPacket(PacketType.StateRequest, "");
        }

        public void Save()
        {
            if (!ServerSupportsState) return; // skip until handshake confirms support
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected) return;
            // Routine saves: send without saveId so they don't trigger the ACK path
            State.saveId = "";
            string json = JsonUtility.ToJson(State);
            NetworkManager.Instance.SendPacket(PacketType.StateSave, json);
        }

        /// Reliable save with server ACK + retry. Use for level-up, boss kills,
        /// epic+ drops — anything where a silent fail would feel bad.
        public void SaveImportant(string reason)
        {
            if (!ServerSupportsState) return;
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected)
            {
                Debug.LogWarning($"[PlayerStateManager] SaveImportant({reason}) skipped — disconnected");
                return;
            }
            string saveId = System.Guid.NewGuid().ToString("N");
            State.saveId = saveId;
            string json = JsonUtility.ToJson(State);
            State.saveId = ""; // reset so subsequent routine Save() doesn't reuse
            NetworkManager.Instance.SendPacket(PacketType.StateSave, json);
            _pendingAcks[saveId] = new PendingAck { sentAt = Time.unscaledTime, payload = json, reason = reason, retried = false };
        }

        private class PendingAck { public float sentAt; public string payload; public string reason; public bool retried; }
        private readonly Dictionary<string, PendingAck> _pendingAcks = new Dictionary<string, PendingAck>();
        private const float AckTimeoutSeconds = 5f;

        private void Update()
        {
            if (_pendingAcks.Count == 0) return;
            float now = Time.unscaledTime;
            List<string> toRemove = null;
            List<string> toRetry = null;
            List<string> toFail = null;
            foreach (var kv in _pendingAcks)
            {
                float age = now - kv.Value.sentAt;
                if (age < AckTimeoutSeconds) continue;
                if (!kv.Value.retried)
                {
                    (toRetry ??= new List<string>()).Add(kv.Key);
                }
                else
                {
                    (toFail ??= new List<string>()).Add(kv.Key);
                    (toRemove ??= new List<string>()).Add(kv.Key);
                }
            }
            if (toRetry != null)
            {
                foreach (var id in toRetry)
                {
                    var p = _pendingAcks[id];
                    p.retried = true;
                    p.sentAt = now;
                    if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
                        NetworkManager.Instance.SendPacket(PacketType.StateSave, p.payload);
                    Debug.LogWarning($"[PlayerStateManager] ACK timeout — retrying saveId={id.Substring(0,8)} ({p.reason})");
                }
            }
            if (toFail != null)
            {
                foreach (var id in toFail)
                {
                    var p = _pendingAcks[id];
                    Debug.LogError($"[PlayerStateManager] SAVE FAILED saveId={id.Substring(0,8)} ({p.reason})");
                    Astrion.UI.ToastUI.Instance?.Show($"저장 실패: {p.reason}",
                        new Color(0.95f, 0.30f, 0.30f));
                }
            }
            if (toRemove != null)
                foreach (var id in toRemove) _pendingAcks.Remove(id);
        }

        private void HandlePacket(GamePacket packet)
        {
            if (packet.Type == PacketType.StateAck)
            {
                try
                {
                    var ack = JsonUtility.FromJson<AckPayload>(packet.Payload);
                    if (ack != null && !string.IsNullOrEmpty(ack.saveId))
                        _pendingAcks.Remove(ack.saveId);
                }
                catch { /* ignore */ }
                return;
            }
            if (packet.Type != PacketType.StateData) return;

            try
            {
                if (string.IsNullOrEmpty(packet.Payload) || packet.Payload == "{}")
                {
                    State = new PlayerState();
                }
                else
                {
                    var loaded = JsonUtility.FromJson<PlayerState>(packet.Payload);
                    if (loaded != null)
                    {
                        State = loaded;
                        if (State.collectedFragmentIds == null)
                            State.collectedFragmentIds = new string[0];
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerStateManager] Failed to parse state: {e.Message}");
                State = new PlayerState();
            }

            IsLoaded = true;
            ServerSupportsState = true;
            // state load: kept silent — UI surfaces what changed
            OnLoaded?.Invoke();
        }

        public bool IsFragmentCollected(string id)
        {
            if (string.IsNullOrEmpty(id) || State.collectedFragmentIds == null) return false;
            foreach (var s in State.collectedFragmentIds)
                if (s == id) return true;
            return false;
        }

        public void MarkFragmentCollected(string id)
        {
            if (IsFragmentCollected(id)) return;
            var list = new List<string>(State.collectedFragmentIds ?? new string[0]);
            list.Add(id);
            State.collectedFragmentIds = list.ToArray();
            Save();
        }

        public bool IsTargetBroken(string id)
        {
            if (string.IsNullOrEmpty(id) || State.brokenTargetIds == null) return false;
            foreach (var s in State.brokenTargetIds)
                if (s == id) return true;
            return false;
        }

        public void MarkTargetBroken(string id)
        {
            if (IsTargetBroken(id)) return;
            var list = new List<string>(State.brokenTargetIds ?? new string[0]);
            list.Add(id);
            State.brokenTargetIds = list.ToArray();
            Save();
        }

        public bool IsPickupCollected(string id)
        {
            if (string.IsNullOrEmpty(id) || State.collectedPickupIds == null) return false;
            foreach (var s in State.collectedPickupIds)
                if (s == id) return true;
            return false;
        }

        public void MarkPickupCollected(string id)
        {
            if (IsPickupCollected(id)) return;
            var list = new List<string>(State.collectedPickupIds ?? new string[0]);
            list.Add(id);
            State.collectedPickupIds = list.ToArray();
            Save();
        }

        public bool IsQuestCompleted(string id)
        {
            if (string.IsNullOrEmpty(id) || State.completedQuestIds == null) return false;
            foreach (var s in State.completedQuestIds)
                if (s == id) return true;
            return false;
        }

        public void MarkQuestCompleted(string id)
        {
            if (IsQuestCompleted(id)) return;
            var list = new List<string>(State.completedQuestIds ?? new string[0]);
            list.Add(id);
            State.completedQuestIds = list.ToArray();
            // Save called by caller chain (TurnIn → ClearAfterTurnIn → Save)
        }

        public void UpdateStats(int hp, int maxHp, int mp, int maxMp)
        {
            State.hp = hp;
            State.maxHp = maxHp;
            State.mp = mp;
            State.maxMp = maxMp;
            Save();
        }

        public void UpdateLastScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (State.lastScene == sceneName) return;
            State.lastScene = sceneName;
            Save();
        }

        public void UpdateAttributes(int level, int exp, int str, int dex, int @int, int luk, int statPoints, string equippedWeaponId)
        {
            State.level = level;
            State.exp = exp;
            State.statStr = str;
            State.statDex = dex;
            State.statInt = @int;
            State.statLuk = luk;
            State.statPoints = statPoints;
            State.equippedWeaponId = equippedWeaponId ?? "";
            Save();
        }

        public void UpdateGold(int gold)
        {
            State.gold = gold;
            Save();
        }

        public void UpdateEquipment(string weapon, string helmet, string armor, string ring)
        {
            State.equippedWeaponId = weapon ?? "";
            State.equippedHelmetId = helmet ?? "";
            State.equippedArmorId  = armor  ?? "";
            State.equippedRingId   = ring   ?? "";
            Save();
        }

        public void UpdateSkillPoints(int skillPoints)
        {
            State.skillPoints = skillPoints;
            Save();
        }

        public void UpdateQuestState(string questId, string questTitle, int progress, int target, int state)
        {
            State.questId = questId ?? "";
            State.questTitle = questTitle ?? "";
            State.questProgress = progress;
            State.questTarget = target;
            State.questState = state;
            Save();
        }

        public void ClearAfterTurnIn()
        {
            State.questId = "";
            State.questTitle = "";
            State.questProgress = 0;
            State.questTarget = 0;
            State.questState = 0;
            // Note: collected fragments stay cleared since quest is done — don't reset them
            Save();
        }
    }
}
