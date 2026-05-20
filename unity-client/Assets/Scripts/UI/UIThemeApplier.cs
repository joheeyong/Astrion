using UnityEngine;
using UnityEngine.UI;

namespace Astrion.UI
{
    /// Drop one of these on a scene-root GameObject. On Awake it sweeps every
    /// UI Image in the active scene and swaps in the parchment-and-brass
    /// theme sprites by matching the source Image's color to UITheme's
    /// reference palette.
    ///
    /// The match is intentionally tight (RGB delta < 0.02) so a designer-set
    /// color that happens to be near a theme reference doesn't get clobbered.
    /// Original alpha is preserved.
    public class UIThemeApplier : MonoBehaviour
    {
        [Tooltip("If true, also adds a fullscreen parchment backdrop behind every Canvas.")]
        public bool addBackdrop = false;

        private void Awake()
        {
            // FindObjectsOfType so we cover every Canvas in the scene, not just
            // children of this GameObject.
            var images = FindObjectsOfType<Image>(true);
            int themed = 0;
            foreach (var img in images)
            {
                if (img == null) continue;
                // Skip images that already have a non-default sprite — those
                // were authored intentionally (icons, logos, art).
                if (img.sprite != null) continue;

                if (ColorMatch(img.color, UITheme.PanelBgRef))
                {
                    img.sprite = UITheme.PanelParchment;
                    img.type = Image.Type.Simple;
                    PreserveAlpha(img, Color.white);
                    themed++;
                }
                else if (ColorMatch(img.color, UITheme.PanelInnerRef))
                {
                    img.sprite = UITheme.PanelInnerLeather;
                    img.type = Image.Type.Simple;
                    PreserveAlpha(img, Color.white);
                    themed++;
                }
                else if (ColorMatch(img.color, UITheme.BtnColorRef))
                {
                    img.sprite = UITheme.BrassButton;
                    img.type = Image.Type.Simple;
                    PreserveAlpha(img, Color.white);
                    themed++;
                }
                else if (ColorMatch(img.color, UITheme.FieldBgRef))
                {
                    img.sprite = UITheme.BrassField;
                    img.type = Image.Type.Simple;
                    PreserveAlpha(img, Color.white);
                    themed++;
                }
            }

            if (addBackdrop) AddBackdropBehindCanvas();
            // Optional: Debug.Log to confirm — leave commented out
            // Debug.Log($"[UIThemeApplier] themed {themed} images");
        }

        private static void PreserveAlpha(Image img, Color baseC)
        {
            float oldA = img.color.a;
            baseC.a = oldA;
            img.color = baseC;
        }

        private static bool ColorMatch(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.02f
                && Mathf.Abs(a.g - b.g) < 0.02f
                && Mathf.Abs(a.b - b.b) < 0.02f;
        }

        private void AddBackdropBehindCanvas()
        {
            var canvases = FindObjectsOfType<Canvas>(true);
            foreach (var cv in canvases)
            {
                // Only inject into Overlay / Camera canvases that look like full-screen menus.
                if (cv.renderMode != RenderMode.ScreenSpaceOverlay
                    && cv.renderMode != RenderMode.ScreenSpaceCamera) continue;

                // Skip canvases that already have a child named 'Backdrop'.
                bool already = false;
                for (int i = 0; i < cv.transform.childCount; i++)
                {
                    if (cv.transform.GetChild(i).name == "ThemeBackdrop") { already = true; break; }
                }
                if (already) continue;

                var bg = new GameObject("ThemeBackdrop");
                bg.transform.SetParent(cv.transform, false);
                bg.transform.SetAsFirstSibling();
                var rt = bg.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var img = bg.AddComponent<Image>();
                img.sprite = UITheme.Backdrop;
                img.color = new Color(1f, 1f, 1f, 1f);
            }
        }
    }
}
