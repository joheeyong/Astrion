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
        [Serializable] private class FriendListPayload {
            public FriendEntry[] friends;
            public string[] incoming;   // requests YOU received
            public string[] outgoing;   // requests YOU sent
        }
        [Serializable] private class FriendErrorPayload { public string message; }
        [Serializable] private class FriendAddedByPayload { public string by; }
        [Serializable] private class FriendRequestFromPayload { public string from; }

        public IReadOnlyList<FriendEntry> Friends => _friends;
        public IReadOnlyList<string> Incoming => _incoming;
        public IReadOnlyList<string> Outgoing => _outgoing;
        public event Action OnFriendListUpdated;
        public event Action<string> OnFriendError;
        public event Action<string> OnAddedBy;       // request accepted (both sides see this)
        public event Action<string> OnRequestFrom;   // someone sent YOU a request

        private readonly List<FriendEntry> _friends = new();
        private readonly List<string> _incoming = new();
        private readonly List<string> _outgoing = new();

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

        public void Remove(string name)        => SendTarget(PacketType.FriendRemove, name);
        public void Accept(string fromName)    => SendTarget(PacketType.FriendAccept, fromName);
        public void Reject(string fromName)    => SendTarget(PacketType.FriendReject, fromName);
        public void CancelOutgoing(string to)  => SendTarget(PacketType.FriendCancel, to);

        private void SendTarget(PacketType t, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(t, JsonUtility.ToJson(new TargetPayload { target = name.Trim() }));
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
                        _incoming.Clear();
                        _outgoing.Clear();
                        if (data != null)
                        {
                            if (data.friends  != null) _friends.AddRange(data.friends);
                            if (data.incoming != null) _incoming.AddRange(data.incoming);
                            if (data.outgoing != null) _outgoing.AddRange(data.outgoing);
                        }
                        OnFriendListUpdated?.Invoke();
                    }
                    catch (Exception e) { Debug.LogWarning($"[Friends] list parse: {e.Message}"); }
                    break;

                case PacketType.FriendRequestFrom:
                    try
                    {
                        var p = JsonUtility.FromJson<FriendRequestFromPayload>(packet.Payload);
                        if (p != null && !string.IsNullOrEmpty(p.from)) OnRequestFrom?.Invoke(p.from);
                    }
                    catch (Exception e) { Debug.LogWarning($"[Friends] req-from parse: {e.Message}"); }
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
