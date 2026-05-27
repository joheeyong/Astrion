using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Astrion.UI
{
    /// Live minimap that tracks the player + everything else worth showing.
    ///
    /// Every frame: move the 'you' dot to the player's world position.
    /// Every 1 second: scan the scene for ServerMonster2D and the three
    /// NPC types, growing/shrinking a pool of dot Images as needed.
    /// World→panel projection is the same fixed bounds used in the v1
    /// world map scenes (see CreateZoneScene in ProjectSetupWorldMap).
    ///
    /// Pool-based so we don't allocate Images per frame; the FindObjects
    /// calls are throttled because they're the only real cost.
    public class MinimapWidget : MonoBehaviour
    {
        [SerializeField] private RectTransform mapArea;
        [SerializeField] private RectTransform youDot;

        private const float WorldMinX = -26f;
        private const float WorldMaxX =  26f;
        private const float WorldMinY = -5f;
        private const float WorldMaxY =  7f;

        // Throttle the FindObjectsOfType calls — they're scene-wide scans.
        private const float ScanInterval = 1.0f;
        private float _nextScanAt;

        private Transform _playerCached;
        private readonly List<RectTransform> _monsterDots = new();
        private readonly List<RectTransform> _npcDots = new();

        // Runtime-generated, since we don't want a Sprite asset in the
        // project just for this. Built lazily on first scan.
        private Sprite _dotSprite;

        private static readonly Color MonsterColor = new Color(0.85f, 0.30f, 0.30f, 0.95f);
        private static readonly Color NpcColor     = new Color(0.45f, 0.85f, 1.00f, 0.95f);

        private void Update()
        {
            if (mapArea == null || youDot == null) return;
            UpdatePlayerDot();
            if (Time.time >= _nextScanAt)
            {
                _nextScanAt = Time.time + ScanInterval;
                UpdateMonsterDots();
                UpdateNpcDots();
            }
        }

        private void UpdatePlayerDot()
        {
            var player = ResolvePlayer();
            if (player == null) { youDot.gameObject.SetActive(false); return; }
            youDot.gameObject.SetActive(true);
            youDot.anchoredPosition = WorldToMap(player.position);
        }

        private void UpdateMonsterDots()
        {
            var monsters = Object.FindObjectsOfType<Astrion.Game.ServerMonster2D>();
            EnsurePool(_monsterDots, monsters.Length, "MonsterDot", MonsterColor, 6);
            for (int i = 0; i < monsters.Length; i++)
            {
                if (monsters[i] == null) continue;
                _monsterDots[i].gameObject.SetActive(true);
                _monsterDots[i].anchoredPosition = WorldToMap(monsters[i].transform.position);
            }
            for (int i = monsters.Length; i < _monsterDots.Count; i++)
                _monsterDots[i].gameObject.SetActive(false);
        }

        private void UpdateNpcDots()
        {
            var quest = Object.FindObjectsOfType<Astrion.Game.NPC2D>();
            var shops = Object.FindObjectsOfType<Astrion.Game.ShopNPC2D>();
            var inns  = Object.FindObjectsOfType<Astrion.Game.InnkeeperNPC2D>();
            int total = quest.Length + shops.Length + inns.Length;
            EnsurePool(_npcDots, total, "NpcDot", NpcColor, 7);

            int idx = 0;
            foreach (var q in quest) Place(q.transform, ref idx);
            foreach (var s in shops) Place(s.transform, ref idx);
            foreach (var i in inns)  Place(i.transform, ref idx);
            for (int i = idx; i < _npcDots.Count; i++) _npcDots[i].gameObject.SetActive(false);
        }

        private void Place(Transform t, ref int idx)
        {
            if (t == null) return;
            _npcDots[idx].gameObject.SetActive(true);
            _npcDots[idx].anchoredPosition = WorldToMap(t.position);
            idx++;
        }

        private void EnsurePool(List<RectTransform> pool, int desired, string namePrefix, Color color, int size)
        {
            while (pool.Count < desired)
            {
                if (_dotSprite == null) _dotSprite = BuildDotSprite();
                var go = new GameObject(namePrefix + pool.Count);
                go.transform.SetParent(mapArea, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(size, size);
                var img = go.AddComponent<Image>();
                img.sprite = _dotSprite;
                img.color = color;
                img.raycastTarget = false;
                pool.Add(rt);
            }
        }

        private Vector2 WorldToMap(Vector3 wp)
        {
            float tx = Mathf.InverseLerp(WorldMinX, WorldMaxX, wp.x);
            float ty = Mathf.InverseLerp(WorldMinY, WorldMaxY, wp.y);
            Vector2 size = mapArea.rect.size;
            return new Vector2((tx - 0.5f) * size.x, (ty - 0.5f) * size.y);
        }

        private Transform ResolvePlayer()
        {
            if (_playerCached != null && _playerCached.gameObject.activeInHierarchy) return _playerCached;
            var p = Object.FindObjectOfType<Astrion.Game.PlayerController2D>();
            if (p != null) _playerCached = p.transform;
            return _playerCached;
        }

        /// 16×16 filled circle, white. We tint per-image so one sprite serves
        /// every dot.
        private static Sprite BuildDotSprite()
        {
            const int s = 16;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float cx = s * 0.5f, cy = s * 0.5f, r2 = (s * 0.5f - 0.5f) * (s * 0.5f - 0.5f);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x - cx, dy = y - cy;
                tex.SetPixel(x, y, dx * dx + dy * dy <= r2 ? Color.white : new Color(0, 0, 0, 0));
            }
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
