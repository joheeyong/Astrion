using System;
using System.Collections.Generic;
using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    /// DDOL singleton for the ranking leaderboard. Owns the in-memory
    /// snapshot per category (level / gold / kills) and the wire plumbing
    /// for RANKING_REQUEST / RANKING_DATA. RankingUI subscribes to OnUpdated
    /// and rebuilds rows when fresh data lands.
    public class RankingSystem : MonoBehaviour
    {
        public static RankingSystem Instance { get; private set; }

        [Serializable] public class Entry { public int rank; public string name; public long score; }
        [Serializable] private class Payload
        {
            public string category;
            public Entry[] entries;
            public int selfRank;
            public long selfScore;
        }
        [Serializable] private class RequestPayload { public string category; }

        public string CurrentCategory { get; private set; } = "level";
        public IReadOnlyList<Entry> Entries => _entries;
        public int SelfRank { get; private set; } = -1;
        public long SelfScore { get; private set; } = 0;
        public event Action OnUpdated;

        private readonly List<Entry> _entries = new();

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

        public void Request(string category)
        {
            if (string.IsNullOrEmpty(category)) category = "level";
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(PacketType.RankingRequest,
                JsonUtility.ToJson(new RequestPayload { category = category }));
        }

        private void OnPacket(GamePacket packet)
        {
            if (packet.Type != PacketType.RankingData) return;
            try
            {
                var data = JsonUtility.FromJson<Payload>(packet.Payload);
                if (data == null) return;
                CurrentCategory = data.category ?? "level";
                _entries.Clear();
                if (data.entries != null) _entries.AddRange(data.entries);
                SelfRank = data.selfRank;
                SelfScore = data.selfScore;
                OnUpdated?.Invoke();
            }
            catch (Exception e) { Debug.LogWarning($"[Ranking] parse: {e.Message}"); }
        }
    }
}
