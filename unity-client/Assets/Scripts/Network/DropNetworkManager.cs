using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Astrion.Game;

namespace Astrion.Network
{
    public class DropNetworkManager : MonoBehaviour
    {
        public static DropNetworkManager Instance { get; private set; }

        private readonly Dictionary<string, NetworkDrop2D> _drops = new Dictionary<string, NetworkDrop2D>();
        private Sprite _bgSpr;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _bgSpr = BuildRoundedSquareSprite(48, 6);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived += HandlePacket;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived -= HandlePacket;
            if (Instance == this) Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ClearLocal();
        }

        private void ClearLocal()
        {
            foreach (var d in _drops.Values) if (d != null) Destroy(d.gameObject);
            _drops.Clear();
        }

        private void HandlePacket(GamePacket packet)
        {
            switch (packet.Type)
            {
                case PacketType.DropSpawn:   OnSpawn(packet.Payload);   break;
                case PacketType.DropGranted: OnGranted(packet.Payload); break;
                case PacketType.DropRemoved: OnRemoved(packet.Payload); break;
            }
        }

        private void OnSpawn(string payload)
        {
            try
            {
                var d = JsonUtility.FromJson<SpawnData>(payload);
                if (d == null || string.IsNullOrEmpty(d.dropId)) return;
                if (_drops.ContainsKey(d.dropId)) return;
                var def = ItemDatabase.Get(d.itemId);

                var go = new GameObject($"Drop_{d.dropId.Substring(0, System.Math.Min(8, d.dropId.Length))}");
                go.transform.position = new Vector3(d.x, d.y + 0.3f, 0);

                // Visual child (colored square + letter)
                var visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);
                visual.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                var sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = _bgSpr;
                sr.color = def != null ? def.iconColor : new Color(0.7f, 0.7f, 0.7f);
                sr.sortingOrder = 8;

                // Letter via TextMesh
                var letterGo = new GameObject("Letter");
                letterGo.transform.SetParent(go.transform, false);
                letterGo.transform.localScale = new Vector3(0.04f, 0.04f, 1f);
                var tm = letterGo.AddComponent<TextMesh>();
                tm.text = def != null ? def.iconLetter : "?";
                tm.fontSize = 36;
                tm.fontStyle = FontStyle.Bold;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = new Color(0.10f, 0.07f, 0.04f);
                var mr = letterGo.GetComponent<MeshRenderer>();
                mr.sortingOrder = 9;

                var col = go.AddComponent<CircleCollider2D>();
                col.radius = 0.30f;
                col.isTrigger = true;
                var drop = go.AddComponent<NetworkDrop2D>();
                drop.Init(d.dropId, d.itemId, d.quantity);
                _drops[d.dropId] = drop;
            }
            catch (System.Exception e) { Debug.LogWarning($"[Drop] spawn error: {e.Message}"); }
        }

        private void OnGranted(string payload)
        {
            try
            {
                var d = JsonUtility.FromJson<GrantedData>(payload);
                if (d == null || string.IsNullOrEmpty(d.itemId)) return;
                if (InventorySystem.Instance != null)
                    InventorySystem.Instance.Add(d.itemId, d.quantity);
                var def = ItemDatabase.Get(d.itemId);
                string name = def != null ? def.displayName : d.itemId;
                Debug.Log($"[Drop] +{d.quantity} {name} (claimed)");
                Color tint = def != null ? ItemDatabase.RarityColor(def.rarity) : new Color(0.85f, 0.78f, 0.55f);
                Astrion.UI.ToastUI.Instance?.Show($"[+]  {name}  × {d.quantity}", tint);

                // Epic+ drops are worth ack'ing — silent loss of a legendary
                // would be miserable. Lower rarities ride the regular debounced save.
                if (def != null && def.rarity >= ItemDatabase.Rarity.Epic)
                    PlayerStateManager.Instance?.SaveImportant($"드롭: {name}");
            }
            catch { /* ignore */ }
        }

        private void OnRemoved(string payload)
        {
            try
            {
                var d = JsonUtility.FromJson<RemovedData>(payload);
                if (d == null) return;
                if (_drops.TryGetValue(d.dropId, out var nd) && nd != null) Destroy(nd.gameObject);
                _drops.Remove(d.dropId);
            }
            catch { /* ignore */ }
        }

        public void SendClaim(string dropId)
        {
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected) return;
            string json = "{\"dropId\":\"" + dropId + "\"}";
            NetworkManager.Instance.SendPacket(PacketType.DropClaim, json);
        }

        // Procedurally generated rounded square sprite for item bg
        private static Sprite BuildRoundedSquareSprite(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    float dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - Mathf.Max(0, d - radius + 1));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        [System.Serializable] private class SpawnData   { public string dropId; public string itemId; public int quantity; public float x, y; }
        [System.Serializable] private class GrantedData { public string dropId; public string itemId; public int quantity; }
        [System.Serializable] private class RemovedData { public string dropId; }
    }
}
