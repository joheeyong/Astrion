using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Re-opens every gameplay scene and injects an HUDStyleApplier GameObject
/// into it. The applier runs at Awake and adds the LoginPanel signature
/// (gold outline + accent lines + corner squares) to every panel that
/// already uses the medieval PanelBg color.
///
/// LoginScene / CharacterSelectScene / CharacterCreateScene are explicitly
/// excluded — their look is intentional and is the template the in-game
/// HUD is being aligned to.
///
/// Sits as a partial of ProjectSetup so it can be called from Setup() once
/// after every Create*Scene method has run.
public partial class ProjectSetup
{
    /// Pre-game lobby scenes that the player explicitly asked us to leave
    /// alone. Their visual identity is intentional — this is the look the
    /// in-game HUD is being aligned TO.
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
                Debug.Log($"[Astrion/HUDStyle] skipping {name} (excluded)");
                continue;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            // Skip if a previous run already injected the applier.
            var existing = Object.FindObjectsOfType<Astrion.UI.HUDStyleApplier>(true);
            if (existing != null && existing.Length > 0)
            {
                EditorSceneManager.SaveScene(scene);
                continue;
            }

            var go = new GameObject("HUDStyleApplier");
            go.AddComponent<Astrion.UI.HUDStyleApplier>();

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Astrion/HUDStyle] injected applier into {name}");
        }
    }
}
