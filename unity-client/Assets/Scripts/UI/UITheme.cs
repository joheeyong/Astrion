using UnityEngine;

namespace Astrion.UI
{
    /// Runtime sprite cache for the parchment / brass fantasy theme.
    /// Sprites are generated procedurally on first access and reused across
    /// every scene — no asset import, no per-scene texture work.
    ///
    /// Reference colors mirror ProjectSetup's medieval palette so an
    /// UIThemeApplier can detect themed elements by matching Image.color.
    public static class UITheme
    {
        // ── Reference colors (match ProjectSetup palette) ────────────────
        public static readonly Color PanelBgRef    = new Color(0.10f, 0.08f, 0.06f, 0.92f);
        public static readonly Color PanelInnerRef = new Color(0.13f, 0.10f, 0.07f, 0.7f);
        public static readonly Color FieldBgRef    = new Color(0.08f, 0.06f, 0.04f, 0.95f);
        public static readonly Color BtnColorRef   = new Color(0.42f, 0.28f, 0.15f, 1f);

        // ── Theme accent colors ──────────────────────────────────────────
        public static readonly Color Parchment = new Color(0.86f, 0.74f, 0.52f);
        public static readonly Color Brass     = new Color(0.78f, 0.55f, 0.24f);
        public static readonly Color DarkLeather = new Color(0.22f, 0.13f, 0.07f);
        public static readonly Color CrimsonCloth = new Color(0.45f, 0.18f, 0.16f);
        public static readonly Color InkSepia    = new Color(0.32f, 0.18f, 0.08f);

        // ── Sprite cache ─────────────────────────────────────────────────
        private static Sprite _panelSpr, _brassBtnSpr, _fieldSpr, _backdropSpr, _scrollSpr;
        private static Sprite _panelInnerSpr;

        public static Sprite PanelParchment => _panelSpr ?? (_panelSpr = MakePanelParchment());
        public static Sprite PanelInnerLeather => _panelInnerSpr ?? (_panelInnerSpr = MakePanelInner());
        public static Sprite BrassButton    => _brassBtnSpr ?? (_brassBtnSpr = MakeBrassButton());
        public static Sprite BrassField     => _fieldSpr ?? (_fieldSpr = MakeBrassField());
        public static Sprite Backdrop       => _backdropSpr ?? (_backdropSpr = MakeBackdrop());
        public static Sprite ScrollBanner   => _scrollSpr ?? (_scrollSpr = MakeScroll());

        // ────────────────────────────────────────────────────────────────
        // Procedural texture generators (private)
        // ────────────────────────────────────────────────────────────────

