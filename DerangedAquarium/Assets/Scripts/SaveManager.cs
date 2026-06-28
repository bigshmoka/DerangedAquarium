using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Reflection; // Cached reflection types

// ===================================================================
// --- SERIALIZABLE RAW DATA STRUCTURES ---
// ===================================================================
[System.Serializable]
public class GameSaveData
{
    public int walletBalance = 100;
    public List<PlacedItemDataWrapper> placed3DItems = new List<PlacedItemDataWrapper>();
    public List<AquariumItemDataWrapper> placed2DItems = new List<AquariumItemDataWrapper>();
    public List<float> algaeNodeLevels = new List<float>();
    public int questChainIndex = 0;
    public int questCurrentCount = 0;
}

[System.Serializable]
public class PlacedItemDataWrapper
{
    public string prefabResourceName;
    public int originalCost;
    public Vector3 position;
    public Quaternion rotation;
}

[System.Serializable]
public class AquariumItemDataWrapper
{
    public string prefabResourceName;
    public Vector3 position;
    public Vector3 localScale; 
    public float fishScaleModifier;
    public Vector3 fishBaseScale;
    public float fishFacingSign = 1f; 
    public float fishHunger = 0f;
    public bool fishIsFull = false;
    public float fishFullnessTimer = 0f;
}

