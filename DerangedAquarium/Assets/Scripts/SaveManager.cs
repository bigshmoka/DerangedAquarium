using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Collections; 

// --- SERIALIZABLE RAW DATA STRUCTURES ---
[System.Serializable]
public class GameSaveData
{
    public int walletBalance = 100;
    public List<PlacedItemDataWrapper> placed3DItems = new List<PlacedItemDataWrapper>();
    public List<AquariumItemDataWrapper> placed2DItems = new List<AquariumItemDataWrapper>();
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
    
    // --- FIXED: SAVE DIRECTION FOR FLIP DETECTION SYSTEM ---
    public float fishFacingSign = 1f; 
}

// --- MASTER SAVE SYSTEM ENGINE ---
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string saveFilePath;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        saveFilePath = Path.Combine(Application.persistentDataPath, "storefront_save.json");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) SaveGame();
        if (Input.GetKeyDown(KeyCode.F9)) LoadGame();
    }

    public void SaveGame()
    {
        if (GlobalEconomyManager.Instance == null) return;

        GameSaveData dataToSave = new GameSaveData();

        dataToSave.walletBalance = GlobalEconomyManager.Instance.GetBalance();

        GameObject itemContainer = GameObject.Find("--- PLACED 3D ITEMS ---");
        if (itemContainer != null)
        {
            foreach (Transform child in itemContainer.transform)
            {
                PlacedItemData itemProfile = child.GetComponent<PlacedItemData>();
                if (itemProfile != null)
                {
                    PlacedItemDataWrapper itemWrapper = new PlacedItemDataWrapper();
                    string cleanName = child.name.Replace("_Placed", "").Trim();
                    itemWrapper.prefabResourceName = cleanName;
                    itemWrapper.originalCost = itemProfile.originalCost;
                    itemWrapper.position = child.position;
                    itemWrapper.rotation = child.rotation;

                    dataToSave.placed3DItems.Add(itemWrapper);
                }
            }
        }

        AquariumManager tankManager = FindFirstObjectByType<AquariumManager>();
        if (tankManager != null)
        {
            foreach (Transform child in tankManager.transform)
            {
                bool isFish = child.GetComponent("NaturalFishAI") != null || child.name.Contains("Fish");
                bool isSnail = child.GetComponent("SnailAI") != null || child.name.Contains("Snail");
                bool isDecor = child.name.Contains("_Placed") || child.GetComponent("TankDecoration") != null;
                bool isUtilityItem = child.name.Contains("Feeder") || child.name.Contains("Item") || child.name.Contains("Machine");

                if (isFish || isSnail || isDecor || isUtilityItem)
                {
                    AquariumItemDataWrapper aqWrapper = new AquariumItemDataWrapper();
                    string cleanName = child.name.Replace("(Clone)", "").Replace("_Placed", "").Trim();
                    aqWrapper.prefabResourceName = cleanName;
                    aqWrapper.position = child.position;
                    aqWrapper.localScale = child.localScale;

                    NaturalFishAI fishAI = child.GetComponent<NaturalFishAI>();
                    if (fishAI != null)
                    {
                        aqWrapper.fishScaleModifier = fishAI.currentScaleModifier;
                        aqWrapper.fishBaseScale = fishAI.baseScale;
                        aqWrapper.fishFacingSign = fishAI.facingDirectionSign;
                    }

                    SnailAI snailAI = child.GetComponent<SnailAI>();
                    if (snailAI != null)
                    {
                        aqWrapper.fishBaseScale = snailAI.originalScale;
                    }

                    dataToSave.placed2DItems.Add(aqWrapper);
                }
            }
        }

        string jsonString = JsonUtility.ToJson(dataToSave, true);
        File.WriteAllText(saveFilePath, jsonString);

        Debug.Log($"<color=green>[Save System]</color> Full Game Saved (3D Shop & 2D Tank) to: {saveFilePath}");
    }

    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("[Save System] No existing save file found. Cannot execute loading sequence.");
            return;
        }

        if (GlobalEconomyManager.Instance == null) return;

        string jsonString = File.ReadAllText(saveFilePath);
        GameSaveData loadedData = JsonUtility.FromJson<GameSaveData>(jsonString);

        int currentWalletBalance = GlobalEconomyManager.Instance.GetBalance();
        int loadingBalanceDifference = loadedData.walletBalance - currentWalletBalance;
        if (loadingBalanceDifference > 0) GlobalEconomyManager.Instance.AddMoney(loadingBalanceDifference);
        else if (loadingBalanceDifference < 0) GlobalEconomyManager.Instance.DeductMoney(Mathf.Abs(loadingBalanceDifference));

        GameObject itemContainer = GameObject.Find("--- PLACED 3D ITEMS ---");
        if (itemContainer != null)
        {
            for (int i = itemContainer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(itemContainer.transform.GetChild(i).gameObject);
            }
        }

        foreach (PlacedItemDataWrapper savedItem in loadedData.placed3DItems)
        {
            GameObject rawPrefabFile = Resources.Load<GameObject>($"StorefrontPrefabs/{savedItem.prefabResourceName}");
            if (rawPrefabFile != null)
            {
                GameObject loadedInstance = Instantiate(rawPrefabFile, savedItem.position, savedItem.rotation, itemContainer != null ? itemContainer.transform : null);
                loadedInstance.name = savedItem.prefabResourceName + "_Placed";
                PlacedItemData priceTag = loadedInstance.AddComponent<PlacedItemData>();
                priceTag.originalCost = savedItem.originalCost;
            }
        }

        AquariumManager tankManager = FindFirstObjectByType<AquariumManager>();
        if (tankManager != null)
        {
            foreach (Transform child in tankManager.transform)
            {
                bool isFish = child.GetComponent("NaturalFishAI") != null || child.name.Contains("Fish");
                bool isSnail = child.GetComponent("SnailAI") != null || child.name.Contains("Snail");
                bool isDecor = child.name.Contains("_Placed") || child.GetComponent("TankDecoration") != null;
                bool isUtilityItem = child.name.Contains("Feeder") || child.name.Contains("Item") || child.name.Contains("Machine");

                if (isFish || isSnail || isDecor || isUtilityItem)
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

                    // --- FIXED: INJECT GROWTH STAGES DIRECTLY BEFORE WAKING SCRIPTS ---
                    if (fishAI != null)
                    {
                        fishAI.baseScale = saved2DItem.fishBaseScale != Vector3.zero ? saved2DItem.fishBaseScale : new Vector3(0.4f, 0.4f, 1f);
                        fishAI.currentScaleModifier = saved2DItem.fishScaleModifier > 0f ? saved2DItem.fishScaleModifier : fishAI.startingScale;
                        fishAI.facingDirectionSign = saved2DItem.fishFacingSign != 0f ? saved2DItem.fishFacingSign : 1f;
                        
                        // Natively assign current rendering local scale configuration immediately
                        loaded2DInstance.transform.localScale = saved2DItem.localScale;
                    }
                    else if (snailAI != null)
                    {
                        snailAI.originalScale = saved2DItem.fishBaseScale != Vector3.zero ? saved2DItem.fishBaseScale : new Vector3(0.4f, 0.4f, 1f);
                        loaded2DInstance.transform.localScale = saved2DItem.localScale;
                    }
                    else
                    {
                        // Fallback stretch properties only for basic decorations and auto-feeders
                        StartCoroutine(ApplyDelayedScale(loaded2DInstance, saved2DItem.localScale));
                    }

                    if (!tankManager.isTankVisible)
                    {
                        Renderer[] childRenderers = loaded2DInstance.GetComponentsInChildren<Renderer>();
                        foreach (Renderer rend in childRenderers)
                        {
                            rend.enabled = false;
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[Save System] Failed to find 2D asset: 'AquariumPrefabs/{saved2DItem.prefabResourceName}'.");
                }
            }
        }

        Debug.Log("<color=cyan>[Save System]</color> Complete game state (3D & 2D) successfully loaded!");
    }

    private IEnumerator ApplyDelayedScale(GameObject target, Vector3 savedScale)
    {
        yield return new WaitForEndOfFrame();

        if (target != null)
        {
            target.transform.localScale = savedScale;
        }
    }
}