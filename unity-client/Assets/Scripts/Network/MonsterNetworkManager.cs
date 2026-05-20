using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Astrion.Game;

namespace Astrion.Network
{
    public class MonsterNetworkManager : MonoBehaviour
    {
        public static MonsterNetworkManager Instance { get; private set; }

        private readonly Dictionary<string, ServerMonster2D> _monsters = new Dictionary<string, ServerMonster2D>();
        private Sprite _slimeSpr;
        private Sprite _hulkSpr;
        private Sprite _batSpr;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _slimeSpr = BuildSlimeSprite();
            _hulkSpr = BuildShadowHulkSprite();
            _batSpr  = BuildBatSprite();
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
            // Clear any monsters from the previous scene/zone
            ClearLocal();

            string zoneId = SceneToZone(scene.name);
            if (string.IsNullOrEmpty(zoneId)) return;

            // Persist the player's current scene so we can return here on relogin
            PlayerStateManager.Instance?.UpdateLastScene(scene.name);

            StartCoroutine(SendZoneEnterWhenReady(zoneId));
        }

        private void ClearLocal()
        {
            foreach (var kv in _monsters)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            _monsters.Clear();
        }

        private IEnumerator SendZoneEnterWhenReady(string zoneId)
        {
            float deadline = Time.time + 5f;
            while (Time.time < deadline)
            {
                if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
                {
                    string nickname = UnityEngine.PlayerPrefs.GetString("characterName", "");
                    string nickPart = string.IsNullOrEmpty(nickname) ? "" : ",\"nickname\":\"" + EscapeJson(nickname) + "\"";
                    string json = "{\"zoneId\":\"" + zoneId + "\"" + nickPart + "}";
                    NetworkManager.Instance.SendPacket(PacketType.ZoneEnter, json);
                    // zone transitions visible via HUD map name
                    yield break;
                }
                yield return null;
            }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private string SceneToZone(string sceneName)
        {
            switch (sceneName)
            {
                case "MainScene": return "beacon_of_winds";
                case "ForgottenWoodsScene": return "forgotten_woods";
                case "CitadelOfDawnScene": return "citadel_of_dawn";
                default: return "";
            }
        }

        private void HandlePacket(GamePacket packet)
        {
            switch (packet.Type)
            {
                case PacketType.MonsterSpawn:  OnSpawn(packet.Payload);  break;
                case PacketType.MonsterMove:   OnMove(packet.Payload);   break;
                case PacketType.MonsterHp:     OnHp(packet.Payload);     break;
                case PacketType.MonsterDie:    OnDie(packet.Payload);    break;
                case PacketType.MonsterAttack: OnAttack(packet.Payload); break;
            }
        }

        private void OnAttack(string payload)
        {
            try
            {
                var d = JsonUtility.FromJson<AttackData>(payload);
                if (d == null) return;
                // Show a hit flash on the monster sprite regardless of target
                if (_monsters.TryGetValue(d.id, out var m) && m != null)
                    m.OnAttackAnimation();
                // Apply damage only when we're the target
                string myId = UnityEngine.PlayerPrefs.GetString("playerId", "");
                if (d.targetPlayerId == myId && d.damage > 0)
                {
                    Astrion.Game.PlayerStats.Instance?.ApplyDamage(d.damage);
                    Astrion.Game.Camera2D.Shake(0.18f, 0.14f);
                }
            }
            catch { /* ignore */ }
        }

        private void OnSpawn(string payload)
        {
            try
            {
                var d = JsonUtility.FromJson<SpawnData>(payload);
                if (d == null || string.IsNullOrEmpty(d.id)) return;
                if (_monsters.ContainsKey(d.id)) return;
                bool isHulk = d.type == "shadow_hulk";
                bool isBat  = d.type == "bat";
                var go = new GameObject($"Monster_{d.id.Substring(0, System.Math.Min(8, d.id.Length))}");
                var visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);
                var sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = isHulk ? _hulkSpr : (isBat ? _batSpr : _slimeSpr);
                sr.sortingOrder = isHulk ? 10 : 9;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = isHulk
                    ? new Vector2(1.6f, 1.1f)
                    : (isBat ? new Vector2(0.40f, 0.26f) : new Vector2(0.46f, 0.34f));
                col.isTrigger = true;
                var m = go.AddComponent<ServerMonster2D>();
                m.Init(d.id, d.x, d.y, d.direction);
                _monsters[d.id] = m;
            }
            catch (System.Exception e) { Debug.LogWarning($"[Monsters] spawn parse error: {e.Message}"); }
        }

