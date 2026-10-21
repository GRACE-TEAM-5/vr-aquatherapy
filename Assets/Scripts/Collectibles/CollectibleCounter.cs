using System;
using UnityEngine.SceneManagement;
using UnityEngine;

/// <summary>
/// Counts collected objects. The count resets whenever a new scene is loaded,
/// so every environment (forest, beach) starts at zero.
/// </summary>
public static class CollectibleCounter
{
    public static event Action<int> CountChanged;

    public static int Count { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Count = 0;
        CountChanged = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
            Reset();
    }

    public static void Increment()
    {
        Count++;
        CountChanged?.Invoke(Count);
    }

    public static void Reset()
    {
        Count = 0;
        CountChanged?.Invoke(Count);
    }
}
