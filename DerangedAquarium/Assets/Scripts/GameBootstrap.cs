using UnityEngine;

public static class GameBootstrap
{
    // This attribute forces the method to fire BEFORE your first frame even wakes up,
    // completely independent of whatever scene is currently active in the editor!
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ExecuteGlobalInitialization()
    {
        // Check if our central authority wallet system is already running in memory
        if (GlobalEconomyManager.Instance == null)
        {
            Debug.Log("<color=cyan>[Bootstrap]</color> Global economy channel not detected. Initializing master systems...");

            // Pull the master systems prefab out of the magic 'Resources' folder dynamically
            GameObject globalSystemsPrefab = Resources.Load<GameObject>("--- GLOBAL SYSTEM ---");

            if (globalSystemsPrefab != null)
            {
                GameObject spawnedGlobals = Object.Instantiate(globalSystemsPrefab);
                spawnedGlobals.name = "--- GLOBAL SYSTEM [BOOTSTRAPPED] ---";
                Debug.Log("<color=green>[Bootstrap] SUCCESS:</color> Persistent global architecture materialized safely.");
            }
            else
            {
                Debug.LogError("[Bootstrap] CRITICAL: Could not find a prefab named '--- GLOBAL SYSTEM ---' inside a 'Resources' folder! Please check your naming and path setups.");
            }
        }
    }
}