using System.Collections.Generic;
using Astrion.Network;
using UnityEngine;

namespace Astrion.Game
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject remotePlayerPrefab;
        [SerializeField] private GameObject starBoltPrefab; // for visualizing other players' skill casts

        private string _playerId;
        private GameObject _localPlayer;
        private readonly Dictionary<string, GameObject> _remotePlayers = new();
        private readonly Dictionary<string, Astrion.UI.PlayerNameTag> _remoteTags = new();

        private void Start()
        {
            _playerId = PlayerPrefs.GetString("playerId", "");
            if (string.IsNullOrEmpty(_playerId))
            {
                Debug.LogError("[Game] No playerId found. Should not reach MainScene without login.");
                return;
            }

            Debug.Log($"[Game] Entered game as {_playerId}");
            NetworkManager.Instance.OnPacketReceived += HandlePacket;

            // Spawn local player or use scene-time PlayerPrefab
            if (playerPrefab != null)
            {
                _localPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                _localPlayer.name = "LocalPlayer";
            }
            else
            {
                _localPlayer = GameObject.Find("PlayerPrefab");
            }

            // Self name tag (gold tint, no HP bar since HUD already shows it)
            if (_localPlayer != null && _localPlayer.GetComponent<Astrion.UI.PlayerNameTag>() == null)
            {
                var tag = _localPlayer.AddComponent<Astrion.UI.PlayerNameTag>();
                string displayName = PlayerPrefs.GetString("characterName", _playerId);
                tag.SetName(displayName);
                tag.SetNameColor(new Color(1f, 0.92f, 0.45f));
                tag.ShowHpBar(false);
            }

            EnsureCameraFollow();
        }

        private void EnsureCameraFollow()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            if (cam.GetComponent<Camera2D>() == null)
                cam.gameObject.AddComponent<Camera2D>();
        }

        private void HandlePacket(GamePacket packet)
        {
            switch (packet.Type)
            {
                case PacketType.SpawnPlayer:
                    OnSpawnPlayer(packet.Payload);
                    break;
                case PacketType.DespawnPlayer:
                    OnDespawnPlayer(packet.Payload);
                    break;
                case PacketType.PlayerMoved:
                    OnPlayerMoved(packet.Payload);
                    break;
                case PacketType.ChatMessage:
                    OnChatMessage(packet.Payload);
                    break;
                case PacketType.SkillBroadcast:
                    OnSkillBroadcast(packet.Payload);
                    break;
                case PacketType.PlayerStatus:
                    OnPlayerStatus(packet.Payload);
                    break;
            }
        }

        private void Update()
        {
            if (_localPlayer == null || !NetworkManager.Instance.IsConnected) return;

            Vector3 pos = _localPlayer.transform.position;
            int facing = 1;
            var pc = _localPlayer.GetComponent<PlayerController2D>();
            if (pc != null) facing = pc.FacingRight ? 1 : -1;
            string movePayload = JsonUtility.ToJson(new MoveRequest { x = pos.x, y = pos.y, z = pos.z, facing = facing });
            NetworkManager.Instance.SendPacket(PacketType.Move, movePayload);
        }

        private void OnSpawnPlayer(string payload)
        {
            var data = JsonUtility.FromJson<SpawnData>(payload);
            if (data.playerId == _playerId) return;

            if (!_remotePlayers.ContainsKey(data.playerId))
            {
                var go = Instantiate(remotePlayerPrefab,
                    new Vector3(data.position.x, data.position.y, data.position.z),
                    Quaternion.identity);
                go.name = "Remote_" + data.playerId;
                _remotePlayers[data.playerId] = go;

                var tag = go.AddComponent<Astrion.UI.PlayerNameTag>();
                tag.SetName(string.IsNullOrEmpty(data.nickname) ? data.playerId : data.nickname);
                tag.SetNameColor(new Color(0.85f, 0.86f, 0.95f)); // remote = cooler tint
                tag.SetHp(100, 100); // until first PLAYER_STATUS arrives
                _remoteTags[data.playerId] = tag;

                Debug.Log($"[Game] Player spawned: {data.playerId} ({data.nickname})");
            }
        }

        private void OnPlayerStatus(string payload)
        {
            var data = JsonUtility.FromJson<PlayerStatusData>(payload);
            if (data == null || string.IsNullOrEmpty(data.playerId)) return;
            if (_remoteTags.TryGetValue(data.playerId, out var tag) && tag != null)
                tag.SetHp(data.hp, data.maxHp);
        }

        private void OnDespawnPlayer(string payload)
        {
            var data = JsonUtility.FromJson<DespawnData>(payload);
            if (_remotePlayers.TryGetValue(data.playerId, out var go))
            {
                Destroy(go);
                _remotePlayers.Remove(data.playerId);
                _remoteTags.Remove(data.playerId);
                Debug.Log($"[Game] Player despawned: {data.playerId}");
            }
        }

        private void OnPlayerMoved(string payload)
        {
            var data = JsonUtility.FromJson<MoveData>(payload);
            if (_remotePlayers.TryGetValue(data.playerId, out var go))
            {
                go.transform.position = new Vector3(data.position.x, data.position.y, data.position.z);
                // Flip sprite container based on facing
                var container = go.transform.Find("SpriteContainer");
                if (container != null && data.facing != 0)
                {
                    var s = container.localScale;
                    s.x = data.facing > 0 ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
                    container.localScale = s;
                }
            }
        }

        private void OnSkillBroadcast(string payload)
        {
            var data = JsonUtility.FromJson<SkillCastData>(payload);
            if (data == null || data.playerId == _playerId) return; // self — already spawned locally
            if (starBoltPrefab == null) return;
            var origin = new Vector3(data.x, data.y, 0f);
            var go = Instantiate(starBoltPrefab, origin, Quaternion.identity);
            go.SetActive(true);
            var bolt = go.GetComponent<Astrion.Game.StarBolt2D>();
            if (bolt != null) bolt.Init(data.dir, FindHomingTargetForRemote(origin, data.dir), visualOnly: true);
        }

        // Match the local FireStarbolt homing logic so remote bolts visibly curve toward enemies
        private Transform FindHomingTargetForRemote(Vector2 origin, int dir)
        {
            var monsters = Object.FindObjectsOfType<Astrion.Game.ServerMonster2D>();
            Transform best = null;
            float bestDist = float.MaxValue;
            const float range = 9f;
            foreach (var m in monsters)
            {
                if (m == null) continue;
                Vector2 to = (Vector2)m.transform.position - origin;
                if (to.x * dir < 0f) continue; // not in front of the caster
                float d = to.magnitude;
                if (d > range) continue;
                if (d < bestDist) { bestDist = d; best = m.transform; }
            }
            return best;
        }

        private void OnChatMessage(string payload)
        {
            var data = JsonUtility.FromJson<ChatData>(payload);
            if (data == null || string.IsNullOrEmpty(data.message)) return;
            Debug.Log($"[Chat] {data.playerId}: {data.message}");
            var hud = Astrion.UI.GameHUD.Instance;
            if (hud != null) hud.AppendChatLine(data.playerId, data.message);
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived -= HandlePacket;
        }

        // JSON DTOs
        [System.Serializable] public class MoveRequest { public float x, y, z; public int facing; }
        [System.Serializable] public class SpawnData { public string playerId; public string nickname; public PositionData position; }
        [System.Serializable] public class DespawnData { public string playerId; }
        [System.Serializable] public class MoveData { public string playerId; public PositionData position; public int facing; }
        [System.Serializable] public class ChatData { public string playerId; public string message; }
        [System.Serializable] public class PositionData { public float x, y, z; }
        [System.Serializable] public class SkillCastData { public string playerId; public float x, y; public int dir; public string type; }
        [System.Serializable] public class PlayerStatusData { public string playerId; public int hp; public int maxHp; }
    }
}
