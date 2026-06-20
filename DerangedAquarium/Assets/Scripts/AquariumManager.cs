using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AquariumManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject foodPrefab;
    public GameObject fishPrefab;

    [Header("Economy Settings")]
    public int totalMoney = 100;
    // --- NEW: Tracks if the shop is visually open to block tools safely ---
    [HideInInspector] public bool isShopOpen = false; 

    [Header("UI Windows & Tools")]
    public GameObject shopMenuWindow; 
    public TMP_Text moneyText; 
    public TMP_Text errorNotificationText; 
    
    [Header("Feeding Tool Settings")]
    public Button feedToolButton;       
    public TMP_Text feedToolText;       
    private bool isFeedToolActive = false;

    [Header("Sponge Tool Settings")]
    public Button spongeToolButton;     
    public TMP_Text spongeToolText;     
    private bool isSpongeToolActive = false;

    private GameObject activeDecorationPreview;
    private bool isPlacingDecoration = false;
    private GameObject selectedDecorationPrefab;
    private int selectedDecorationCost;

    void Start()
    {
        UpdateMoneyUI(); 
        UpdateFeedButtonUI(); 
        UpdateSpongeButtonUI(); 

        if (errorNotificationText != null) errorNotificationText.gameObject.SetActive(false);

        // Spawn initial starting fish at a perfect, uniform baby scale (e.g., 0.4f)
        if (fishPrefab != null)
        {
            SpawnBabyFish(fishPrefab, new Vector3(-2f, 0f, 0f));
            SpawnBabyFish(fishPrefab, new Vector3(0f, 2f, 0f));
            SpawnBabyFish(fishPrefab, new Vector3(2f, -1f, 0f));
        }
    }

    void Update()
    {
        if (isPlacingDecoration)
        {
            HandleDecorationPlacement();
        }
        else
        {
            HandleMouseClicks();
        }
    }

    void HandleMouseClicks()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // --- FIX: Check our state tracker instead of the UI GameObject's active status ---
            if (isShopOpen) return;
            if (Input.mousePosition.y < 120) return;

            // SCREEN BOUNDARY SAFETY CHECK (1920x1080)
            if (Input.mousePosition.x < 0 || Input.mousePosition.x > 1920 ||
                Input.mousePosition.y < 0 || Input.mousePosition.y > 1080)
            {
                return; 
            }

            if (isFeedToolActive && !isSpongeToolActive)
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0f; 

                if (foodPrefab != null)
                {
                    Instantiate(foodPrefab, mousePos, Quaternion.identity);
                }
            }
        }
    }

    void SpawnBabyFish(GameObject prefab, Vector3 position)
    {
        GameObject newFish = Instantiate(prefab, position, Quaternion.identity);
        float babyScale = 0.4f;
        newFish.transform.localScale = new Vector3(babyScale, babyScale, 1f);
    }

    public void ToggleFeedingTool()
    {
        isFeedToolActive = !isFeedToolActive;
        if (isFeedToolActive) isSpongeToolActive = false;
        UpdateFeedButtonUI();
        UpdateSpongeButtonUI();
    }

    public void ToggleSpongeTool()
    {
        isSpongeToolActive = !isSpongeToolActive;
        if (isSpongeToolActive) isFeedToolActive = false;
        UpdateFeedButtonUI();
        UpdateSpongeButtonUI();
    }

    public bool IsSpongeToolActive()
    {
        return isSpongeToolActive;
    }

    void UpdateFeedButtonUI()
    {
        if (feedToolText != null && feedToolButton != null)
        {
            if (isFeedToolActive)
            {
                feedToolText.text = "Feed: ON";
                feedToolButton.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f, 1.0f);
            }
            else
            {
                feedToolText.text = "Feed: OFF";
                feedToolButton.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            }
        }
    }

    void UpdateSpongeButtonUI()
    {
        if (spongeToolText != null && spongeToolButton != null)
        {
            if (isSpongeToolActive)
            {
                spongeToolText.text = "Sponge: ON";
                spongeToolButton.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f, 1.0f);
            }
            else
            {
                spongeToolText.text = "Sponge: OFF";
                spongeToolButton.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            }
        }
    }

    // --- FIX: Modernized to use state tracking flags instead of forcing active layout states ---
    public void OpenShopMenu() { isShopOpen = true; }
    public void CloseShopMenu() { isShopOpen = false; }

    public void BuyFishFromShop(GameObject fishPrefab, int cost)
    {
        if (totalMoney >= cost && fishPrefab != null)
        {
            totalMoney -= cost;
            UpdateMoneyUI();
            SpawnBabyFish(fishPrefab, Vector3.zero);
            CloseShopMenu();
        }
    }

    public void SelectDecorationFromShop(GameObject decorationPrefab, int cost)
    {
        if (totalMoney >= cost && decorationPrefab != null && !isPlacingDecoration)
        {
            selectedDecorationPrefab = decorationPrefab;
            selectedDecorationCost = cost;
            CloseShopMenu();

            isFeedToolActive = false;
            isSpongeToolActive = false;
            UpdateFeedButtonUI();
            UpdateSpongeButtonUI();

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;

            activeDecorationPreview = Instantiate(selectedDecorationPrefab, mousePos, Quaternion.identity);
            SpriteRenderer sr = activeDecorationPreview.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.5f);

            isPlacingDecoration = true;
        }
    }

    void HandleDecorationPlacement()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        if (activeDecorationPreview != null) activeDecorationPreview.transform.position = mousePos;

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Destroy(activeDecorationPreview);
            isPlacingDecoration = false;
            selectedDecorationPrefab = null;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            totalMoney -= selectedDecorationCost;
            UpdateMoneyUI();
            SpriteRenderer sr = activeDecorationPreview.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1.0f);
            activeDecorationPreview = null;
            isPlacingDecoration = false;
            selectedDecorationPrefab = null;
        }
    }

    public void TriggerNotificationAlert(string message)
    {
        if (errorNotificationText != null)
        {
            errorNotificationText.text = message;
            errorNotificationText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideNotificationAlert));
            Invoke(nameof(HideNotificationAlert), 2.5f);
        }
    }

    void HideNotificationAlert() { if (errorNotificationText != null) errorNotificationText.gameObject.SetActive(false); }
    void UpdateMoneyUI() { if (moneyText != null) moneyText.text = "Money: $" + totalMoney; }
    public void DeductPlantedCash(int amount) { totalMoney -= amount; if (totalMoney < 0) totalMoney = 0; UpdateMoneyUI(); }
}