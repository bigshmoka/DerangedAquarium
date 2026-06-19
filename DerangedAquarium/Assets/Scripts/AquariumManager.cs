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

    [Header("UI Windows & Tools")]
    public TMP_Text moneyText; 
    public GameObject shopMenuWindow; 
    public TMP_Text errorNotificationText; 
    
    [Header("Feeding Tool Settings")]
    public Button feedToolButton;       
    public TMP_Text feedToolText;       
    private bool isFeedToolActive = false;

    [Header("Sponge Tool Settings")]
    public Button spongeToolButton;     // Drag your SpongeToolButton here
    public TMP_Text spongeToolText;     // Drag the Sponge Button's TMP text component here
    private bool isSpongeToolActive = false;

    private GameObject activeDecorationPreview;
    private bool isPlacingDecoration = false;
    private GameObject selectedDecorationPrefab;
    private int selectedDecorationCost;

    void Start()
    {
        UpdateMoneyUI(); 
        UpdateFeedButtonUI(); 
        UpdateSpongeButtonUI(); // Set initial sponge appearance

        if (shopMenuWindow != null) shopMenuWindow.SetActive(false);
        if (errorNotificationText != null) errorNotificationText.gameObject.SetActive(false);

        // Spawn initial starting fish
        if (fishPrefab != null)
        {
            Instantiate(fishPrefab, new Vector3(-2f, 0f, 0f), Quaternion.identity);
            Instantiate(fishPrefab, new Vector3(0f, 2f, 0f), Quaternion.identity);
            Instantiate(fishPrefab, new Vector3(2f, -1f, 0f), Quaternion.identity);
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
            if (shopMenuWindow != null && shopMenuWindow.activeSelf) return;
            if (Input.mousePosition.y < 120) return;

            // SCREEN BOUNDARY SAFETY CHECK (1920x1080)
            if (Input.mousePosition.x < 0 || Input.mousePosition.x > 1920 ||
                Input.mousePosition.y < 0 || Input.mousePosition.y > 1080)
            {
                return; 
            }

            // ONLY DROP FOOD IF FEED TOOL IS ACTIVE (and not doing sponge work)
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

    // --- TOOL INTERCHANGE LOGIC ---

    public void ToggleFeedingTool()
    {
        isFeedToolActive = !isFeedToolActive;
        
        // If we turn feeding ON, force sponge OFF
        if (isFeedToolActive) isSpongeToolActive = false;

        UpdateFeedButtonUI();
        UpdateSpongeButtonUI();
    }

    public void ToggleSpongeTool()
    {
        isSpongeToolActive = !isSpongeToolActive;

        // If we turn sponge ON, force feeding OFF
        if (isSpongeToolActive) isFeedToolActive = false;

        UpdateFeedButtonUI();
        UpdateSpongeButtonUI();
    }

    // Public getter so AlgaeNodes can verify if the sponge is equipped
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
                feedToolButton.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f, 1.0f); // Green
            }
            else
            {
                feedToolText.text = "Feed: OFF";
                feedToolButton.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 1.0f); // Gray
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
                spongeToolButton.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f, 1.0f); // Vibrant Blue
            }
            else
            {
                spongeToolText.text = "Sponge: OFF";
                spongeToolButton.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 1.0f); // Gray
            }
        }
    }

    // --- PRE-EXISTING SYSTEMS ---

    public void OpenShopMenu() { if (shopMenuWindow != null) shopMenuWindow.SetActive(true); }
    public void CloseShopMenu() { if (shopMenuWindow != null) shopMenuWindow.SetActive(false); }

    public void BuyFishFromShop(GameObject fishPrefab, int cost)
    {
        if (totalMoney >= cost && fishPrefab != null)
        {
            totalMoney -= cost;
            UpdateMoneyUI();
            Instantiate(fishPrefab, Vector3.zero, Quaternion.identity);
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