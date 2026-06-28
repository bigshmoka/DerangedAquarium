using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AquariumManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject foodPrefab;
    public GameObject fishPrefab;

    [Header("Economy Settings")]
    public int totalMoneySetting = 100;

    [Header("UI Windows & Tools")]
    public GameObject shopMenuWindow; 
    public TMP_Text moneyText; 
    public TMP_Text errorNotificationText; 
    
    [Header("Feeding Tool Settings")]
    public Button feedToolButton;       
    public TMP_Text feedToolText;       

    [Header("Sponge Tool Settings")]
    public Button spongeToolButton;     
    public TMP_Text spongeToolText;     

    // ===================================================================
    // --- RESTORED & UPGRADED: VIEW-STATE PROPERTY LAYER ---
    // Tracks visibility. When set to false by your 3D interaction scripts,
    // it automatically commands the UI to flush its tool highlights clean.
    // ===================================================================
    private bool _isTankVisible = false;
    [HideInInspector] 
    public bool isTankVisible 
    {
        get { return _isTankVisible; }
        set 
        { 
            _isTankVisible = value; 
            if (!_isTankVisible)
            {
                isShopOpen = false; 
                ResetTankUIState();
            }
        }
    }

    // --- RESTORED: LOAD SYSTEM DUPLICATION GUARD ---
    [HideInInspector] public bool skipDefaultSpawn = false;

    [Header("Multi-Tank Core Settings")]
    public string tankID = "StarterTank";
    public AlgaeManager algaeManager;

    [Header("Visual Links (Assign in Inspector!)")]
    [Tooltip("Drag THIS tank's specific 2D Camera here.")]
    public Camera tankCamera;

    [Tooltip("Drag THIS tank's specific main UI Canvas here.")]
    public Canvas mainTankCanvas;

    private TankEconomy economy;
    private TankShopUI shopUI;
    private TankHierarchyTracker hierarchyTracker;
    private TankPlacementSystem placementSystem;
    private TankInputHandler inputHandler;

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public int totalMoney {
        get { return economy != null ? economy.totalMoney : totalMoneySetting; }
        set { 
            if (economy != null) {
                economy.totalMoney = value;
                economy.UpdateBalanceUI(); 
            } 
        }
    }
    
    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public bool isShopOpen {
        get { return shopUI != null ? shopUI.isShopOpen : false; }
        set { if (shopUI != null) shopUI.isShopOpen = value; }
    }

    void Awake()
    {
        // Internal structural component initialization mappings
        economy = gameObject.GetComponent<TankEconomy>() ?? gameObject.AddComponent<TankEconomy>();
        shopUI = gameObject.GetComponent<TankShopUI>() ?? gameObject.AddComponent<TankShopUI>();
        hierarchyTracker = gameObject.GetComponent<TankHierarchyTracker>() ?? gameObject.AddComponent<TankHierarchyTracker>();
        placementSystem = gameObject.GetComponent<TankPlacementSystem>() ?? gameObject.AddComponent<TankPlacementSystem>();
        inputHandler = gameObject.GetComponent<TankInputHandler>() ?? gameObject.AddComponent<TankInputHandler>();

        if (algaeManager == null) algaeManager = GetComponentInChildren<AlgaeManager>();

        // Wire window component profiles up straight to the UI handler
        shopUI.shopMenuWindow = shopMenuWindow;
        shopUI.moneyText = moneyText;
        shopUI.errorNotificationText = errorNotificationText;
        shopUI.feedToolButton = feedToolButton;
        shopUI.feedToolText = feedToolText;
        shopUI.spongeToolButton = spongeToolButton;
        shopUI.spongeToolText = spongeToolText;

        inputHandler.foodPrefab = foodPrefab;

        economy.Initialize(shopUI);
        placementSystem.Initialize(economy, shopUI);
        inputHandler.Initialize(shopUI, placementSystem, hierarchyTracker);

        // --- RESTORED: MULTI-TANK AUTOMATED LINK INJECTION ---
        if (mainTankCanvas != null)
        {
            ShopClickTab localTab = mainTankCanvas.GetComponentInChildren<ShopClickTab>(true);
            if (localTab != null)
            {
                localTab.aquariumManager = this;
                if (shopMenuWindow != null) shopMenuWindow.SetActive(true);
            }
        }

        // Safety validations to ensure layout assemblies aren't missing assignments
        if (tankCamera == null) Debug.LogError($"[Tank Fix] You forgot to drag the Camera into the {gameObject.name} manager!");
        if (mainTankCanvas == null) Debug.LogError($"[Tank Fix] You forgot to drag the Canvas into the {gameObject.name} manager!");

        // Put expansion modules to sleep on frame zero to save system performance
        if (tankID != "StarterTank")
        {
            gameObject.SetActive(false);
        }
    }

    void Start()
    {
        if (gameObject.scene.isLoaded) SceneManager.SetActiveScene(gameObject.scene);

        int aqLayerIndex = LayerMask.NameToLayer("Aquarium");
        if (aqLayerIndex != -1)
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects()) SetLayerRecursive(root, aqLayerIndex);
        }

        UpdateMoneyUI(); 
        shopUI.UpdateFeedButtonUI();
        shopUI.UpdateSpongeButtonUI();

        if (errorNotificationText != null) errorNotificationText.gameObject.SetActive(false);

        // --- RESTORED: INDEPENDENT SPAWNING MATRIX WITH LOAD SHIELDING ---
        if (!skipDefaultSpawn && fishPrefab != null)
        {
            SpawnBabyFish(fishPrefab, new Vector3(-2f, 0f, 0f));
            SpawnBabyFish(fishPrefab, new Vector3(0f, 2f, 0f));
            SpawnBabyFish(fishPrefab, new Vector3(2f, -1f, 0f));
        }

        ApplyCameraCullingFilters();
    }

    void LateUpdate()
    {
        int aqLayerIndex = LayerMask.NameToLayer("Aquarium");
        if (aqLayerIndex != -1) EnsureLayerRecursive(this.gameObject, aqLayerIndex);
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    private void EnsureLayerRecursive(GameObject obj, int newLayer)
    {
        if (obj.layer == LayerMask.NameToLayer("UI")) return;
        if (obj.layer != newLayer) obj.layer = newLayer;
        foreach (Transform child in obj.transform) EnsureLayerRecursive(child.gameObject, newLayer);
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        if (obj.layer == LayerMask.NameToLayer("UI")) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, newLayer);
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    private void ApplyCameraCullingFilters()
    {
        int aqLayerIndex = LayerMask.NameToLayer("Aquarium");
        if (aqLayerIndex == -1) return;

        int uiLayerIndex = LayerMask.NameToLayer("UI");

        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in allCameras)
        {
            if (cam.orthographic && (cam.name.Contains("Aquarium") || cam.name.Contains("Tank") || (cam.cullingMask & (1 << aqLayerIndex)) != 0))
            {
                int isolatedMask = (1 << aqLayerIndex);
                if (uiLayerIndex != -1) isolatedMask |= (1 << uiLayerIndex);
                cam.cullingMask = isolatedMask;
            }
        }
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public void UpdateMoneyUI()
    {
        if (economy != null) economy.UpdateBalanceUI();
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public bool IsSpongeToolActive()
    {
        return shopUI != null && shopUI.isSpongeToolActive;
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public void ToggleFeedingTool()
    {
        if (shopUI != null) shopUI.ToggleFeedingTool();
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public void ToggleSpongeTool()
    {
        if (shopUI != null) shopUI.ToggleSpongeTool();
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public void OpenShopMenu()
    {
        if (shopUI != null) shopUI.OpenShopMenu();
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public void CloseShopMenu()
    {
        if (shopUI != null) shopUI.CloseShopMenu();
    }

    // Explicitly commands tool and canvas flushes clean
    public void ResetTankUIState()
    {
        if (shopUI != null) shopUI.ResetUI();
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public void TriggerNotificationAlert(string msg)
    {
        if (shopUI != null) shopUI.TriggerNotificationAlert(msg);
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public void DeductPlantedCash(int amt)
    {
        if (economy != null) economy.DeductCash(amt);
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public Transform GetFoodContainer()
    {
        return hierarchyTracker != null ? hierarchyTracker.foodContainer : null;
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public Transform GetBubbleContainer()
    {
        return hierarchyTracker != null ? hierarchyTracker.bubbleContainer : null;
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public void SpawnBabyFish(GameObject prefab, Vector3 localPosition)
    {
        Vector3 worldPos = this.transform.position + localPosition;
        GameObject newFish = Instantiate(prefab, worldPos, Quaternion.identity, this.transform);
        float babyScale = 0.4f;
        newFish.transform.localScale = new Vector3(babyScale, babyScale, 1f);
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public void BuyFishFromShop(GameObject prefab, int cost)
    {
        if (prefab != null && economy.TrySpendMoney(cost))
        {
            shopUI.CloseShopMenu();
            SpawnBabyFish(prefab, Vector3.zero);
            if (QuestManager.Instance != null) QuestManager.Instance.ProgressQuest("buy_creatures", 1);
        }
        else if (economy.totalMoney < cost) 
        {
            shopUI.TriggerNotificationAlert("Not enough money!");
        }
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public void SelectDecorationFromShop(GameObject prefab, int cost)
    {
        if (prefab != null && economy.totalMoney >= cost) 
        {
            placementSystem.StartDecorationPlacement(prefab, cost);
        }
        else if (economy.totalMoney < cost) 
        {
            shopUI.TriggerNotificationAlert("Not enough money!");
        }
    }

    // --- RESTORED FROM ORIGINAL "SCRIPTS" ASSET ---
    public void SelectItemFromShop(GameObject prefab, int cost)
    {
        if (prefab != null && economy.totalMoney >= cost) 
        {
            placementSystem.StartItemPlacement(prefab, cost);
        }
        else if (economy.totalMoney < cost) 
        {
            shopUI.TriggerNotificationAlert("Not enough money!");
        }
    }
}