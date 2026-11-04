using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central place for scene names and scene switching.
/// The public instance methods can be wired to UI events; the static
/// <see cref="Load"/> method is used by <see cref="SceneMenu"/> and other scripts.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    // Scene names as they appear in File > Build Profiles (Scene List).
    public const string MainMenuScene = "MainMenu";
    public const string ForestScene = "Forest";
    public const string BeachScene = "Beach";

    public void LoadScene(string sceneName) => Load(sceneName);
    public void LoadMainMenu() => Load(MainMenuScene);
    public void LoadForest() => Load(ForestScene);
    public void LoadBeach() => Load(BeachScene);
    public void RestartCurrentScene() => Restart();
    public void QuitGame() => Quit();

    public static void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("SceneLoader: no scene name given.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"SceneLoader: scene '{sceneName}' is not in the build scene list.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public static void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
