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

    [HideInInspector] public bool isTankVisible = true;

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
        // 1. Fetch or attach all sub-component modules
        economy = gameObject.GetComponent<TankEconomy>() ?? gameObject.AddComponent<TankEconomy>();
        shopUI = gameObject.GetComponent<TankShopUI>() ?? gameObject.AddComponent<TankShopUI>();
        hierarchyTracker = gameObject.GetComponent<TankHierarchyTracker>() ?? gameObject.AddComponent<TankHierarchyTracker>();
        placementSystem = gameObject.GetComponent<TankPlacementSystem>() ?? gameObject.AddComponent<TankPlacementSystem>();
        inputHandler = gameObject.GetComponent<TankInputHandler>() ?? gameObject.AddComponent<TankInputHandler>();

        // 2. ASSIGN UI REFERENCES FIRST
        shopUI.shopMenuWindow = shopMenuWindow;
        shopUI.moneyText = moneyText;
        shopUI.errorNotificationText = errorNotificationText;
        shopUI.feedToolButton = feedToolButton;
        shopUI.feedToolText = feedToolText;
        shopUI.spongeToolButton = spongeToolButton;
        shopUI.spongeToolText = spongeToolText;

        inputHandler.foodPrefab = foodPrefab;

        // 3. Now initialize sub-systems safely with populated data fields
        economy.Initialize(shopUI);
        placementSystem.Initialize(economy, shopUI);
        inputHandler.Initialize(shopUI, placementSystem, hierarchyTracker);
    }

    void Start()
    {
        if (gameObject.scene.isLoaded)
        {
            SceneManager.SetActiveScene(gameObject.scene);
        }

        // GLOBAL SCENE LAYER REPAIR TRACKER
        // Paints all static root objects in the scene (like background borders and sand) onto the Aquarium layer
        int aqLayerIndex = LayerMask.NameToLayer("Aquarium");
        if (aqLayerIndex != -1)
        {
            GameObject[] rootObjects = gameObject.scene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                SetLayerRecursive(root, aqLayerIndex);
            }
        }

        UpdateMoneyUI(); 
        shopUI.UpdateFeedButtonUI();
        shopUI.UpdateSpongeButtonUI();

        if (errorNotificationText != null) errorNotificationText.gameObject.SetActive(false);

        if (fishPrefab != null)
        {
            SpawnBabyFish(fishPrefab, new Vector3(-2f, 0f, 0f));
            SpawnBabyFish(fishPrefab, new Vector3(0f, 2f, 0f));
            SpawnBabyFish(fishPrefab, new Vector3(2f, -1f, 0f));
        }

        // Shield the 2D camera view from rendering 3D storefront meshes
        ApplyCameraCullingFilters();
    }

    // --- NEW: REAL-TIME CHILD LAYER ENFORCER ---
    // This loops through any objects generated under the manager hierarchy tree (like food, coins, or fish)
    // and forces them onto the correct rendering layer the exact frame they appear in the game world!
    void LateUpdate()
    {
        int aqLayerIndex = LayerMask.NameToLayer("Aquarium");
        if (aqLayerIndex != -1)
        {
            EnsureLayerRecursive(this.gameObject, aqLayerIndex);
        }
    }

    private void EnsureLayerRecursive(GameObject obj, int newLayer)
    {
        // Safety guard: Skip UI canvases so we don't break overlay rendering fields
        if (obj.layer == LayerMask.NameToLayer("UI")) return;

        if (obj.layer != newLayer)
        {
            obj.layer = newLayer;
        }

        foreach (Transform child in obj.transform)
        {
            EnsureLayerRecursive(child.gameObject, newLayer);
        }
    }

    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        if (obj.layer == LayerMask.NameToLayer("UI")) return;

        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }

    private void ApplyCameraCullingFilters()
    {
        int aqLayerIndex = LayerMask.NameToLayer("Aquarium");
        if (aqLayerIndex == -1)
        {
            Debug.LogWarning("[Camera Isolation] 'Aquarium' layer is missing from your project Tag & Layer configuration settings!");
            return;
        }

        int uiLayerIndex = LayerMask.NameToLayer("UI");

        // Scan all cameras loaded across active multi-scene memory layout nodes
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in allCameras)
        {
            // Isolate the 2D orthographic aquarium camera component
            if (cam.orthographic && (cam.name.Contains("Aquarium") || cam.name.Contains("Tank") || (cam.cullingMask & (1 << aqLayerIndex)) != 0))
            {
                // Re-bind its culling mask to rendering ONLY the Aquarium layer and the overlay Canvas UI layer
                int isolatedMask = (1 << aqLayerIndex);
                if (uiLayerIndex != -1)
                {
                    isolatedMask |= (1 << uiLayerIndex);
                }

                cam.cullingMask = isolatedMask;
                Debug.Log($"<color=cyan>[Camera Mask]</color> Successfully shielded orthographic camera <b>{cam.name}</b> from rendering 3D furniture meshes!");
            }
        }
    }

    public void UpdateMoneyUI()
    {
        if (economy != null) economy.UpdateBalanceUI();
    }

    public bool IsSpongeToolActive() => shopUI != null && shopUI.isSpongeToolActive;
    public void ToggleFeedingTool() => shopUI.ToggleFeedingTool();
    public void ToggleSpongeTool() => shopUI.ToggleSpongeTool();
    public void OpenShopMenu() => shopUI.OpenShopMenu();
    public void CloseShopMenu() => shopUI.CloseShopMenu();
    public void TriggerNotificationAlert(string msg) => shopUI.TriggerNotificationAlert(msg);
    public void DeductPlantedCash(int amt) => economy.DeductCash(amt);

    public Transform GetFoodContainer() => hierarchyTracker != null ? hierarchyTracker.foodContainer : null;
    public Transform GetBubbleContainer() => hierarchyTracker != null ? hierarchyTracker.bubbleContainer : null;

    public void SpawnBabyFish(GameObject prefab, Vector3 position)
    {
        GameObject newFish = Instantiate(prefab, position, Quaternion.identity, this.transform);
        float babyScale = 0.4f;
        newFish.transform.localScale = new Vector3(babyScale, babyScale, 1f);
    }

    public void BuyFishFromShop(GameObject prefab, int cost)
    {
        if (prefab != null && economy.TrySpendMoney(cost))
        {
            shopUI.CloseShopMenu();
            SpawnBabyFish(prefab, Vector3.zero);

            // PROGRESS THE CREATURE PURCHASE CAMPAIGN QUEST
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.ProgressQuest("buy_creatures", 1);
            }
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