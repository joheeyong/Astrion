using UnityEngine;
using UnityEngine.UI;

namespace Astrion.UI
{
    /// Sweeps the scene at Awake and applies the Login/Character-Select panel
    /// signature to every HUD panel that uses the medieval PanelBg color:
    ///
    ///   - gold Outline component (matches LoginPanel's AccentGoldDim outline)
    ///   - top + bottom gold accent lines spanning the central 70%
    ///   - four small gold corner squares
    ///
    /// Idempotent — drops a hidden '_HudStyle_' marker child the first time
    /// it runs so re-entry on scene reload doesn't duplicate decorations.
    ///
    /// Detection is by color match (RGB delta < 0.02). Panels that already
    /// authored their own outline / decoration aren't touched again. Small
    /// elements (slot icons, badges) are skipped — the accent ring would
    /// look comically large on a 44×44 square.
    public class HUDStyleApplier : MonoBehaviour
    {
        // Reference colors — keep in sync with ProjectSetup palette.
        private static readonly Color PanelBgRef = new Color(0.10f, 0.08f, 0.06f, 0.92f);
        private static readonly Color SlotBgRef  = new Color(0.13f, 0.10f, 0.07f, 0.95f);
        private static readonly Color AccentGold    = new Color(0.85f, 0.65f, 0.22f, 1f);
        private static readonly Color AccentGoldDim = new Color(0.85f, 0.65f, 0.22f, 0.30f);
        private static readonly Color AccentGoldFaint = new Color(0.85f, 0.72f, 0.40f, 0.15f);

        // Minimum panel size to receive decorations. Anything smaller is a slot
        // / badge and should stay clean.
        private const float MinW = 120f;
        private const float MinH = 56f;

        private void Awake()
        {
            var images = FindObjectsOfType<Image>(true);
            foreach (var img in images)
            {
                if (img == null) continue;
                var rt = img.rectTransform;
                if (rt == null) continue;
                if (HasMarker(rt)) continue;

                Vector2 size = AbsSize(rt);
                if (size.x < MinW || size.y < MinH) continue;

                // Match either by panel-tint color OR by GameObject name suffix.
                // HUD panels often render PanelBg through a sprite while
                // Image.color stays white, so a color-only check misses them.
                // The naming convention in ProjectSetup is consistent:
                // CharPanel, MinimapPanel, ChatPanel, InventoryPanel,
                // SkillWindow, ItemTooltipPanel, etc.
                bool nameMatch  = IsHudPanelName(img.gameObject.name);
                bool colorMatch = IsPanelColor(img.color);
                if (!nameMatch && !colorMatch) continue;

                Apply(rt);
            }
        }

        private static bool IsHudPanelName(string n)
        {
            if (string.IsNullOrEmpty(n)) return false;
            return n.EndsWith("Panel")
                || n.EndsWith("Window")
                || n.EndsWith("Frame")
                || n.EndsWith("Tooltip");
        }

        private static bool IsPanelColor(Color c)
        {
            return ColorMatch(c, PanelBgRef) || ColorMatch(c, SlotBgRef);
        }

        private static bool ColorMatch(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.02f
                && Mathf.Abs(a.g - b.g) < 0.02f
                && Mathf.Abs(a.b - b.b) < 0.02f;
        }

        private static Vector2 AbsSize(RectTransform rt)
        {
            // sizeDelta misses anchor-stretched panels — use rect.size instead.
            return new Vector2(Mathf.Abs(rt.rect.size.x), Mathf.Abs(rt.rect.size.y));
        }

        private static bool HasMarker(RectTransform rt)
        {
            for (int i = 0; i < rt.childCount; i++)
                if (rt.GetChild(i).name == "_HudStyle_") return true;
            return false;
        }

        private static void Apply(RectTransform rt)
        {
            // Hidden marker — guards against double-apply if Awake runs twice
            // (e.g. additive scene load or a reset call later).
            var marker = new GameObject("_HudStyle_");
            marker.transform.SetParent(rt, false);
            marker.SetActive(false);

            // Outline — match LoginPanel exactly.
            if (rt.GetComponent<Outline>() == null)
            {
                var ol = rt.gameObject.AddComponent<Outline>();
                ol.effectColor = AccentGoldDim;
                ol.effectDistance = new Vector2(1, 1);
            }

            // Top accent line — gold, bright.
            AddAccentLine(rt, new Vector2(0.15f, 1f), new Vector2(0.85f, 1f),
                          AccentGold, 2);
            // Bottom accent line — gold, faint.
            AddAccentLine(rt, new Vector2(0.15f, 0f), new Vector2(0.85f, 0f),
                          AccentGoldFaint, 1);

            // Four corner squares.
            AddCornerDeco(rt, new Vector2(0, 1), new Vector2( 8, -8));
            AddCornerDeco(rt, new Vector2(1, 1), new Vector2(-8, -8));
            AddCornerDeco(rt, new Vector2(0, 0), new Vector2( 8,  8));
            AddCornerDeco(rt, new Vector2(1, 0), new Vector2(-8,  8));
        }

        private static void AddAccentLine(RectTransform parent,
                                          Vector2 anchorMin, Vector2 anchorMax,
                                          Color color, float thickness)
        {
            var go = new GameObject("AccentLine");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, thickness);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static void AddCornerDeco(RectTransform parent, Vector2 anchor, Vector2 offset)
        {
            var go = new GameObject("CornerDeco");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(6, 6);
            var img = go.AddComponent<Image>();
            img.color = AccentGold;
            img.raycastTarget = false;
        }
    }
}
