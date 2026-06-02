using System;
using System.Collections.Generic;
using UnityEngine;
using Astrion.Network;

namespace Astrion.Game
{
    /// DDOL singleton for P2P trade. Holds the live bilateral state (both
    /// sides' offer slots + gold + lock + confirm flags) and the wire
    /// plumbing for TRADE_* packets. TradeUI subscribes to OnUpdated and
    /// rebuilds the panel on every state push.
    public class TradeSystem : MonoBehaviour
    {
        public static TradeSystem Instance { get; private set; }

        [Serializable] public class Slot { public string itemId = ""; public int qty; }
        [Serializable] public class StatePayload
        {
            public string a; public string b;
            public Slot[] aOffer; public Slot[] bOffer;
            public long aGold; public long bGold;
            public bool aLocked, bLocked;
            public bool aConfirmed, bConfirmed;
        }
        [Serializable] private class RequestFromPayload { public string from; }
        [Serializable] private class OpenPayload { public string partner; }
        [Serializable] public class Gain { public string id; public int qty; }
        [Serializable] private class ResultPayload {
            public bool success; public string message;
            public Gain[] gainedItems; public long gainedGold;
        }
        [Serializable] private class ErrorPayload { public string message; }
        [Serializable] private class TargetPayload { public string target; }
        [Serializable] private class FromPayload { public string from; }
        [Serializable] private class OfferPayload { public int slot; public string itemId; public int qty; }
        [Serializable] private class GoldPayload { public long gold; }

        public string Partner { get; private set; } = "";
        public StatePayload State { get; private set; }
        public string PendingInviter { get; private set; } = "";
        public bool InTrade => !string.IsNullOrEmpty(Partner);

        /// True when *this* client is side A in the underlying record.
        public bool IsSideA => State != null && State.a == UnityEngine.PlayerPrefs.GetString("playerId", "");

        public event Action OnTradeOpen;       // window should open
        public event Action OnTradeUpdated;    // state refreshed
        public event Action OnTradeClosed;     // success or cancel
        public event Action<string> OnRequestFrom;
        public event Action<ResultBundle> OnResult;
        public event Action<string> OnError;

        public class ResultBundle
        {
            public bool success;
            public string message;
            public List<Gain> gained;
            public long gainedGold;
        }

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

        // ── Outgoing ────────────────────────────────────────────────────

        public void RequestTrade(string target) => SendTarget(PacketType.TradeRequest, target);
        public void AcceptPending()
        {
            if (string.IsNullOrEmpty(PendingInviter)) return;
            string from = PendingInviter; PendingInviter = "";
            SendFrom(PacketType.TradeAccept, from);
        }
        public void RejectPending()
        {
            if (string.IsNullOrEmpty(PendingInviter)) return;
            string from = PendingInviter; PendingInviter = "";
            SendFrom(PacketType.TradeReject, from);
        }
        public void Offer(int slot, string itemId, int qty)
        {
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(PacketType.TradeOffer,
                JsonUtility.ToJson(new OfferPayload { slot = slot, itemId = itemId ?? "", qty = qty }));
        }
        public void ClearSlot(int slot) => Offer(slot, "", 0);
        public void SetGold(long gold)
        {
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(PacketType.TradeGold,
                JsonUtility.ToJson(new GoldPayload { gold = Math.Max(0L, gold) }));
        }
        public void Lock()    => SendEmpty(PacketType.TradeLock);
        public void Unlock()  => SendEmpty(PacketType.TradeUnlock);
        public void Confirm() => SendEmpty(PacketType.TradeConfirm);
        public void Cancel()  => SendEmpty(PacketType.TradeCancel);

        private void SendTarget(PacketType t, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var nm = NetworkManager.Instance;
            if (nm == null || !nm.IsConnected) return;
            nm.SendPacket(t, JsonUtility.ToJson(new TargetPayload { target = name.Trim() }));
        }
        private void SendFrom(PacketType t, string name)
        {
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

        // ── Incoming ────────────────────────────────────────────────────

        private void OnPacket(GamePacket packet)
        {
            try
            {
                switch (packet.Type)
                {
                    case PacketType.TradeRequestFrom:
                        var rf = JsonUtility.FromJson<RequestFromPayload>(packet.Payload);
                        if (rf != null && !string.IsNullOrEmpty(rf.from))
                        {
                            PendingInviter = rf.from;
                            OnRequestFrom?.Invoke(rf.from);
                        }
                        break;

                    case PacketType.TradeOpen:
                        var op = JsonUtility.FromJson<OpenPayload>(packet.Payload);
                        Partner = op?.partner ?? "";
                        State = null;
                        OnTradeOpen?.Invoke();
                        break;

                    case PacketType.TradeState:
                        State = JsonUtility.FromJson<StatePayload>(packet.Payload);
                        OnTradeUpdated?.Invoke();
                        break;

                    case PacketType.TradeResult:
                        var rp = JsonUtility.FromJson<ResultPayload>(packet.Payload);
                        if (rp != null)
                        {
                            ApplyResultToInventory(rp);
                            OnResult?.Invoke(new ResultBundle {
                                success = rp.success,
                                message = rp.message ?? "",
                                gained  = rp.gainedItems != null ? new List<Gain>(rp.gainedItems) : new List<Gain>(),
                                gainedGold = rp.gainedGold,
                            });
                        }
                        Partner = ""; State = null;
                        OnTradeClosed?.Invoke();
                        break;

                    case PacketType.TradeError:
                        var ep = JsonUtility.FromJson<ErrorPayload>(packet.Payload);
                        if (ep != null && !string.IsNullOrEmpty(ep.message))
                            OnError?.Invoke(ep.message);
                        break;
                }
            }
            catch (Exception e) { Debug.LogWarning($"[Trade] parse: {e.Message}"); }
        }

        /// Server already mutated the canonical Redis state and saved. To
        /// keep the local InventorySystem coherent without a STATE_REQUEST
        /// round-trip, mirror the delta locally too. PlayerStateManager.State
        /// is updated by InventorySystem.SaveToState on the next change so
        /// no extra work is needed beyond Add().
        private void ApplyResultToInventory(ResultPayload rp)
        {
            if (!rp.success) return;
            var inv = InventorySystem.Instance;
            if (inv != null && rp.gainedItems != null)
            {
                foreach (var g in rp.gainedItems)
                {
                    if (g == null || string.IsNullOrEmpty(g.id) || g.qty <= 0) continue;
                    inv.Add(g.id, g.qty);
                }
            }
            // Items the player gave away were already pulled by the user's
            // Offer/ClearSlot calls; the trade UI keeps a local 'reserved'
            // bookkeeping so the inventory display matches what the server
            // sees. See TradeUI.OfferSlot for the reserve/release pair.
            if (rp.gainedGold != 0)
            {
                var stats = PlayerStats.Instance;
                if (stats != null && rp.gainedGold > 0) stats.AddGold((int)rp.gainedGold);
            }
        }
    }
}
