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
    public TMP_Text errorNotificationText; // Drag your NotificationText UI object here
    
    [Header("Feeding Tool Settings")]
    public Button feedToolButton;       // Drag your FeedToolButton here
    public TMP_Text feedToolText;       // Drag the TextMeshPro text component of that button here
    private bool isFeedToolActive = false;

    // --- PLACEMENT VARIABLES ---
    private GameObject activeDecorationPreview;
    private bool isPlacingDecoration = false;
    private GameObject selectedDecorationPrefab;
    private int selectedDecorationCost;

    void Start()
    {
        UpdateMoneyUI(); 
        UpdateFeedButtonUI(); // Set initial button appearance

        if (shopMenuWindow != null)
        {
            shopMenuWindow.SetActive(false);
        }

        // Deactivate error notification text at start
        if (errorNotificationText != null)
        {
            errorNotificationText.gameObject.SetActive(false);
        }

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
        // Left click detected
        if (Input.GetMouseButtonDown(0))
        {
            // Block clicks if clicking inside an open shop menu panel
            if (shopMenuWindow != null && shopMenuWindow.activeSelf) return;
            
            // Block food drops if clicking low on the screen where the bottom UI bar sits
            if (Input.mousePosition.y < 120) return;

            // --- SCREEN BOUNDARY SAFETY CHECK (1920x1080) ---
            // If the mouse wanders outside the active game window layout, ignore the click entirely!
            if (Input.mousePosition.x < 0 || Input.mousePosition.x > 1920 ||
                Input.mousePosition.y < 0 || Input.mousePosition.y > 1080)
            {
                return; 
            }

            // --- CRITICAL CHECK: ONLY DROP FOOD IF THE TOOL IS TOGGLED ON ---
            if (isFeedToolActive)
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

    // --- TOGGLE FUNCTION CALLED BY THE FEED BUTTON ---
    public void ToggleFeedingTool()
    {
        isFeedToolActive = !isFeedToolActive;
        UpdateFeedButtonUI();
    }

    void UpdateFeedButtonUI()
    {
        if (feedToolText != null && feedToolButton != null)
        {
            if (isFeedToolActive)
            {
                feedToolText.text = "Feed: ON";
                feedToolButton.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f, 1.0f); // Vibrant Green
            }
            else
            {
                feedToolText.text = "Feed: OFF";
                feedToolButton.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 1.0f); // Neutral Gray
            }
        }
    }

    public void OpenShopMenu()
    {
        if (shopMenuWindow != null)
        {
            shopMenuWindow.SetActive(true);
        }
    }

    public void CloseShopMenu()
    {
        if (shopMenuWindow != null)
        {
            shopMenuWindow.SetActive(false);
        }
    }

    // Universal fish purchase function called by Fish Grid buttons
    public void BuyFishFromShop(GameObject fishPrefab, int cost)
    {
        if (totalMoney >= cost && fishPrefab != null)
        {
            totalMoney -= cost;
            UpdateMoneyUI();

            // Spawn the fish right in the open water!
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

            // Force feeding tool OFF when buying a decoration so inputs don't fight
            isFeedToolActive = false;
            UpdateFeedButtonUI();

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;

            activeDecorationPreview = Instantiate(selectedDecorationPrefab, mousePos, Quaternion.identity);
            SpriteRenderer sr = activeDecorationPreview.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.5f); // Transparent preview ghost
            }

            isPlacingDecoration = true;
        }
    }

    void HandleDecorationPlacement()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        if (activeDecorationPreview != null)
        {
            activeDecorationPreview.transform.position = mousePos;
        }

        // Cancel placement on Right-Click or Escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Destroy(activeDecorationPreview);
            isPlacingDecoration = false;
            selectedDecorationPrefab = null;
            return;
        }

        // Confirm placement on Left-Click
        if (Input.GetMouseButtonDown(0))
        {
            totalMoney -= selectedDecorationCost;
            UpdateMoneyUI();

            SpriteRenderer sr = activeDecorationPreview.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1.0f); // Make solid
            }

            activeDecorationPreview = null;
            isPlacingDecoration = false;
            selectedDecorationPrefab = null;
        }
    }

    // --- VISUAL NOTIFICATION ALERTS ---
    public void TriggerNotificationAlert(string message)
    {
        if (errorNotificationText != null)
        {
            errorNotificationText.text = message;
            errorNotificationText.gameObject.SetActive(true);

            // Clear any lingering hide requests so spam-clicking doesn't cause overlapping glitches
            CancelInvoke(nameof(HideNotificationAlert));

            // Auto-hide the text pop-up after 2.5 seconds
            Invoke(nameof(HideNotificationAlert), 2.5f);
        }
    }

    void HideNotificationAlert()
    {
        if (errorNotificationText != null)
        {
            errorNotificationText.gameObject.SetActive(false);
        }
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = "Money: $" + totalMoney;
        }
    }

    public void DeductPlantedCash(int amount)
    {
        totalMoney -= amount;
        if (totalMoney < 0) totalMoney = 0; 
        UpdateMoneyUI(); 
    }
}