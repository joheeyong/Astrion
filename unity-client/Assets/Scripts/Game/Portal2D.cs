using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astrion.Game
{
    /// Stores the originating scene name across a portal-driven scene load
    /// so the arrival portal in the new scene can position the player next to it.
    public static class PortalTransition
    {
        public static string FromScene = "";
    }

    [RequireComponent(typeof(Collider2D))]
    public class Portal2D : MonoBehaviour
    {
        [SerializeField] private string targetScene = "ForgottenWoodsScene";
        [SerializeField] private float cooldown = 1.5f;
        [SerializeField] private float arrivalOffset = 1.5f;

        private static float _lastTransitionTime = -100f;
        private SpriteRenderer _visualSr;
        private float _visualBaseAlpha = 1f;
        // Per-glyph mesh renderers so we can pulse them together with the swirl.
        private MeshRenderer[] _labelMrs;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            var visualT = transform.Find("Visual");
            if (visualT != null)
            {
                _visualSr = visualT.GetComponent<SpriteRenderer>();
                if (_visualSr != null) _visualBaseAlpha = _visualSr.color.a;
            }
            BuildLabel();
        }

        private void Update()
        {
            // Slow throb on the swirl + label so the portal reads as 'active'
            // even when no other animation is happening on screen.
            float pulse = 0.70f + 0.30f * Mathf.Sin(Time.time * 2.4f);
            if (_visualSr != null)
            {
                var c = _visualSr.color;
                c.a = _visualBaseAlpha * pulse;
                _visualSr.color = c;
            }
            if (_labelMrs != null)
            {
                // Modulate the TextMesh sharedMaterial color tint via property block.
                // Cheaper than allocating instances; we don't change material.
                Color tint = new Color(1f, 0.92f, 0.65f, pulse);
                foreach (var mr in _labelMrs)
                {
                    if (mr == null) continue;
                    var tm = mr.GetComponent<TextMesh>();
                    if (tm != null) tm.color = new Color(tm.color.r, tm.color.g, tm.color.b, pulse);
                }
            }
        }

        private void Start()
        {
            // Are we the arrival portal for the previous scene?
            if (!string.IsNullOrEmpty(PortalTransition.FromScene)
                && targetScene == PortalTransition.FromScene)
            {
                var player = Object.FindObjectOfType<PlayerController2D>();
                if (player != null)
                {
                    float sign = transform.position.x > 0f ? -1f : 1f;
                    var pos = transform.position + new Vector3(sign * arrivalOffset, 0.3f, 0f);
                    player.transform.position = pos;
                    _lastTransitionTime = Time.time;
                }
                PortalTransition.FromScene = "";
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController2D>() == null) return;
            if (Time.time - _lastTransitionTime < cooldown) return;
            _lastTransitionTime = Time.time;
            PortalTransition.FromScene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(targetScene);
        }

        // ── Label ─────────────────────────────────────────────────────────
        // Builds a TextMesh above the portal saying '→ Solaria' (or whichever
        // direction faces the destination), with a black outline for legibility
        // against any biome backdrop. Without this, the procedural swirl was
        // unlabelled and the player had to walk through to find out where it
        // led — bad UX for a 25-zone world.

        private void BuildLabel()
        {
            string display = ResolveDisplayName(targetScene);
            if (string.IsNullOrEmpty(display)) return;

            // Direction arrow based on portal's offset from the map's center.
            // 'right edge of the scene' portals are at x>0, 'left edge' at x<0.
            string arrow = transform.position.x > 0f ? "→" : "←";
            string text = transform.position.x > 0f ? display + "  " + arrow
                                                    : arrow + "  " + display;

            var labelRootGo = new GameObject("Label");
            labelRootGo.transform.SetParent(transform, false);
            labelRootGo.transform.localPosition = new Vector3(0, 1.9f, 0);
            labelRootGo.transform.localScale = Vector3.one * 0.04f;

            // Black outline: four offset copies behind the main glyph (same
            // trick the damage popup uses). Cheap, no shader, scales.
            Vector2[] outlineOffsets = {
                new Vector2( 0.04f,  0f), new Vector2(-0.04f, 0f),
                new Vector2( 0f,  0.04f), new Vector2( 0f, -0.04f),
            };
            var renderers = new System.Collections.Generic.List<MeshRenderer>();
            foreach (var off in outlineOffsets)
            {
                var olGo = new GameObject("Outline");
                olGo.transform.SetParent(labelRootGo.transform, false);
                olGo.transform.localPosition = new Vector3(off.x, off.y, 0.001f);
                var olTm = olGo.AddComponent<TextMesh>();
                ConfigText(olTm, text, Color.black);
                var olMr = olGo.GetComponent<MeshRenderer>();
                olMr.sortingOrder = 11;
                renderers.Add(olMr);
            }

            var mainTm = labelRootGo.AddComponent<TextMesh>();
            ConfigText(mainTm, text, new Color(1f, 0.92f, 0.65f));
            var mainMr = labelRootGo.GetComponent<MeshRenderer>();
            mainMr.sortingOrder = 12;
            renderers.Add(mainMr);

            _labelMrs = renderers.ToArray();
        }

        private static void ConfigText(TextMesh tm, string text, Color color)
        {
            tm.text = text;
            tm.fontSize = 48;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            // characterSize is the per-glyph world unit; with our 0.04 outer
            // scale this lands at roughly 0.40 world units tall — readable
            // from a few units of camera distance.
            tm.characterSize = 0.10f;
        }

        /// Resolves the human-readable name for the target scene. Goes through
        /// SceneZoneMap + WorldMapData first (the canonical lookup), and falls
        /// back to stripping 'Scene' off the end for anything not yet listed.
        private static string ResolveDisplayName(string scene)
        {
            if (string.IsNullOrEmpty(scene)) return "";
            try
            {
                var zoneId = Astrion.Network.SceneZoneMap.SceneToZone(scene);
                if (!string.IsNullOrEmpty(zoneId))
                {
                    int idx = Astrion.UI.WorldMapData.NodeIndex(zoneId);
                    if (idx >= 0) return Astrion.UI.WorldMapData.Nodes[idx].display;
                }
            }
            catch { /* WorldMapData may not be available in earlier compile stages */ }
            return scene.EndsWith("Scene") ? scene.Substring(0, scene.Length - 5) : scene;
        }
    }
}
