using System;
using System.Collections.Generic;
using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    /// DDOL singleton that owns the in-memory block (mute) list and the
    /// wire plumbing for BLOCK_* packets. UI panels (BlockListUI, FriendsUI)
    /// subscribe to OnUpdated to refresh.
    ///
    /// Blocking is one-directional and Redis-backed on the server, so the
    /// state survives logouts. Server fan-outs (whisper / friend / party /
    /// trade / chat) all gate on the recipient's block list before delivery
    /// — the client list here is for UI display + commands.
    public class BlockSystem : MonoBehaviour
    {
        public static BlockSystem Instance { get; private set; }

        [Serializable] private class ListPayload { public string[] blocked; }
        [Serializable] private class TargetPayload { public string target; }

        public IReadOnlyList<string> Blocked => _blocked;
        public event Action OnUpdated;

        private readonly List<string> _blocked = new();
        private readonly HashSet<string> _blockedSet = new(StringComparer.OrdinalIgnoreCase);

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

        public bool IsBlocked(string name) =>
            !string.IsNullOrEmpty(name) && _blockedSet.Contains(name);

        public void Block(string name)   => SendTarget(PacketType.BlockAdd, name);
        public void Unblock(string name) => SendTarget(PacketType.BlockRemove, name);
        public void RequestList()        => SendEmpty(PacketType.BlockListRequest);

        private void SendTarget(PacketType t, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(t, JsonUtility.ToJson(new TargetPayload { target = name.Trim() }));
        }

        private void SendEmpty(PacketType t)
        {
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(t, "{}");
        }

        private void OnPacket(GamePacket packet)
        {
            if (packet.Type != PacketType.BlockListData) return;
            try
            {
                var data = JsonUtility.FromJson<ListPayload>(packet.Payload);
                _blocked.Clear();
                _blockedSet.Clear();
                if (data?.blocked != null)
                {
                    foreach (var b in data.blocked)
                    {
                        if (string.IsNullOrEmpty(b)) continue;
                        _blocked.Add(b);
                        _blockedSet.Add(b);
                    }
                }
                OnUpdated?.Invoke();
            }
            catch (Exception e) { Debug.LogWarning($"[Block] parse: {e.Message}"); }
        }
    }
}
