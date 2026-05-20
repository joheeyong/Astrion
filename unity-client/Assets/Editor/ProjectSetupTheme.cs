using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Re-opens every scene authored by ProjectSetup and injects a UIThemeApplier
/// GameObject into it, so all the existing UI panels / buttons / inputs pick
/// up the parchment+brass sprites at runtime via color matching.
///
/// Sits as a partial of ProjectSetup so it can be called from Setup() once
/// after every Create*Scene method has run.
public partial class ProjectSetup
{
    /// Pre-game lobby scenes that the player explicitly asked us to leave
    /// alone. Their visual identity is intentional and predates the in-game
    /// fantasy theme push.
    private static readonly System.Collections.Generic.HashSet<string> ThemeExcludedScenes =
        new System.Collections.Generic.HashSet<string> {
            "LoginScene",
            "CharacterSelectScene",
            "CharacterCreateScene",
        };

    public static void InjectThemeIntoAllScenes()
    {
        foreach (var path in AllScenes)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (ThemeExcludedScenes.Contains(name))
            {
                Debug.Log($"[Astrion/Theme] skipping {name} (excluded)");
                continue;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            // Skip if a previous run already injected the applier.
            var existing = Object.FindObjectsOfType<Astrion.UI.UIThemeApplier>(true);
            if (existing != null && existing.Length > 0)
            {
                EditorSceneManager.SaveScene(scene);
                continue;
            }

            var go = new GameObject("UIThemeApplier");
            var applier = go.AddComponent<Astrion.UI.UIThemeApplier>();
            // In-game scenes already have a world/sky background, so don't
            // pile on another fullscreen layer. The applier just retints
            // the existing HUD widgets.
            applier.addBackdrop = false;

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Astrion/Theme] injected applier into {name}");
        }
    }
}