// ===================================================================
// --- MASTER SAVE SYSTEM ENGINE ---
// ===================================================================
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string saveFilePath;

    // --- CACHED REFLECTION FIELDS ---
    // Storing these lookups in memory at startup completely eliminates runtime save stutters
    private FieldInfo fishHungerField;
    private FieldInfo fishIsFullField;
    private FieldInfo fishFullnessTimerField;
    private FieldInfo questChainIndexField;
    private MethodInfo questLoadMethod;

    // --- CACHED HIERARCHY REFERENCES ---
    private GameObject cached3DItemContainer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        saveFilePath = Path.Combine(Application.persistentDataPath, "storefront_save.json");

        InitializeReflectionCache();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) SaveGame();
        if (Input.GetKeyDown(KeyCode.F9)) LoadGame();
    }

    /// <summary>
    /// Runs exactly once on boot. Converts heavy text-based engine searches into fast memory pointers.
    /// </summary>
    private void InitializeReflectionCache()
    {
        // Cache NaturalFishAI private fields
        fishHungerField = typeof(NaturalFishAI).GetField("currentHunger", BindingFlags.Instance | BindingFlags.NonPublic);
        fishIsFullField = typeof(NaturalFishAI).GetField("isFull", BindingFlags.Instance | BindingFlags.NonPublic);
        fishFullnessTimerField = typeof(NaturalFishAI).GetField("fullnessTimer", BindingFlags.Instance | BindingFlags.NonPublic);

        // Cache QuestManager private fields and methods
        questChainIndexField = typeof(QuestManager).GetField("currentChainIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        questLoadMethod = typeof(QuestManager).GetMethod("LoadCurrentQuestFromChain", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private GameObject Get3DItemContainer()
    {
        if (cached3DItemContainer == null)
        {
            cached3DItemContainer = GameObject.Find("--- PLACED 3D ITEMS ---");
        }
        return cached3DItemContainer;
    }

    public void SaveGame()
    {
        if (GlobalEconomyManager.Instance == null) return;

        GameSaveData dataToSave = new GameSaveData { walletBalance = GlobalEconomyManager.Instance.GetBalance() };

        // 3D Item Serialization
        GameObject itemContainer = Get3DItemContainer();
        if (itemContainer != null)
        {
            foreach (Transform child in itemContainer.transform)
            {
                PlacedItemData itemProfile = child.GetComponent<PlacedItemData>();
                if (itemProfile != null)
                {
                    dataToSave.placed3DItems.Add(new PlacedItemDataWrapper
                    {
                        prefabResourceName = child.name.Replace("_Placed", "").Trim(),
                        originalCost = itemProfile.originalCost,
                        position = child.position,
                        rotation = child.rotation
                    });
                }
            }
        }

        // 2D Aquarium Serialization
        AquariumManager tankManager = FindFirstObjectByType<AquariumManager>();
        if (tankManager != null)
        {
            foreach (Transform child in tankManager.transform)
            {
                bool isFish = child.GetComponent<NaturalFishAI>() != null || child.name.Contains("Fish");
                bool isSnail = child.GetComponent<SnailAI>() != null || child.name.Contains("Snail");
                
                // FIXED: Reverted back to string lookup check since TankDecoration doesn't exist as a C# class symbol
                bool isDecor = child.name.Contains("_Placed") || child.GetComponent("TankDecoration") != null;
                bool isUtilityItem = child.name.Contains("Feeder") || child.name.Contains("Item") || child.name.Contains("Machine");

                if (isFish || isSnail || isDecor || isUtilityItem)
                {
                    AquariumItemDataWrapper aqWrapper = new AquariumItemDataWrapper
                    {
                        prefabResourceName = child.name.Replace("(Clone)", "").Replace("_Placed", "").Trim(),
                        position = child.position,
                        localScale = child.localScale
                    };

                    NaturalFishAI fishAI = child.GetComponent<NaturalFishAI>();
                    if (fishAI != null)
                    {
                        aqWrapper.fishScaleModifier = fishAI.currentScaleModifier;
                        aqWrapper.fishBaseScale = fishAI.baseScale;
                        aqWrapper.fishFacingSign = fishAI.facingDirectionSign;

                        // Using pre-cached fields directly avoids heavy CPU loops
                        if (fishHungerField != null) aqWrapper.fishHunger = (float)fishHungerField.GetValue(fishAI);
                        if (fishIsFullField != null) aqWrapper.fishIsFull = (bool)fishIsFullField.GetValue(fishAI);
                        if (fishFullnessTimerField != null) aqWrapper.fishFullnessTimer = (float)fishFullnessTimerField.GetValue(fishAI);
                    }

                    SnailAI snailAI = child.GetComponent<SnailAI>();
                    if (snailAI != null) aqWrapper.fishBaseScale = snailAI.originalScale;

                    dataToSave.placed2DItems.Add(aqWrapper);
                }
            }
        }

        // Algae Serialization
        AlgaeManager algaeManager = FindFirstObjectByType<AlgaeManager>();
        if (algaeManager != null && algaeManager.algaeNodes != null)
        {
            foreach (AlgaeNode node in algaeManager.algaeNodes)
            {
                dataToSave.algaeNodeLevels.Add(node != null ? node.currentAlgaeLevel : 0f);
            }
        }

        // Quest Serialization
        if (QuestManager.Instance != null)
        {
            if (questChainIndexField != null) dataToSave.questChainIndex = (int)questChainIndexField.GetValue(QuestManager.Instance);
            if (QuestManager.Instance.activeQuests != null && QuestManager.Instance.activeQuests.Count > 0)
            {
                dataToSave.questCurrentCount = QuestManager.Instance.activeQuests[0].currentCount;
            }
        }

        File.WriteAllText(saveFilePath, JsonUtility.ToJson(dataToSave, true));
        Debug.Log($"<color=green>[Save System]</color> Optimized Save Completed.");
    }

    public void LoadGame()
    {
        if (!File.Exists(saveFilePath)) return;
        if (GlobalEconomyManager.Instance == null) return;

        GameSaveData loadedData = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(saveFilePath));

        // Wallet Balance Synchronization
        int currentWalletBalance = GlobalEconomyManager.Instance.GetBalance();
        int balanceDiff = loadedData.walletBalance - currentWalletBalance;
        if (balanceDiff > 0) GlobalEconomyManager.Instance.AddMoney(balanceDiff);
        else if (balanceDiff < 0) GlobalEconomyManager.Instance.DeductMoney(Mathf.Abs(balanceDiff));

        // Clear 3D Room
        GameObject itemContainer = Get3DItemContainer();
        if (itemContainer != null)
        {
            for (int i = itemContainer.transform.childCount - 1; i >= 0; i--) Destroy(itemContainer.transform.GetChild(i).gameObject);
        }

        // Spawn 3D Room
        foreach (PlacedItemDataWrapper savedItem in loadedData.placed3DItems)
        {
            GameObject rawPrefab = Resources.Load<GameObject>($"StorefrontPrefabs/{savedItem.prefabResourceName}");
            if (rawPrefab != null)
            {
                GameObject loadedInstance = Instantiate(rawPrefab, savedItem.position, savedItem.rotation, itemContainer != null ? itemContainer.transform : null);
                loadedInstance.name = savedItem.prefabResourceName + "_Placed";
                loadedInstance.AddComponent<PlacedItemData>().originalCost = savedItem.originalCost;
            }
        }

        // Clear & Re-populate 2D Aquarium
        AquariumManager tankManager = FindFirstObjectByType<AquariumManager>();
        if (tankManager != null)
        {
            foreach (Transform child in tankManager.transform)
            {
                // FIXED: Reverted back to safe string component lookup check for TankDecoration
                if (child.GetComponent<NaturalFishAI>() != null || child.name.Contains("Fish") ||
                    child.GetComponent<SnailAI>() != null || child.name.Contains("Snail") ||
                    child.name.Contains("_Placed") || child.GetComponent("TankDecoration") != null ||
                    child.name.Contains("Feeder") || child.name.Contains("Item") || child.name.Contains("Machine"))
                {
                    Destroy(child.gameObject);
                }
            }

            foreach (AquariumItemDataWrapper saved2DItem in loadedData.placed2DItems)
            {
                GameObject raw2DPrefab = Resources.Load<GameObject>($"AquariumPrefabs/{saved2DItem.prefabResourceName}");
                if (raw2DPrefab != null)
                {
                    GameObject loaded2DInstance = Instantiate(raw2DPrefab, saved2DItem.position, Quaternion.identity, tankManager.transform);
                    loaded2DInstance.name = saved2DItem.prefabResourceName;

                    NaturalFishAI fishAI = loaded2DInstance.GetComponent<NaturalFishAI>();
                    SnailAI snailAI = loaded2DInstance.GetComponent<SnailAI>();

                    if (fishAI != null)
                    {
                        fishAI.baseScale = saved2DItem.fishBaseScale != Vector3.zero ? saved2DItem.fishBaseScale : new Vector3(0.4f, 0.4f, 1f);
                        fishAI.currentScaleModifier = saved2DItem.fishScaleModifier > 0f ? saved2DItem.fishScaleModifier : fishAI.startingScale;
                        fishAI.facingDirectionSign = saved2DItem.fishFacingSign != 0f ? saved2DItem.fishFacingSign : 1f;
                        loaded2DInstance.transform.localScale = saved2DItem.localScale;

                        // Optimized Pointers
                        if (fishHungerField != null) fishHungerField.SetValue(fishAI, saved2DItem.fishHunger);
                        if (fishIsFullField != null) fishIsFullField.SetValue(fishAI, saved2DItem.fishIsFull);
                        if (fishFullnessTimerField != null) fishFullnessTimerField.SetValue(fishAI, saved2DItem.fishFullnessTimer);
                    }
                    else if (snailAI != null)
                    {
                        snailAI.originalScale = saved2DItem.fishBaseScale != Vector3.zero ? saved2DItem.fishBaseScale : new Vector3(0.4f, 0.4f, 1f);
                        loaded2DInstance.transform.localScale = saved2DItem.localScale;
                    }
                    else
                    {
                        StartCoroutine(ApplyDelayedScale(loaded2DInstance, saved2DItem.localScale));
                    }

                    if (!tankManager.isTankVisible)
                    {
                        foreach (Renderer rend in loaded2DInstance.GetComponentsInChildren<Renderer>()) rend.enabled = false;
                    }
                }
            }
        }

        // Restore Algae Cleanness
        AlgaeManager algaeManager = FindFirstObjectByType<AlgaeManager>();
        if (algaeManager != null && algaeManager.algaeNodes != null && loadedData.algaeNodeLevels != null)
        {
            int nodeCount = Mathf.Min(algaeManager.algaeNodes.Length, loadedData.algaeNodeLevels.Count);
            for (int i = 0; i < nodeCount; i++)
            {
                if (algaeManager.algaeNodes[i] != null) algaeManager.algaeNodes[i].InitializeAlgaeLevel(loadedData.algaeNodeLevels[i]);
            }
        }

        // --- RESTORE QUEST STATE WITH AUTO-ROLLOVER INTERCEPT PROTECTION ---
        if (QuestManager.Instance != null)
        {
            int workingChainIndex = loadedData.questChainIndex;
            int workingCount = loadedData.questCurrentCount;

            // 1. Initially set the index back down to the saved setting
            if (questChainIndexField != null) questChainIndexField.SetValue(QuestManager.Instance, workingChainIndex);
            
            // 2. Load the base configurations of that specific quest into activeQuests arrays
            if (questLoadMethod != null) questLoadMethod.Invoke(QuestManager.Instance, null);

            // 3. Evaluate if we are trapped inside the boundary limbo glitch window!
            if (QuestManager.Instance.activeQuests != null && QuestManager.Instance.activeQuests.Count > 0)
            {
                Quest loadedQuest = QuestManager.Instance.activeQuests[0];

                if (workingCount >= loadedQuest.targetCount)
                {
                    // INTERCEPTED: The save happened during the objective completion frame!
                    // Forcefully push the index to the next level configuration automatically.
                    workingChainIndex++;
                    
                    if (questChainIndexField != null) questChainIndexField.SetValue(QuestManager.Instance, workingChainIndex);
                    if (questLoadMethod != null) questLoadMethod.Invoke(QuestManager.Instance, null);

                    // Reset sub-task values to 0 for the fresh next assignment
                    if (QuestManager.Instance.activeQuests != null && QuestManager.Instance.activeQuests.Count > 0)
                    {
                        QuestManager.Instance.activeQuests[0].currentCount = 0;
                    }
                    
                    Debug.Log("<color=yellow>[Save Intercept]</color> Completed objective layout detected upon loading! Pushed chapter index forward smoothly.");
                }
                else
                {
                    // Normal state parameters: Apply the objective sub-count as it was
                    loadedQuest.currentCount = workingCount;
                }
            }
        }

        Debug.Log("<color=cyan>[Save System]</color> Optimized Load Completed Successfully.");
    }

    private IEnumerator ApplyDelayedScale(GameObject target, Vector3 savedScale)
    {
        yield return new WaitForEndOfFrame();
        if (target != null) target.transform.localScale = savedScale;
    }
}