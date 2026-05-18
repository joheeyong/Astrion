using UnityEngine;

namespace Astrion.Game
{
    /// Single dust puff at the player's feet — fade-out + scale-up over ~0.4s.
    /// Spawned by PlayerController2D on jump take-off and landing.
    public class DustPuff2D : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.4f;
        [SerializeField] private float scaleStart = 0.5f;
        [SerializeField] private float scaleEnd = 1.4f;

        private static Sprite _sprite;
        private SpriteRenderer _sr;
        private float _t;
        private float _driftX;

        public static DustPuff2D Spawn(Vector3 worldPos, float horizontalDrift = 0f)
        {
            EnsureSprite();
            var go = new GameObject("DustPuff");
            go.transform.position = worldPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sprite;
            sr.color = new Color(0.78f, 0.72f, 0.58f, 0.85f);
            sr.sortingOrder = 8;
            var dp = go.AddComponent<DustPuff2D>();
            dp._driftX = horizontalDrift;
            return dp;
        }

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            transform.localScale = Vector3.one * scaleStart;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / lifetime);
            float scale = Mathf.Lerp(scaleStart, scaleEnd, u);
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.position += new Vector3(_driftX * Time.deltaTime, 0.6f * Time.deltaTime * (1f - u), 0f);
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = 0.85f * (1f - u);
                _sr.color = c;
            }
            if (u >= 1f) Destroy(gameObject);
        }

        private static void EnsureSprite()
        {
            if (_sprite != null) return;
            int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    a = Mathf.Pow(a, 1.6f); // softer edge
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            _sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.3f), 100);
        }
    }
}
