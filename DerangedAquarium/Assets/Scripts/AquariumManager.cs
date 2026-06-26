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

    // --- NEW: GLOBAL VISIBILITY TRACKER FLAG ---
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
        economy = gameObject.GetComponent<TankEconomy>() ?? gameObject.AddComponent<TankEconomy>();
        shopUI = gameObject.GetComponent<TankShopUI>() ?? gameObject.AddComponent<TankShopUI>();
        hierarchyTracker = gameObject.GetComponent<TankHierarchyTracker>() ?? gameObject.AddComponent<TankHierarchyTracker>();
        placementSystem = gameObject.GetComponent<TankPlacementSystem>() ?? gameObject.AddComponent<TankPlacementSystem>();
        inputHandler = gameObject.GetComponent<TankInputHandler>() ?? gameObject.AddComponent<TankInputHandler>();

        economy.Initialize(shopUI);
        placementSystem.Initialize(economy, shopUI);
        inputHandler.Initialize(shopUI, placementSystem, hierarchyTracker);

        economy.totalMoney = totalMoneySetting;

        shopUI.shopMenuWindow = shopMenuWindow;
        shopUI.moneyText = moneyText;
        shopUI.errorNotificationText = errorNotificationText;
        shopUI.feedToolButton = feedToolButton;
        shopUI.feedToolText = feedToolText;
        shopUI.spongeToolButton = spongeToolButton;
        shopUI.spongeToolText = spongeToolText;

        inputHandler.foodPrefab = foodPrefab;
    }

    void Start()
    {
        if (gameObject.scene.isLoaded)
        {
            SceneManager.SetActiveScene(gameObject.scene);
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