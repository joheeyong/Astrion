using UnityEngine;

namespace Astrion.Game
{
    /// Drives the four equipment overlays under SpriteContainer (Weapon/Helmet/Armor/Ring)
    /// from PlayerStats. Subscribes to PlayerStats.OnChanged so equipping in the
    /// inventory updates the body visual immediately.
    public class PlayerEquipmentVisual : MonoBehaviour
    {
        // When false, doesn't auto-subscribe to PlayerStats — callers drive ApplyEquipment().
        // Used for remote players whose equipment comes from server packets.
        [SerializeField] private bool autoRefresh = true;

        private SpriteRenderer _weaponSR, _helmetSR, _armorSR, _ringSR;
        private GameObject _swordTrailGo;

        // Cached procedural sprites — shared across instances
        private static Sprite _daggerSprite;
        private static Sprite _swordSprite;
        private static Sprite _bowSprite;
        private static Sprite _staffSprite;
        private static Sprite _helmetSprite;
        private static Sprite _armorSprite;
        private static Sprite _ringSprite;

        private void Awake()
        {
            EnsureSpritesBuilt();
            // Weapon now lives under RightArm so it swings with the arm
            var weaponT = transform.Find("SpriteContainer/RightArm/WeaponVisual");
            _weaponSR = weaponT != null ? weaponT.GetComponent<SpriteRenderer>() : null;
            if (weaponT != null)
            {
                var trailT = weaponT.Find("SwordTrail");
                _swordTrailGo = trailT != null ? trailT.gameObject : null;
            }
            _helmetSR = transform.Find("SpriteContainer/HelmetVisual")?.GetComponent<SpriteRenderer>();
            _armorSR  = transform.Find("SpriteContainer/ArmorVisual")?.GetComponent<SpriteRenderer>();
            _ringSR   = transform.Find("SpriteContainer/RingVisual")?.GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (autoRefresh)
            {
                if (PlayerStats.Instance != null) PlayerStats.Instance.OnChanged += Refresh;
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (autoRefresh && PlayerStats.Instance != null) PlayerStats.Instance.OnChanged -= Refresh;
        }

        /// Set externally (since the SerializeField isn't accessible at runtime).
        public void SetAutoRefreshExternal(bool value) { autoRefresh = value; }

        /// External-driven equipment apply for remote players.
        /// Call from network packets when SPAWN_PLAYER / PLAYER_STATUS arrives.
        public void ApplyEquipment(string weaponId, string helmetId, string armorId, string ringId)
        {
            ApplyWeapon(weaponId);
            ApplyHelmet(helmetId);
            ApplyArmor(armorId);
            ApplyRing(ringId);
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
            bool isBow   = id == "star_bow" || id == "star_bow_bound";
            bool isSword = id.Contains("sword");
            bool isStaff = id.Contains("staff");
            _weaponSR.sprite = isStaff ? _staffSprite
                              : isBow   ? _bowSprite
                              : isSword ? _swordSprite
                                        : _daggerSprite;
            _weaponSR.color = def.iconColor;
            _weaponSR.gameObject.SetActive(true);

            // Sword trail only on actual blades
            if (_swordTrailGo != null) _swordTrailGo.SetActive(isSword);
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
            if (_swordSprite  == null) _swordSprite  = MakeSwordSprite(8, 56);
            if (_bowSprite    == null) _bowSprite    = MakeBowSprite(28, 56);
            if (_staffSprite  == null) _staffSprite  = MakeStaffSprite(10, 60);
            if (_helmetSprite == null) _helmetSprite = MakeHelmetSprite(20, 10);
            if (_armorSprite  == null) _armorSprite  = MakeArmorSprite(26, 30);
            if (_ringSprite   == null) _ringSprite   = MakeRingSprite(8);
        }

        private static Sprite MakeStaffSprite(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));

            int orbRadius = w / 2;
            int orbCy = h - orbRadius - 1;
            int shaftTop = orbCy - orbRadius;

            // Shaft (thin brown rod)
            int shaftX = w / 2;
            for (int y = 0; y < shaftTop; y++)
            {
                tex.SetPixel(shaftX - 1, y, new Color(0.42f, 0.28f, 0.14f));
                tex.SetPixel(shaftX,     y, Color.white);
                tex.SetPixel(shaftX + 1, y, new Color(0.42f, 0.28f, 0.14f));
            }
            // Pommel grip wrapping (a couple of darker bands)
            for (int y = 3; y < 10; y++)
                for (int x = shaftX - 1; x <= shaftX + 1; x++)
                    if (((y - 3) / 2) % 2 == 0)
                        tex.SetPixel(x, y, new Color(0.20f, 0.14f, 0.08f));

