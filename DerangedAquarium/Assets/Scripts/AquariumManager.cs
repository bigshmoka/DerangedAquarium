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
    
    // --- NEW FEEDING TOOL VARIABLES ---
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

    // --- NEW: TOGGLE FUNCTION CALLED BY THE BUTTON ---
    public void ToggleFeedingTool()
    {
        // Flip the true/false switch
        isFeedToolActive = !isFeedToolActive;
        
        // Refresh the button's text and color to reflect the state change
        UpdateFeedButtonUI();
    }

    void UpdateFeedButtonUI()
    {
        if (feedToolText != null && feedToolButton != null)
        {
            if (isFeedToolActive)
            {
                feedToolText.text = "Feed: ON";
                // Change the button image color to a highlighted green when active
                feedToolButton.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f, 1.0f);
            }
            else
            {
                feedToolText.text = "Feed: OFF";
                // Change the button image color back to a dull neutral gray when inactive
                feedToolButton.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
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

    public void SelectDecorationFromShop(GameObject decorationPrefab, int cost)
    {
        if (totalMoney >= cost && decorationPrefab != null && !isPlacingDecoration)
        {
            selectedDecorationPrefab = decorationPrefab;
            selectedDecorationCost = cost;

            CloseShopMenu();

            // Force feeding tool OFF when buying a decoration so they don't fight for inputs
            isFeedToolActive = false;
            UpdateFeedButtonUI();

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;

            activeDecorationPreview = Instantiate(selectedDecorationPrefab, mousePos, Quaternion.identity);
            SpriteRenderer sr = activeDecorationPreview.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.5f);
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
            if (sr != null)
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1.0f);
            }

            activeDecorationPreview = null;
            isPlacingDecoration = false;
            selectedDecorationPrefab = null;
        }
    }

    void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = "Money: $" + totalMoney;
        }
    }

    public void BuyNewFish()
    {
        if (totalMoney >= 30 && fishPrefab != null)
        {
            totalMoney -= 30;
            UpdateMoneyUI(); 
            Instantiate(fishPrefab, Vector3.zero, Quaternion.identity);
        }
    }

    public void DeductPlantedCash(int amount)
    {
        totalMoney -= amount;
        if (totalMoney < 0) totalMoney = 0; 
        UpdateMoneyUI(); 
    }
    // Universal fish purchase function called by Fish Grid TMP buttons
public void BuyFishFromShop(GameObject fishPrefab, int cost)
{
    if (totalMoney >= cost && fishPrefab != null)
    {
        // Deduct the cash amount immediately
        totalMoney -= cost;
        UpdateMoneyUI();

        // Spawn the swimming fish right in the middle of the tank!
        Instantiate(fishPrefab, Vector3.zero, Quaternion.identity);

        // Close the shop window so they can see their brand new pet spawn
        CloseShopMenu();
    }
}
}