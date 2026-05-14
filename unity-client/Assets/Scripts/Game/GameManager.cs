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
                Debug.Log($"[Game] Player spawned: {data.playerId}");
            }
        }

        private void OnDespawnPlayer(string payload)
        {
            var data = JsonUtility.FromJson<DespawnData>(payload);
            if (_remotePlayers.TryGetValue(data.playerId, out var go))
            {
                Destroy(go);
                _remotePlayers.Remove(data.playerId);
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
            var go = Instantiate(starBoltPrefab, new Vector3(data.x, data.y, 0f), Quaternion.identity);
            go.SetActive(true);
            var bolt = go.GetComponent<Astrion.Game.StarBolt2D>();
            if (bolt != null) bolt.Init(data.dir, null, visualOnly: true);
        }

        private void OnChatMessage(string payload)
        {
            var data = JsonUtility.FromJson<ChatData>(payload);
            Debug.Log($"[Chat] {data.playerId}: {data.message}");
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
                NetworkManager.Instance.OnPacketReceived -= HandlePacket;
        }

        // JSON DTOs
        [System.Serializable] public class MoveRequest { public float x, y, z; public int facing; }
        [System.Serializable] public class SpawnData { public string playerId; public PositionData position; }
        [System.Serializable] public class DespawnData { public string playerId; }
        [System.Serializable] public class MoveData { public string playerId; public PositionData position; public int facing; }
        [System.Serializable] public class ChatData { public string playerId; public string message; }
        [System.Serializable] public class PositionData { public float x, y, z; }
        [System.Serializable] public class SkillCastData { public string playerId; public float x, y; public int dir; public string type; }
    }
}