        private static Sprite TexToSprite(Texture2D t)
        {
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height),
                                  new Vector2(0.5f, 0.5f), 100f);
        }

        /// Aged parchment with grain + warm vignetting.
        private static Sprite MakeBackdrop()
        {
            const int w = 512, h = 384;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color baseC = new Color(0.78f, 0.65f, 0.45f);
            Color edgeC = new Color(0.36f, 0.22f, 0.10f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float fx = (x - w * 0.5f) / (w * 0.5f);
                float fy = (y - h * 0.5f) / (h * 0.5f);
                float r = Mathf.Sqrt(fx * fx + fy * fy);
                float vig = Mathf.Clamp01((r - 0.40f) * 1.35f);
                float n = Mathf.PerlinNoise(x * 0.015f, y * 0.015f) * 0.5f
                        + Mathf.PerlinNoise(x * 0.06f, y * 0.06f)  * 0.5f;
                float grain = 0.85f + n * 0.20f;
                Color c = Color.Lerp(baseC, edgeC, vig) * grain;
                c.a = 1f;
                px[y * w + x] = c;
            }
            tex.SetPixels(px); tex.Apply();
            return TexToSprite(tex);
        }

        /// Parchment panel with thin brass frame; replaces solid PanelBg.
        private static Sprite MakePanelParchment()
        {
            const int w = 256, h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            int frame = 4;
            int inner = 6;
            Color paper = new Color(0.86f, 0.74f, 0.52f, 0.96f);
            Color edge  = new Color(0.50f, 0.35f, 0.18f, 0.96f);
            Color brass = new Color(0.78f, 0.55f, 0.24f, 1f);
            Color dark  = new Color(0.22f, 0.13f, 0.07f, 1f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                Color c;
                if (d < 2) c = dark;
                else if (d < frame) c = brass;
                else if (d < frame + inner) c = edge;
                else
                {
                    float n = Mathf.PerlinNoise(x * 0.025f, y * 0.025f);
                    c = paper * (0.92f + n * 0.12f);
                    c.a = paper.a;
                }
                px[y * w + x] = c;
            }
            tex.SetPixels(px); tex.Apply();
            return TexToSprite(tex);
        }

        /// Inner panel — slightly lighter parchment, no outer brass frame.
        private static Sprite MakePanelInner()
        {
            const int w = 128, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color paper = new Color(0.90f, 0.78f, 0.55f, 0.80f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                Color c;
                if (d < 1) c = new Color(0.45f, 0.30f, 0.15f, 0.8f);
                else
                {
                    float n = Mathf.PerlinNoise(x * 0.04f, y * 0.04f);
                    c = paper * (0.94f + n * 0.10f);
                    c.a = paper.a;
                }
                px[y * w + x] = c;
            }
            tex.SetPixels(px); tex.Apply();
            return TexToSprite(tex);
        }

        /// Brass-rimmed wood button.
        private static Sprite MakeBrassButton()
        {
            const int w = 128, h = 48;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color wood = new Color(0.42f, 0.28f, 0.15f);
            Color brass = new Color(0.78f, 0.55f, 0.24f);
            Color brassDark = new Color(0.42f, 0.30f, 0.12f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                Color c;
                if (d < 1) c = brassDark;
                else if (d < 3) c = brass;
                else
                {
                    // wood grain — vertical streaks
                    float n = Mathf.PerlinNoise(x * 0.06f, y * 0.4f);
                    c = wood * (0.85f + n * 0.25f);
                }
                // soft top sheen
                if (y > h - 4) c *= 1.15f;
                c.a = 1f;
                px[y * w + x] = c;
            }
            tex.SetPixels(px); tex.Apply();
            return TexToSprite(tex);
        }

        /// Dark inset field with brass border — for input boxes.
        private static Sprite MakeBrassField()
        {
            const int w = 128, h = 36;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color ink = new Color(0.16f, 0.10f, 0.05f);
            Color brass = new Color(0.62f, 0.42f, 0.18f);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                Color c;
                if (d < 2) c = brass;
                else
                {
                    float n = Mathf.PerlinNoise(x * 0.10f, y * 0.10f);
                    c = ink * (0.95f + n * 0.10f);
                }
                c.a = 0.96f;
                px[y * w + x] = c;
            }
            tex.SetPixels(px); tex.Apply();
            return TexToSprite(tex);
        }

        /// Title scroll with rolled ends (matches WorldMapUI's banner).
        private static Sprite MakeScroll()
        {
            const int w = 280, h = 56;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color paper = new Color(0.93f, 0.82f, 0.58f);
            Color rolled = new Color(0.55f, 0.36f, 0.18f);
            Color shadow = new Color(0.30f, 0.18f, 0.08f);
            int rollW = 28;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = paper;
                if (x < rollW || x > w - rollW - 1)
                {
                    int rx = x < rollW ? x : (w - 1 - x);
                    float v = rx / (float)rollW;
                    c = Color.Lerp(rolled, shadow, 1f - v);
                }
                if (y < 3) c *= 0.65f;
                if (y > h - 4) c *= 0.55f;
                c.a = 1f;
                px[y * w + x] = c;
            }
            tex.SetPixels(px); tex.Apply();
            return TexToSprite(tex);
        }
    }
}
