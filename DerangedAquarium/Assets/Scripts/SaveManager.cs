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
    
    // --- STRUCTURAL CHANGE: ALGAE LEVELS ARE NOW LINKED TO UNIQUE TANK IDENTIFIERS ---
    public List<TankAlgaeSaveData> tankAlgaeRecords = new List<TankAlgaeSaveData>();
    
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
    
    // --- NEW MULTI-TANK TRACKING FIELD ---
    public string assignedTankID = "StarterTank";
}

[System.Serializable]
public class TankAlgaeSaveData
{
    public string tankID;
    public List<float> nodeLevels = new List<float>();
}

// ===================================================================
// --- MASTER SAVE SYSTEM ENGINE ---
// ===================================================================
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string saveFilePath;

    private FieldInfo fishHungerField;
    private FieldInfo fishIsFullField;
    private FieldInfo fishFullnessTimerField;
    private FieldInfo questChainIndexField;
    private MethodInfo questLoadMethod;

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

    private void InitializeReflectionCache()
    {
        fishHungerField = typeof(NaturalFishAI).GetField("currentHunger", BindingFlags.Instance | BindingFlags.NonPublic);
        fishIsFullField = typeof(NaturalFishAI).GetField("isFull", BindingFlags.Instance | BindingFlags.NonPublic);
        fishFullnessTimerField = typeof(NaturalFishAI).GetField("fullnessTimer", BindingFlags.Instance | BindingFlags.NonPublic);

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

        // 1. Serialize 3D placed shop furniture elements
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

        // ===================================================================
        // --- FIXED: INACTIVE SPECTRUM SAVE SCAN ---
        // Forces the serialization script to check dormant, inactive background 
        // objects so expand-zone tycoon setups save cleanly!
        // ===================================================================
        AquariumManager[] allTanks = FindObjectsByType<AquariumManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AquariumManager tankManager in allTanks)
        {
            if (tankManager == null) continue;

            string currentTankID = tankManager.tankID;

            // Serialize creatures and local upgrades inside this explicit container
            foreach (Transform child in tankManager.transform)
            {
                bool isFish = child.GetComponent<NaturalFishAI>() != null || child.name.Contains("Fish");
                bool isSnail = child.GetComponent<SnailAI>() != null || child.name.Contains("Snail");
                bool isDecor = child.name.Contains("_Placed") || child.GetComponent("TankDecoration") != null;
                bool isUtilityItem = child.name.Contains("Feeder") || child.name.Contains("Item") || child.name.Contains("Machine");

                if (isFish || isSnail || isDecor || isUtilityItem)
                {
                    AquariumItemDataWrapper aqWrapper = new AquariumItemDataWrapper
                    {
                        prefabResourceName = child.name.Replace("(Clone)", "").Replace("_Placed", "").Trim(),
                        position = child.position,
                        localScale = child.localScale,
                        assignedTankID = currentTankID // Stamp item with its matching home tank ID!
                    };

                    NaturalFishAI fishAI = child.GetComponent<NaturalFishAI>();
                    if (fishAI != null)
                    {
                        aqWrapper.fishScaleModifier = fishAI.currentScaleModifier;
                        aqWrapper.fishBaseScale = fishAI.baseScale;
                        aqWrapper.fishFacingSign = fishAI.facingDirectionSign;

                        if (fishHungerField != null) aqWrapper.fishHunger = (float)fishHungerField.GetValue(fishAI);
                        if (fishIsFullField != null) aqWrapper.fishIsFull = (bool)fishIsFullField.GetValue(fishAI);
                        if (fishFullnessTimerField != null) aqWrapper.fishFullnessTimer = (float)fishFullnessTimerField.GetValue(fishAI);
                    }

                    SnailAI snailAI = child.GetComponent<SnailAI>();
                    if (snailAI != null) aqWrapper.fishBaseScale = snailAI.originalScale;

                    dataToSave.placed2DItems.Add(aqWrapper);
                }
            }

            // Serialize Algae levels for this specific tank container structure
            AlgaeManager algaeManager = tankManager.algaeManager;
            if (algaeManager == null) algaeManager = tankManager.GetComponentInChildren<AlgaeManager>();

            if (algaeManager != null && algaeManager.algaeNodes != null)
            {
                TankAlgaeSaveData algaeRecord = new TankAlgaeSaveData { tankID = currentTankID };
                foreach (AlgaeNode node in algaeManager.algaeNodes)
                {
                    algaeRecord.nodeLevels.Add(node != null ? node.currentAlgaeLevel : 0f);
                }
                dataToSave.tankAlgaeRecords.Add(algaeRecord);
            }
        }

        // 3. Serialize Quest State
        if (QuestManager.Instance != null)
        {
            if (questChainIndexField != null) dataToSave.questChainIndex = (int)questChainIndexField.GetValue(QuestManager.Instance);
            if (QuestManager.Instance.activeQuests != null && QuestManager.Instance.activeQuests.Count > 0)
            {
                dataToSave.questCurrentCount = QuestManager.Instance.activeQuests[0].currentCount;
            }
        }

        File.WriteAllText(saveFilePath, JsonUtility.ToJson(dataToSave, true));
        Debug.Log($"<color=green>[Save System]</color> Multi-Tank Structural Groundwork Saved Successfully.");
    }

    public void LoadGame()
    {
        if (!File.Exists(saveFilePath)) return;
        if (GlobalEconomyManager.Instance == null) return;

        GameSaveData loadedData = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(saveFilePath));

        // Sync Wallet
        int currentWalletBalance = GlobalEconomyManager.Instance.GetBalance();
        int balanceDiff = loadedData.walletBalance - currentWalletBalance;
        if (balanceDiff > 0) GlobalEconomyManager.Instance.AddMoney(balanceDiff);
        else if (balanceDiff < 0) GlobalEconomyManager.Instance.DeductMoney(Mathf.Abs(balanceDiff));

        // Rebuild 3D Room Items
        GameObject itemContainer = Get3DItemContainer();
        if (itemContainer != null)
        {
            for (int i = itemContainer.transform.childCount - 1; i >= 0; i--) Destroy(itemContainer.transform.GetChild(i).gameObject);
        }

        foreach (PlacedItemDataWrapper savedItem in loadedData.placed3DItems)
        {
            GameObject rawPrefab = Resources.Load<GameObject>($"StorefrontPrefabs/{savedItem.prefabResourceName}");
            if (rawPrefab != null)
            {
                GameObject loadedInstance = Instantiate(rawPrefab, savedItem.position, savedItem.rotation, itemContainer != null ? itemContainer.transform : null);
                loadedInstance.name = savedItem.prefabResourceName + "_Placed";
                loadedInstance.AddComponent<PlacedItemData>().originalCost = savedItem.originalCost;

                // Wire up loaded 3D prefab shells to trigger their template scene mapping chains
                TankInteraction3D loadedTankComp = loadedInstance.GetComponentInChildren<TankInteraction3D>();
                if (loadedTankComp != null)
                {
                    loadedTankComp.enabled = true;
                    if (loadedTankComp.tankID != "Unassigned_Tank")
                    {
                        loadedTankComp.InitializeRuntimeTank(loadedTankComp.tankID);
                    }
                }
            }
        }

        // ===================================================================
        // --- FIXED: INACTIVE ALL-TANK SCANNING AND SPAWN SHIELDING ---
        // 1. Scans ALL active and inactive tanks in your memory architecture.
        // 2. Activates 'skipDefaultSpawn = true' on every single one.
        // This ensures waking up a tank NEVER drops duplicate default fish!
        // ===================================================================
        AquariumManager[] allTanks = FindObjectsByType<AquariumManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Dictionary<string, AquariumManager> tankMap = new Dictionary<string, AquariumManager>();
        
        foreach (AquariumManager tank in allTanks)
        {
            if (tank != null)
            {
                tank.skipDefaultSpawn = true; // Block the starter fish trigger!
                if (!tankMap.ContainsKey(tank.tankID)) tankMap.Add(tank.tankID, tank);
            }
        }

        // Clear existing creatures across ALL active and inactive tanks
        foreach (AquariumManager tank in allTanks)
        {
            if (tank == null) continue;
            foreach (Transform child in tank.transform)
            {
                if (child.GetComponent<NaturalFishAI>() != null || child.name.Contains("Fish") ||
                    child.GetComponent<SnailAI>() != null || child.name.Contains("Snail") ||
                    child.name.Contains("_Placed") || child.GetComponent("TankDecoration") != null ||
                    child.name.Contains("Feeder") || child.name.Contains("Item") || child.name.Contains("Machine"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        // Re-populate 2D Items back into their specific target home tank modules!
        foreach (AquariumItemDataWrapper saved2DItem in loadedData.placed2DItems)
        {
            string targetTankID = string.IsNullOrEmpty(saved2DItem.assignedTankID) ? "StarterTank" : saved2DItem.assignedTankID;
            
            if (tankMap.TryGetValue(targetTankID, out AquariumManager targetTankManager))
            {
                // Wake up the matching container layout object if it was sleeping
                if (!targetTankManager.gameObject.activeSelf)
                {
                    targetTankManager.gameObject.SetActive(true);
                }

                GameObject raw2DPrefab = Resources.Load<GameObject>($"AquariumPrefabs/{saved2DItem.prefabResourceName}");
                if (raw2DPrefab != null)
                {
                    GameObject loaded2DInstance = Instantiate(raw2DPrefab, saved2DItem.position, Quaternion.identity, targetTankManager.transform);
                    loaded2DInstance.name = saved2DItem.prefabResourceName;

                    NaturalFishAI fishAI = loaded2DInstance.GetComponent<NaturalFishAI>();
                    SnailAI snailAI = loaded2DInstance.GetComponent<SnailAI>();

                    if (fishAI != null)
                    {
                        fishAI.baseScale = saved2DItem.fishBaseScale != Vector3.zero ? saved2DItem.fishBaseScale : new Vector3(0.4f, 0.4f, 1f);
                        fishAI.currentScaleModifier = saved2DItem.fishScaleModifier > 0f ? saved2DItem.fishScaleModifier : fishAI.startingScale;
                        fishAI.facingDirectionSign = saved2DItem.fishFacingSign != 0f ? saved2DItem.fishFacingSign : 1f;
                        loaded2DInstance.transform.localScale = saved2DItem.localScale;

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

                    // Dynamic Visibility Isolation Mask
                    if (!targetTankManager.isTankVisible)
                    {
                        foreach (Renderer rend in loaded2DInstance.GetComponentsInChildren<Renderer>()) rend.enabled = false;
                    }
                }
            }
        }

        // Restore Algae records across matching target modules
        if (loadedData.tankAlgaeRecords != null)
        {
            foreach (TankAlgaeSaveData record in loadedData.tankAlgaeRecords)
            {
                if (tankMap.TryGetValue(record.tankID, out AquariumManager targetTank))
                {
                    AlgaeManager algaeManager = targetTank.algaeManager ?? targetTank.GetComponentInChildren<AlgaeManager>();
                    if (algaeManager != null && algaeManager.algaeNodes != null)
                    {
                        int nodeCount = Mathf.Min(algaeManager.algaeNodes.Length, record.nodeLevels.Count);
                        for (int i = 0; i < nodeCount; i++)
                        {
                            if (algaeManager.algaeNodes[i] != null) algaeManager.algaeNodes[i].InitializeAlgaeLevel(record.nodeLevels[i]);
                        }
                    }
                }
            }
        }

        // Restore Quests with Rollover protection intact
        if (QuestManager.Instance != null)
        {
            int workingChainIndex = loadedData.questChainIndex;
            int workingCount = loadedData.questCurrentCount;

            if (questChainIndexField != null) questChainIndexField.SetValue(QuestManager.Instance, workingChainIndex);
            if (questLoadMethod != null) questLoadMethod.Invoke(QuestManager.Instance, null);

            if (QuestManager.Instance.activeQuests != null && QuestManager.Instance.activeQuests.Count > 0)
            {
                Quest loadedQuest = QuestManager.Instance.activeQuests[0];
                if (workingCount >= loadedQuest.targetCount)
                {
                    workingChainIndex++;
                    if (questChainIndexField != null) questChainIndexField.SetValue(QuestManager.Instance, workingChainIndex);
                    if (questLoadMethod != null) questLoadMethod.Invoke(QuestManager.Instance, null);

                    if (QuestManager.Instance.activeQuests != null && QuestManager.Instance.activeQuests.Count > 0)
                    {
                        QuestManager.Instance.activeQuests[0].currentCount = 0;
                    }
                }
                else
                {
                    loadedQuest.currentCount = workingCount;
                }
            }
        }

        Debug.Log("<color=cyan>[Save System]</color> Multi-Tank Safe Load Sequence Finalized Successfully.");
    }

    private IEnumerator ApplyDelayedScale(GameObject target, Vector3 savedScale)
    {
        yield return new WaitForEndOfFrame();
        if (target != null) target.transform.localScale = savedScale;
    }
}