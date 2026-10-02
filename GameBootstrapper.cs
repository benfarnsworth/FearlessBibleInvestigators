using UnityEngine;

public static class GameBootstrapper
{
    private const string CORE_SYSTEMS_PREFAB_PATH = "CoreSystems"; // Loaded from Assets/Resources/CoreSystems.prefab

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ExecuteBootstrapper()
    {
        // 1. Check if an instance already exists (e.g., carried over from a previous scene load)
        if (CoreSystems.Instance != null) return;

        // 2. Try loading the CoreSystems prefab from the Resources folder
        GameObject corePrefab = Resources.Load<GameObject>(CORE_SYSTEMS_PREFAB_PATH);

        if (corePrefab == null)
        {
            Debug.LogError($"[BOOTSTRAPPER] Failed to load CoreSystems prefab at 'Assets/Resources/{CORE_SYSTEMS_PREFAB_PATH}.prefab'. Make sure the file exists!");
            return;
        }

        // 3. Instantiate the system managers root object
        GameObject coreInstance = Object.Instantiate(corePrefab);
        coreInstance.name = "[CoreSystems]";

        // 4. Ensure it persists across all subsequent scene loads
        Object.DontDestroyOnLoad(coreInstance);

        Debug.Log("[BOOTSTRAPPER] CoreSystems initialized automatically.");
    }
}