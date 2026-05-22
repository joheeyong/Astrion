using System;
using System.Collections.Generic;
using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    /// DDOL singleton that owns the in-memory friend list and the wire
    /// plumbing for FRIEND_* packets. Survives scene loads so the UI panel
    /// (FriendsUI in any scene's HUD) reads from a single live source.
    public class FriendSystem : MonoBehaviour
    {
        public static FriendSystem Instance { get; private set; }

        [Serializable] public class FriendEntry { public string name; public bool online; public string zone; }
        [Serializable] private class FriendListPayload { public FriendEntry[] friends; }
        [Serializable] private class FriendErrorPayload { public string message; }
        [Serializable] private class FriendAddedByPayload { public string by; }

        public IReadOnlyList<FriendEntry> Friends => _friends;
        public event Action OnFriendListUpdated;
        public event Action<string> OnFriendError;
        public event Action<string> OnAddedBy;  // someone added YOU

        private readonly List<FriendEntry> _friends = new();

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

        public void RequestList()
        {
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(PacketType.FriendListRequest, "{}");
        }

        public void Add(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            string payload = JsonUtility.ToJson(new TargetPayload { target = name.Trim() });
            nm.SendPacket(PacketType.FriendAdd, payload);
        }

        public void Remove(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            string payload = JsonUtility.ToJson(new TargetPayload { target = name });
            nm.SendPacket(PacketType.FriendRemove, payload);
        }

        [Serializable] private class TargetPayload { public string target; }

        private void OnPacket(GamePacket packet)
        {
            switch (packet.Type)
            {
                case PacketType.FriendListData:
                    try
                    {
                        var data = JsonUtility.FromJson<FriendListPayload>(packet.Payload);
                        _friends.Clear();
                        if (data != null && data.friends != null) _friends.AddRange(data.friends);
                        OnFriendListUpdated?.Invoke();
                    }
                    catch (Exception e) { Debug.LogWarning($"[Friends] list parse: {e.Message}"); }
                    break;

                case PacketType.FriendError:
                    try
                    {
                        var err = JsonUtility.FromJson<FriendErrorPayload>(packet.Payload);
                        if (err != null && !string.IsNullOrEmpty(err.message))
                            OnFriendError?.Invoke(err.message);
                    }
                    catch (Exception e) { Debug.LogWarning($"[Friends] err parse: {e.Message}"); }
                    break;

                case PacketType.FriendAddedBy:
                    try
                    {
                        var who = JsonUtility.FromJson<FriendAddedByPayload>(packet.Payload);
                        if (who != null && !string.IsNullOrEmpty(who.by))
                            OnAddedBy?.Invoke(who.by);
                    }
                    catch (Exception e) { Debug.LogWarning($"[Friends] added-by parse: {e.Message}"); }
                    break;
            }
        }
    }
}
