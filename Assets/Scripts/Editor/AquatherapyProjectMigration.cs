using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time project clean-up: renames/moves scripts and scenes so file names match
/// their class names and the scenes are called MainMenu / Forest / Beach.
/// Uses AssetDatabase.MoveAsset, so GUIDs (and therefore all scene/prefab references)
/// are preserved. Runs automatically after scripts compile and is safe to run again
/// (menu: Aquatherapy > Run Project Migration). Nothing happens once everything is in place.
/// </summary>
[InitializeOnLoad]
internal static class AquatherapyProjectMigration
{
    private static readonly (string from, string to)[] Moves =
    {
        // Scripts: file name must match the class name.
        ("Assets/Scripts/SceneLoader.cs", "Assets/Scripts/Core/SceneLoader.cs"),
        ("Assets/Scripts/RestartButton.cs", "Assets/Scripts/UI/SceneMenu.cs"),
        ("Assets/Scripts/CollectedPrefabCounterUI.cs", "Assets/Scripts/UI/CollectibleCounterUI.cs"),
        ("Assets/Scripts/InteractionManager.cs", "Assets/Scripts/Interaction/InteractionManager.cs"),
        ("Assets/Scripts/Interactable.cs", "Assets/Scripts/Interaction/Interactable.cs"),
        ("Assets/Scripts/ObjectBehaviour.cs", "Assets/Scripts/Interaction/CollectibleWithEffect.cs"),
        ("Assets/Scripts/ObjectSpawner.cs", "Assets/Scripts/Collectibles/CollectibleSpawner.cs"),
        ("Assets/Scripts/ObjectGrounding.cs", "Assets/Scripts/Collectibles/ObjectGrounding.cs"),
        ("Assets/Scripts/TerrainFollowerScroller1.cs", "Assets/Scripts/Movement/TerrainFollowingScroller.cs"),
        ("Assets/Scripts/TerrainFollowerScroller.cs", "Assets/Scripts/Movement/DesktopTerrainFollowingScroller.cs"),
        ("Assets/Scripts/CameraScroller.cs", "Assets/Scripts/Movement/CameraScroller.cs"),

        // Scenes.
        ("Assets/Scenes/Main scene.unity", "Assets/Scenes/MainMenu.unity"),
        ("Assets/Scenes/demoScene_free 1.unity", "Assets/Scenes/Forest.unity"),
        ("Assets/Simple_Beach_Models/Scene/Simple_Beach_Demo.unity", "Assets/Scenes/Beach.unity"),

        // Old prototype scenes (not in the build any more).
        ("Assets/Scenes/Forrest.unity", "Assets/Scenes/Legacy/Forrest.unity"),
        ("Assets/Scenes/Sea.unity", "Assets/Scenes/Legacy/Sea.unity"),
        ("Assets/Scenes/SampleScene.unity", "Assets/Scenes/Legacy/SampleScene.unity"),
        ("Assets/XR test.unity", "Assets/Scenes/Legacy/XR test.unity"),
    };

    private static readonly string[] BuildScenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Forest.unity",
        "Assets/Scenes/Beach.unity",
    };

    static AquatherapyProjectMigration()
    {
        EditorApplication.delayCall += RunSilently;
    }

    private static void RunSilently() => Run(false);

    [MenuItem("Aquatherapy/Run Project Migration")]
    private static void RunFromMenu() => Run(true);

    private static void Run(bool verbose)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        int moved = 0;
        foreach ((string from, string to) in Moves)
        {
            if (!File.Exists(from))
                continue;

            if (File.Exists(to))
            {
                Debug.LogWarning($"[Aquatherapy] Not moving '{from}': '{to}' already exists.");
                continue;
            }

            EnsureFolder(Path.GetDirectoryName(to)?.Replace('\\', '/'));
            string error = AssetDatabase.MoveAsset(from, to);
            if (string.IsNullOrEmpty(error))
            {
                moved++;
                Debug.Log($"[Aquatherapy] Moved '{from}' -> '{to}'");
            }
            else
            {
                Debug.LogError($"[Aquatherapy] Could not move '{from}' -> '{to}': {error}");
            }
        }

        bool buildChanged = UpdateBuildScenes();

        if (moved > 0 || buildChanged)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Aquatherapy] Migration done ({moved} assets moved, build scenes {(buildChanged ? "updated" : "unchanged")}).");
        }
        else if (verbose)
        {
            Debug.Log("[Aquatherapy] Nothing to migrate, project is up to date.");
        }
    }

    private static bool UpdateBuildScenes()
    {
        if (!BuildScenes.All(File.Exists))
            return false;

        string[] current = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
        if (current.SequenceEqual(BuildScenes) && EditorBuildSettings.scenes.All(s => s.enabled))
            return false;

        EditorBuildSettings.scenes = BuildScenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        return true;
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