        private void OnMove(string payload)
        {
            try
            {
                var d = JsonUtility.FromJson<MoveData>(payload);
                if (d != null && _monsters.TryGetValue(d.id, out var m) && m != null)
                    m.OnServerMove(d.x, d.y, d.direction);
            }
            catch { /* ignore */ }
        }

        private void OnHp(string payload)
        {
            try
            {
                var d = JsonUtility.FromJson<HpData>(payload);
                if (d != null && _monsters.TryGetValue(d.id, out var m) && m != null)
                    m.OnHpChanged(d.hp, d.damage);
            }
            catch { /* ignore */ }
        }

        private void OnDie(string payload)
        {
            try
            {
                var d = JsonUtility.FromJson<DieData>(payload);
                if (d == null) return;
                if (_monsters.TryGetValue(d.id, out var m) && m != null) m.OnDeath(d.damage);
                _monsters.Remove(d.id);
            }
            catch { /* ignore */ }
        }

        public void SendHit(string monsterId, int damage)
        {
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected) return;
            string json = "{\"id\":\"" + monsterId + "\",\"damage\":" + damage + "}";
            NetworkManager.Instance.SendPacket(PacketType.MonsterHit, json);
        }

        // === Procedural slime sprite (matches old MakeMonsterTex) ===
        private Sprite BuildSlimeSprite()
        {
            int w = 48, h = 36;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color body = new Color(0.30f, 0.62f, 0.38f);
            Color bodyDark = new Color(0.18f, 0.42f, 0.25f);
            Color outline = new Color(0.05f, 0.10f, 0.06f);
            Color eyeWhite = new Color(0.92f, 0.88f, 0.78f);
            Color eyeBlack = new Color(0.05f, 0.04f, 0.02f);
            Color clear = new Color(0, 0, 0, 0);
            float cx = w * 0.5f, cy = h * 0.45f, rx = w * 0.46f, ry = h * 0.48f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = clear;
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    float d = dx * dx + dy * dy;
                    if (d <= 1.0f && y >= h * 0.15f)
                    {
                        float shade = 1f - dy * 0.6f - Mathf.Abs(dx) * 0.2f;
                        c = Color.Lerp(bodyDark, body, Mathf.Clamp01(shade));
                    }
                    int eyeY = (int)(h * 0.55f);
                    int eyeL = (int)(w * 0.32f), eyeR = (int)(w * 0.62f);
                    if ((Mathf.Abs(x - eyeL) < 4 && Mathf.Abs(y - eyeY) < 3) ||
                        (Mathf.Abs(x - eyeR) < 4 && Mathf.Abs(y - eyeY) < 3))
                        c = eyeWhite;
                    if ((Mathf.Abs(x - eyeL) < 2 && Mathf.Abs(y - eyeY) < 2) ||
                        (Mathf.Abs(x - eyeR) < 2 && Mathf.Abs(y - eyeY) < 2))
                        c = eyeBlack;
                    tex.SetPixel(x, y, c);
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (tex.GetPixel(x, y).a > 0)
                    {
                        bool edge = false;
                        if (x > 0 && tex.GetPixel(x - 1, y).a == 0) edge = true;
                        if (y > 0 && tex.GetPixel(x, y - 1).a == 0) edge = true;
                        if (edge) tex.SetPixel(x, y, outline);
                    }
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
        }

        // Bat: small dark winged silhouette with red eyes
        private Sprite BuildBatSprite()
        {
            int w = 40, h = 26;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color body  = new Color(0.12f, 0.10f, 0.14f);
            Color wing  = new Color(0.22f, 0.18f, 0.26f);
            Color outline = new Color(0.03f, 0.02f, 0.04f);
            Color eyeRed = new Color(0.95f, 0.18f, 0.18f);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, clear);

            float cx = w * 0.5f;
            float bodyCy = h * 0.40f;
            float bodyRx = w * 0.12f;
            float bodyRy = h * 0.35f;

            // Body (oval)
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / bodyRx;
                    float dy = (y - bodyCy) / bodyRy;
                    if (dx * dx + dy * dy <= 1f)
                        tex.SetPixel(x, y, body);
                }

            // Wings — two triangles spreading from body
            int wingTop = (int)(h * 0.55f);
            int wingBot = (int)(h * 0.15f);
            for (int x = 0; x < w; x++)
            {
                if (x < cx - bodyRx || x > cx + bodyRx)
                {
                    float dist = Mathf.Abs(x - cx) - bodyRx;
                    float maxDist = (w * 0.5f) - bodyRx;
                    float tNorm = Mathf.Clamp01(dist / maxDist);
                    int topY = (int)Mathf.Lerp(wingTop, wingTop - 8, tNorm);
                    int botY = (int)Mathf.Lerp(wingBot, wingBot + 4, tNorm);
                    for (int y = Mathf.Max(0, botY); y <= Mathf.Min(h - 1, topY); y++)
                        tex.SetPixel(x, y, wing);
                    // small scallop bumps on bottom edge
                    if ((int)(tNorm * 3) % 2 == 0 && botY - 2 >= 0)
                        tex.SetPixel(x, botY - 1, wing);
                }
            }

            // Eyes
            int eyeY = (int)(h * 0.50f);
            int eyeL = (int)(cx - 1.5f);
            int eyeR = (int)(cx + 1.5f);
            tex.SetPixel(eyeL, eyeY, eyeRed);
            tex.SetPixel(eyeR, eyeY, eyeRed);

            // Outline pass
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (tex.GetPixel(x, y).a > 0)
                    {
                        bool edge = false;
                        if (x > 0 && tex.GetPixel(x - 1, y).a == 0) edge = true;
                        if (y > 0 && tex.GetPixel(x, y - 1).a == 0) edge = true;
                        if (edge) tex.SetPixel(x, y, outline);
                    }
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
        }

        // Shadow Hulk: bigger, darker, glowing red eyes
        private Sprite BuildShadowHulkSprite()
        {
            int w = 144, h = 100;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color body = new Color(0.25f, 0.18f, 0.30f);
            Color bodyDark = new Color(0.12f, 0.08f, 0.18f);
            Color outline = new Color(0.04f, 0.02f, 0.05f);
            Color eyeRed = new Color(0.95f, 0.18f, 0.18f);
            Color eyeBlack = new Color(0.10f, 0.02f, 0.02f);
            Color clear = new Color(0, 0, 0, 0);
            float cx = w * 0.5f, cy = h * 0.45f, rx = w * 0.46f, ry = h * 0.48f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = clear;
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    float dd = dx * dx + dy * dy;
                    if (dd <= 1.0f && y >= h * 0.12f)
                    {
                        float shade = 1f - dy * 0.7f - Mathf.Abs(dx) * 0.25f;
                        c = Color.Lerp(bodyDark, body, Mathf.Clamp01(shade));
                    }
                    int eyeY = (int)(h * 0.55f);
                    int eyeL = (int)(w * 0.34f), eyeR = (int)(w * 0.60f);
                    if ((Mathf.Abs(x - eyeL) < 8 && Mathf.Abs(y - eyeY) < 6) ||
                        (Mathf.Abs(x - eyeR) < 8 && Mathf.Abs(y - eyeY) < 6))
                        c = eyeRed;
                    if ((Mathf.Abs(x - eyeL) < 4 && Mathf.Abs(y - eyeY) < 3) ||
                        (Mathf.Abs(x - eyeR) < 4 && Mathf.Abs(y - eyeY) < 3))
                        c = eyeBlack;
                    tex.SetPixel(x, y, c);
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (tex.GetPixel(x, y).a > 0)
                    {
                        bool edge = false;
                        if (x > 0 && tex.GetPixel(x - 1, y).a == 0) edge = true;
                        if (y > 0 && tex.GetPixel(x, y - 1).a == 0) edge = true;
                        if (edge) tex.SetPixel(x, y, outline);
                    }
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
        }

        [System.Serializable] private class SpawnData { public string id; public string type; public float x, y; public int hp, maxHp, direction; }
        [System.Serializable] private class MoveData  { public string id; public float x, y; public int direction; }
        [System.Serializable] private class HpData    { public string id; public int hp; public int damage; }
        [System.Serializable] private class DieData   { public string id; public int damage; }
        [System.Serializable] private class AttackData { public string id; public string targetPlayerId; public int damage; }
    }
}
