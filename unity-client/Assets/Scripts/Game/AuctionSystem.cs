using System;
using System.Collections.Generic;
using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    /// DDOL singleton — caches active auction listings and owns the wire
    /// plumbing for AUCTION_* packets. AuctionUI subscribes to OnListUpdated
    /// for re-rendering and OnResult for toast/state refresh.
    public class AuctionSystem : MonoBehaviour
    {
        public static AuctionSystem Instance { get; private set; }

        [Serializable] public class Entry {
            public string id; public string seller; public string itemId;
            public int qty; public long price; public long expiresAt;
            public bool mine;
        }
        [Serializable] private class ListPayload { public Entry[] entries; }
        [Serializable] public class Result {
            public bool success; public string message; public string action;
        }
        [Serializable] private class RegisterPayload {
            public string itemId; public int qty; public long price; public long durationHours;
        }
        [Serializable] private class IdPayload { public string auctionId; }

        public IReadOnlyList<Entry> Entries => _entries;
        public event Action OnListUpdated;
        public event Action<Result> OnResult;

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

        public void RequestList()
        {
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(PacketType.AuctionListRequest, "{}");
        }

        public void Register(string itemId, int qty, long price, long durationHours = 24)
        {
            if (string.IsNullOrEmpty(itemId) || qty <= 0 || price <= 0) return;
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(PacketType.AuctionRegister,
                JsonUtility.ToJson(new RegisterPayload {
                    itemId = itemId, qty = qty, price = price, durationHours = durationHours
                }));
        }

        public void Buy(string auctionId)
        {
            if (string.IsNullOrEmpty(auctionId)) return;
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(PacketType.AuctionBuy, JsonUtility.ToJson(new IdPayload { auctionId = auctionId }));
        }

        public void Cancel(string auctionId)
        {
            if (string.IsNullOrEmpty(auctionId)) return;
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(PacketType.AuctionCancel, JsonUtility.ToJson(new IdPayload { auctionId = auctionId }));
        }

        private void OnPacket(GamePacket packet)
        {
            try
            {
                if (packet.Type == PacketType.AuctionListData)
                {
                    var data = JsonUtility.FromJson<ListPayload>(packet.Payload);
                    _entries.Clear();
                    if (data?.entries != null) _entries.AddRange(data.entries);
                    OnListUpdated?.Invoke();
                }
                else if (packet.Type == PacketType.AuctionResult)
                {
                    var r = JsonUtility.FromJson<Result>(packet.Payload);
                    if (r == null) return;
                    OnResult?.Invoke(r);
                    Astrion.UI.ToastUI.Instance?.Show(
                        $"[경매] {r.message}",
                        r.success
                            ? new Color(0.55f, 0.85f, 0.45f)
                            : new Color(0.95f, 0.55f, 0.30f));
                }
                // STATE_DATA is handled by PlayerStateManager; auction success
                // triggers it server-side to keep our InventorySystem fresh.
            }
            catch (Exception e) { Debug.LogWarning($"[Auction] parse: {e.Message}"); }
        }
    }
}