            // Orb at the top (filled circle, outlined)
            float cx = shaftX;
            float cy = orbCy;
            for (int y = orbCy - orbRadius; y < orbCy + orbRadius + 1; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (d <= orbRadius - 1)
                        tex.SetPixel(x, y, Color.white);
                    else if (d <= orbRadius)
                        tex.SetPixel(x, y, new Color(0.10f, 0.08f, 0.20f));
                }
            // Small inner glint
            tex.SetPixel((int)cx - 1, (int)cy + 1, new Color(1f, 1f, 1f));
            tex.SetPixel((int)cx,     (int)cy + 1, new Color(1f, 1f, 1f));

            tex.Apply();
            // Pivot near the grip so the staff rises out of the hand
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.10f), 100);
        }

        private static Sprite MakeSwordSprite(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
            int hilt = Mathf.RoundToInt(h * 0.15f);  // ~15% grip
            int guardThickness = 3;
            int tipLen = Mathf.Max(2, Mathf.RoundToInt(h * 0.08f)); // tapered tip

            // Blade (rectangle from above crossguard up to start of tip)
            int bladeStart = hilt + guardThickness;
            int bladeEnd = h - tipLen;
            for (int y = bladeStart; y < bladeEnd; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    bool edge = x == 1 || x == w - 2;
                    tex.SetPixel(x, y, edge ? new Color(0.05f, 0.04f, 0.06f) : Color.white);
                }
            // Tapered tip
            for (int y = bladeEnd; y < h; y++)
            {
                int taper = (y - bladeEnd) + 1;
                for (int x = 1 + taper; x < w - 1 - taper; x++)
                {
                    bool edge = (x == 1 + taper) || (x == w - 2 - taper) || (y == h - 1);
                    tex.SetPixel(x, y, edge ? new Color(0.05f, 0.04f, 0.06f) : Color.white);
                }
            }
            // Crossguard (wider than blade, dark brown)
            for (int t = 0; t < guardThickness; t++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, hilt + t, new Color(0.32f, 0.22f, 0.10f));
            // Grip (slimmer brown)
            for (int y = 0; y < hilt; y++)
                for (int x = 2; x < w - 2; x++)
                    tex.SetPixel(x, y, new Color(0.20f, 0.13f, 0.06f));
            // Pommel (round-ish bottom)
            for (int x = 2; x < w - 2; x++)
                tex.SetPixel(x, 0, new Color(0.35f, 0.24f, 0.12f));
            tex.Apply();
            // Pivot at the grip — sword extends "up" from the hand
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.10f), 100);
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
            Color clear = new Color(0, 0, 0, 0);
            Color wood = Color.white;                           // tinted at runtime
            Color woodDark = new Color(0.30f, 0.18f, 0.06f);    // outline
            Color bowstring = new Color(0.96f, 0.92f, 0.78f);   // pale string
            Color grip = new Color(0.20f, 0.12f, 0.05f);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, clear);

            float cy = h * 0.5f;
            float bend = w * 0.55f; // how far the curve reaches into +x
            int stringX = (int)(w - bend * 0.45f); // where the string runs vertically

            // Bow body — sweeping C curve from top tip to bottom tip
            for (int y = 1; y < h - 1; y++)
            {
                float t = (y - 1) / (float)(h - 2);          // 0..1
                float angle = (t - 0.5f) * Mathf.PI * 0.95f; // -π/2..π/2 with a tiny squeeze
                int xOuter = Mathf.RoundToInt((w - 1) - Mathf.Cos(angle) * bend);
                if (xOuter < 0 || xOuter >= w) continue;
                // 3-pixel thick wooden limb (highlight + body + outline)
                if (xOuter - 1 >= 0) tex.SetPixel(xOuter - 1, y, wood);
                tex.SetPixel(xOuter,     y, wood);
                if (xOuter + 1 < w)  tex.SetPixel(xOuter + 1, y, woodDark);
            }

            // Bowstring — vertical line between the two tips
            for (int y = 2; y < h - 2; y++)
                tex.SetPixel(Mathf.Clamp(stringX, 0, w - 1), y, bowstring);

            // Grip wrap — small dark band where the hand holds it
            int gripCy = h / 2;
            int gripWoodX = Mathf.RoundToInt(w - 1 - bend);
            for (int dy = -2; dy <= 2; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int gx = Mathf.Clamp(gripWoodX + dx, 0, w - 1);
                    int gy = Mathf.Clamp(gripCy + dy, 0, h - 1);
                    tex.SetPixel(gx, gy, grip);
                }

            tex.Apply();
            // Pivot at the grip (left side, middle) so the bow sits in the hand
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.05f, 0.5f), 100);
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
