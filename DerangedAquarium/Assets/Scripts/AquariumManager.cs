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

    [HideInInspector] public bool isTankVisible = false;

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

    public int totalMoney {
        get { return economy != null ? economy.totalMoney : totalMoneySetting; }
        set { 
            if (economy != null) {
                economy.totalMoney = value;
                economy.UpdateBalanceUI(); 
            } 
        }
    }
    public bool isShopOpen {
        get { return shopUI != null ? shopUI.isShopOpen : false; }
        set { if (shopUI != null) shopUI.isShopOpen = value; }
    }

    void Awake()
    {
        economy = gameObject.GetComponent<TankEconomy>() ?? gameObject.AddComponent<TankEconomy>();
        shopUI = gameObject.GetComponent<TankShopUI>() ?? gameObject.AddComponent<TankShopUI>();
        hierarchyTracker = gameObject.GetComponent<TankHierarchyTracker>() ?? gameObject.AddComponent<TankHierarchyTracker>();
        placementSystem = gameObject.GetComponent<TankPlacementSystem>() ?? gameObject.AddComponent<TankPlacementSystem>();
        inputHandler = gameObject.GetComponent<TankInputHandler>() ?? gameObject.AddComponent<TankInputHandler>();

        if (algaeManager == null) algaeManager = GetComponentInChildren<AlgaeManager>();

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

        if (tankCamera == null) Debug.LogError($"[Tank Fix] You forgot to drag the Camera into the {gameObject.name} manager!");
        if (mainTankCanvas == null) Debug.LogError($"[Tank Fix] You forgot to drag the Canvas into the {gameObject.name} manager!");

        // ===================================================================
        // --- THE HIBERNATION FIX ---
        // If this expansion layout instance is additively awoken on bootup, 
        // force it into complete hibernation. It will wait until the 3D player 
        // physically finalizes placement before running initialization or ticking!
        // ===================================================================
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

        // This spawning block now fires cleanly only when the tank is activated
        if (fishPrefab != null)
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

    private void EnsureLayerRecursive(GameObject obj, int newLayer)
    {
        if (obj.layer == LayerMask.NameToLayer("UI")) return;
        if (obj.layer != newLayer) obj.layer = newLayer;
        foreach (Transform child in obj.transform) EnsureLayerRecursive(child.gameObject, newLayer);
    }

    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        if (obj.layer == LayerMask.NameToLayer("UI")) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, newLayer);
    }

    private void ApplyCameraCullingFilters()
    {
        int aqLayerIndex = LayerMask.NameToLayer("Aquarium");
        if (aqLayerIndex == -1) return;

        int uiLayerIndex = LayerMask.NameToLayer("UI");

        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

    public void UpdateMoneyUI()
    {
        if (economy != null) economy.UpdateBalanceUI();
    }

    public bool IsSpongeToolActive()
    {
        return shopUI != null && shopUI.isSpongeToolActive;
    }

    public void ToggleFeedingTool()
    {
        if (shopUI != null) shopUI.ToggleFeedingTool();
    }

    public void ToggleSpongeTool()
    {
        if (shopUI != null) shopUI.ToggleSpongeTool();
    }

    public void OpenShopMenu()
    {
        if (shopUI != null) shopUI.OpenShopMenu();
    }

    public void CloseShopMenu()
    {
        if (shopUI != null) shopUI.CloseShopMenu();
    }

    public void TriggerNotificationAlert(string msg)
    {
        if (shopUI != null) shopUI.TriggerNotificationAlert(msg);
    }

    public void DeductPlantedCash(int amt)
    {
        if (economy != null) economy.DeductCash(amt);
    }

    public Transform GetFoodContainer()
    {
        return hierarchyTracker != null ? hierarchyTracker.foodContainer : null;
    }

    public Transform GetBubbleContainer()
    {
        return hierarchyTracker != null ? hierarchyTracker.bubbleContainer : null;
    }

    public void SpawnBabyFish(GameObject prefab, Vector3 localPosition)
    {
        Vector3 worldPos = this.transform.position + localPosition;
        GameObject newFish = Instantiate(prefab, worldPos, Quaternion.identity, this.transform);
        float babyScale = 0.4f;
        newFish.transform.localScale = new Vector3(babyScale, babyScale, 1f);
    }

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