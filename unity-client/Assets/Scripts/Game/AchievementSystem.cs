using System;
using System.Collections.Generic;
using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    /// DDOL singleton that caches unlocked achievements + current progress
    /// counters and hosts the wire plumbing for ACHIEVEMENT_* packets.
    /// AchievementUI subscribes to OnListUpdated and OnUnlocked.
    ///
    /// The full defs list is hardcoded *both* server-side (AchievementManager)
    /// and client-side (AchievementDatabase). They must stay in lockstep —
    /// the server only sends ids on unlock, so the client uses the local
    /// def for display name / reward / threshold.
    public class AchievementSystem : MonoBehaviour
    {
        public static AchievementSystem Instance { get; private set; }

        [Serializable] public class Unlock {
            public string id; public string displayName; public string description;
            public string rewardItemId; public int rewardQty;
        }
        [Serializable] public class Progress {
            public long level; public long kills; public long gold;
            public long friends; public long cities;
        }
        [Serializable] private class ListPayload {
            public string[] unlocked;
            public Progress progress;
        }

        public IReadOnlyCollection<string> Unlocked => _unlocked;
        public Progress CurrentProgress { get; private set; } = new Progress();
        public event Action OnListUpdated;
        public event Action<Unlock> OnUnlocked;

        private readonly HashSet<string> _unlocked = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived += OnPacket;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived -= OnPacket;
            if (Instance == this) Instance = null;
        }

        public bool IsUnlocked(string id) =>
            !string.IsNullOrEmpty(id) && _unlocked.Contains(id);

        public void RequestList()
        {
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(PacketType.AchievementListRequest, "{}");
        }

        private void OnPacket(GamePacket packet)
        {
            try
            {
                if (packet.Type == PacketType.AchievementUnlock)
                {
                    var u = JsonUtility.FromJson<Unlock>(packet.Payload);
                    if (u == null || string.IsNullOrEmpty(u.id)) return;
                    _unlocked.Add(u.id);
                    ApplyRewardLocally(u);
                    OnUnlocked?.Invoke(u);
                    OnListUpdated?.Invoke();
                    ShowUnlockToast(u);
                }
                else if (packet.Type == PacketType.AchievementListData)
                {
                    var data = JsonUtility.FromJson<ListPayload>(packet.Payload);
                    _unlocked.Clear();
                    if (data?.unlocked != null)
                        foreach (var id in data.unlocked) if (!string.IsNullOrEmpty(id)) _unlocked.Add(id);
                    CurrentProgress = data?.progress ?? new Progress();
                    OnListUpdated?.Invoke();
                }
            }
            catch (Exception e) { Debug.LogWarning($"[Ach] parse: {e.Message}"); }
        }

        /// Server already appended the reward into the stored state JSON
        /// (AchievementManager.grantReward). Mirror into the live
        /// InventorySystem so the local view matches without waiting for
        /// the next RestoreFromState round trip.
        private void ApplyRewardLocally(Unlock u)
        {
            if (string.IsNullOrEmpty(u.rewardItemId) || u.rewardQty <= 0) return;
            InventorySystem.Instance?.Add(u.rewardItemId, u.rewardQty);
        }

        private static void ShowUnlockToast(Unlock u)
        {
            string body = $"★ 업적 해금  ·  {u.displayName}";
            if (!string.IsNullOrEmpty(u.rewardItemId) && u.rewardQty > 0)
                body += $"  (+{u.rewardQty} {ItemNameOf(u.rewardItemId)})";
            Astrion.UI.ToastUI.Instance?.Show(body, new Color(0.95f, 0.82f, 0.35f));
        }

        private static string ItemNameOf(string id)
        {
            var def = ItemDatabase.Get(id);
            return def != null ? def.displayName : id;
        }
    }
}
