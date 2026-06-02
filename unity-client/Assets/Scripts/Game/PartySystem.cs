using System;
using System.Collections.Generic;
using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    /// DDOL singleton that owns the in-memory party state and the wire
    /// plumbing for PARTY_* packets. Same lifecycle pattern as FriendSystem;
    /// per-scene UIs (PartyWidget, invite buttons, etc.) subscribe to its
    /// events.
    public class PartySystem : MonoBehaviour
    {
        public static PartySystem Instance { get; private set; }

        [Serializable] public class Member {
            public string name; public bool online; public string zone;
            public int hp; public int maxHp; public int level;
        }

        [Serializable] private class UpdatePayload {
            public string partyId; public string leader; public Member[] members;
        }
        [Serializable] private class InviteFromPayload { public string from; }
        [Serializable] private class ErrorPayload { public string message; }
        [Serializable] private class TargetPayload { public string target; }
        [Serializable] private class FromPayload { public string from; }

        public string PartyId { get; private set; } = "";
        public string Leader { get; private set; } = "";
        public IReadOnlyList<Member> Members => _members;
        public bool InParty => _members.Count > 0;
        public bool IsLeader => InParty && !string.IsNullOrEmpty(Leader)
                                && Leader == UnityEngine.PlayerPrefs.GetString("playerId", "");

        /// Pending invite — only the latest. New invites overwrite older
        /// ones since the modal flow only supports one at a time.
        public string PendingInviter { get; private set; } = "";

        public event Action OnPartyUpdated;
        public event Action<string> OnInviteFrom;   // someone invited you
        public event Action<string> OnPartyError;

        private readonly List<Member> _members = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void Start()
        {
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived += OnPacket;
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnSceneChanged;
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived -= OnPacket;
            if (Instance == this) Instance = null;
        }

        /// Pull the freshest roster after every scene transition. The server
        /// pushes PARTY_UPDATE on each membership change, but it doesn't
        /// know we just hopped scenes — without this nudge the widget shows
        /// stale zone strings until the next member action.
        private void OnSceneChanged(UnityEngine.SceneManagement.Scene prev,
                                     UnityEngine.SceneManagement.Scene next)
        {
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            // Skip login/character scenes where the party panel never shows.
            string n = next.name;
            if (n == "LoginScene" || n == "CharacterSelectScene" || n == "CharacterCreateScene") return;
            RequestState();
        }

        // ── Client → server actions ─────────────────────────────────────

        public void Invite(string name)        => SendTarget(PacketType.PartyInvite, name);
        public void Accept(string fromName)    => SendFrom(PacketType.PartyAccept, fromName);
        public void Reject(string fromName)    => SendFrom(PacketType.PartyReject, fromName);
        public void Leave()                    => SendEmpty(PacketType.PartyLeave);
        public void Kick(string name)          => SendTarget(PacketType.PartyKick, name);
        public void RequestState()             => SendEmpty(PacketType.PartyRequest);

        public void AcceptPending()
        {
            if (string.IsNullOrEmpty(PendingInviter)) return;
            string capture = PendingInviter;
            PendingInviter = "";
            Accept(capture);
        }

        public void RejectPending()
        {
            if (string.IsNullOrEmpty(PendingInviter)) return;
            string capture = PendingInviter;
            PendingInviter = "";
            Reject(capture);
        }

        private void SendTarget(PacketType t, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(t, JsonUtility.ToJson(new TargetPayload { target = name.Trim() }));
        }

        private void SendFrom(PacketType t, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(t, JsonUtility.ToJson(new FromPayload { from = name.Trim() }));
        }

        private void SendEmpty(PacketType t)
        {
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(t, "{}");
        }

        // ── Server → client dispatch ────────────────────────────────────

        private void OnPacket(GamePacket packet)
        {
            switch (packet.Type)
            {
                case PacketType.PartyUpdate:
                    try
                    {
                        var data = JsonUtility.FromJson<UpdatePayload>(packet.Payload);
                        PartyId = data?.partyId ?? "";
                        Leader  = data?.leader  ?? "";
                        _members.Clear();
                        if (data?.members != null) _members.AddRange(data.members);
                        OnPartyUpdated?.Invoke();
                    }
                    catch (Exception e) { Debug.LogWarning($"[Party] update parse: {e.Message}"); }
                    break;

                case PacketType.PartyInviteFrom:
                    try
                    {
                        var p = JsonUtility.FromJson<InviteFromPayload>(packet.Payload);
                        if (p != null && !string.IsNullOrEmpty(p.from))
                        {
                            PendingInviter = p.from;
                            OnInviteFrom?.Invoke(p.from);
                        }
                    }
                    catch (Exception e) { Debug.LogWarning($"[Party] invite parse: {e.Message}"); }
                    break;

                case PacketType.PartyError:
                    try
                    {
                        var err = JsonUtility.FromJson<ErrorPayload>(packet.Payload);
                        if (err != null && !string.IsNullOrEmpty(err.message))
                            OnPartyError?.Invoke(err.message);
                    }
                    catch (Exception e) { Debug.LogWarning($"[Party] err parse: {e.Message}"); }
                    break;
            }
        }
    }
}
