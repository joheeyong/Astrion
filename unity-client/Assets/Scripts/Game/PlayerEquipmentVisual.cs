using UnityEngine;

namespace Astrion.Game
{
    /// Drives the four equipment overlays under SpriteContainer (Weapon/Helmet/Armor/Ring)
    /// from PlayerStats. Subscribes to PlayerStats.OnChanged so equipping in the
    /// inventory updates the body visual immediately.
    public class PlayerEquipmentVisual : MonoBehaviour
    {
        private SpriteRenderer _weaponSR, _helmetSR, _armorSR, _ringSR;

        // Cached procedural sprites — shared across instances
        private static Sprite _daggerSprite;
        private static Sprite _bowSprite;
        private static Sprite _helmetSprite;
        private static Sprite _armorSprite;
        private static Sprite _ringSprite;

        private void Awake()
        {
            EnsureSpritesBuilt();
            _weaponSR = transform.Find("SpriteContainer/WeaponVisual")?.GetComponent<SpriteRenderer>();
            _helmetSR = transform.Find("SpriteContainer/HelmetVisual")?.GetComponent<SpriteRenderer>();
            _armorSR  = transform.Find("SpriteContainer/ArmorVisual")?.GetComponent<SpriteRenderer>();
            _ringSR   = transform.Find("SpriteContainer/RingVisual")?.GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (PlayerStats.Instance != null) PlayerStats.Instance.OnChanged -= Refresh;
        }

        private void Refresh()
        {
            var stats = PlayerStats.Instance;
            ApplyWeapon(stats != null ? stats.EquippedWeaponId : "");
            ApplyHelmet(stats != null ? stats.EquippedHelmetId : "");
            ApplyArmor (stats != null ? stats.EquippedArmorId  : "");
            ApplyRing  (stats != null ? stats.EquippedRingId   : "");
        }

        private void ApplyWeapon(string id)
        {
            if (_weaponSR == null) return;
            if (string.IsNullOrEmpty(id)) { _weaponSR.gameObject.SetActive(false); return; }
            var def = ItemDatabase.Get(id);
            if (def == null) { _weaponSR.gameObject.SetActive(false); return; }
            bool isBow = id == "star_bow";
            _weaponSR.sprite = isBow ? _bowSprite : _daggerSprite;
            _weaponSR.color = def.iconColor;
            _weaponSR.gameObject.SetActive(true);
        }

        private void ApplyHelmet(string id)
        {
            if (_helmetSR == null) return;
            if (string.IsNullOrEmpty(id)) { _helmetSR.gameObject.SetActive(false); return; }
            var def = ItemDatabase.Get(id);
            if (def == null) { _helmetSR.gameObject.SetActive(false); return; }
            _helmetSR.sprite = _helmetSprite;
            _helmetSR.color = def.iconColor;
            _helmetSR.gameObject.SetActive(true);
        }

        private void ApplyArmor(string id)
        {
            if (_armorSR == null) return;
            if (string.IsNullOrEmpty(id)) { _armorSR.gameObject.SetActive(false); return; }
            var def = ItemDatabase.Get(id);
            if (def == null) { _armorSR.gameObject.SetActive(false); return; }
            _armorSR.sprite = _armorSprite;
            _armorSR.color = new Color(def.iconColor.r, def.iconColor.g, def.iconColor.b, 0.85f);
            _armorSR.gameObject.SetActive(true);
        }

        private void ApplyRing(string id)
        {
            if (_ringSR == null) return;
            if (string.IsNullOrEmpty(id)) { _ringSR.gameObject.SetActive(false); return; }
            var def = ItemDatabase.Get(id);
            if (def == null) { _ringSR.gameObject.SetActive(false); return; }
            _ringSR.sprite = _ringSprite;
            _ringSR.color = def.iconColor;
            _ringSR.gameObject.SetActive(true);
        }

        // === Procedural equipment sprites ===
        private static void EnsureSpritesBuilt()
        {
            if (_daggerSprite == null) _daggerSprite = MakeRectSprite(5, 18);
            if (_bowSprite    == null) _bowSprite    = MakeBowSprite(18, 24);
            if (_helmetSprite == null) _helmetSprite = MakeHelmetSprite(20, 10);
            if (_armorSprite  == null) _armorSprite  = MakeArmorSprite(26, 30);
            if (_ringSprite   == null) _ringSprite   = MakeRingSprite(8);
        }

        private static Sprite MakeRectSprite(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool edge = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                    tex.SetPixel(x, y, edge ? new Color(0.05f, 0.04f, 0.06f) : Color.white);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 100);
        }

        private static Sprite MakeBowSprite(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
            // Bow curve: leftmost column
            for (int y = 1; y < h - 1; y++)
            {
                float t = (float)y / (h - 1);
                int curve = (int)((1f - Mathf.Abs(t * 2f - 1f)) * (w * 0.6f));
                int xLeft = Mathf.Clamp(curve, 0, w - 1);
                tex.SetPixel(xLeft, y, Color.white);
                if (xLeft + 1 < w) tex.SetPixel(xLeft + 1, y, new Color(0.05f, 0.04f, 0.06f));
            }
            // String down the right side
            for (int y = 1; y < h - 1; y++) tex.SetPixel(w - 1, y, new Color(0.9f, 0.85f, 0.7f));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
        }

        private static Sprite MakeHelmetSprite(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float cx = w * 0.5f;
            float rx = w * 0.45f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = new Color(0, 0, 0, 0);
                    float dx = (x - cx) / rx;
                    float dy = (y - 0) / (float)h;
                    if (dx * dx + dy * dy < 1.0f && y >= 0)
                    {
                        bool edge = (dx * dx + dy * dy > 0.85f);
                        c = edge ? new Color(0.05f, 0.04f, 0.06f) : Color.white;
                    }
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.0f), 100);
        }

        private static Sprite MakeArmorSprite(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            // Solid white rect with rounded corners + dark trim
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool corner = (x < 2 && y < 2) || (x < 2 && y >= h - 2) || (x >= w - 2 && y < 2) || (x >= w - 2 && y >= h - 2);
                    bool edge = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                    if (corner) tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    else if (edge) tex.SetPixel(x, y, new Color(0.05f, 0.04f, 0.06f, 0.9f));
                    else tex.SetPixel(x, y, Color.white);
                }
            // V-neck cut at top
            for (int y = h - 6; y < h; y++)
            {
                int notch = (h - y);
                for (int x = w / 2 - notch; x <= w / 2 + notch; x++)
                    if (x >= 0 && x < w) tex.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100);
        }

        private static Sprite MakeRingSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    Color col;
                    if (d < 0.4f || d > 1.0f) col = new Color(0, 0, 0, 0);
                    else if (d > 0.85f) col = new Color(0.05f, 0.04f, 0.06f);
                    else col = Color.white;
                    tex.SetPixel(x, y, col);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }
    }
}
