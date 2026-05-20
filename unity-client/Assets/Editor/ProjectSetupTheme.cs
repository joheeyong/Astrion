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
    public static void InjectThemeIntoAllScenes()
    {
        foreach (var path in AllScenes)
        {
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

            // Login / character menu / world map scenes look better with a
            // parchment backdrop behind the canvas. In-game scenes already
            // have a sky / world background, so we don't blanket those.
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            applier.addBackdrop = name == "LoginScene"
                              || name == "CharacterSelectScene"
                              || name == "CharacterCreateScene";

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Astrion/Theme] injected applier into {name}");
        }
    }
}
