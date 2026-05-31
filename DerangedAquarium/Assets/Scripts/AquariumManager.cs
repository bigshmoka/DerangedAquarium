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
    public float incomeInterval = 3f; 
    private float incomeTimer;

    [Header("UI Windows")]
    public TMP_Text moneyText; 
    public GameObject shopMenuWindow; 

    // --- PLACEMENT VARIABLES ---
    private GameObject activeDecorationPreview;
    private bool isPlacingDecoration = false;
    private GameObject selectedDecorationPrefab;
    private int selectedDecorationCost;

    void Start()
    {
        UpdateMoneyUI(); 

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
        HandlePassiveIncome();
        
        if (isPlacingDecoration)
        {
            HandleDecorationPlacement();
        }
        else
        {
            HandleMouseClicks();
        }
    }

    void HandlePassiveIncome()
    {
        incomeTimer += Time.deltaTime;
        if (incomeTimer >= incomeInterval)
        {
            NaturalFishAI[] allFish = FindObjectsByType<NaturalFishAI>(FindObjectsSortMode.None);
            int earned = allFish.Length * 5;
            totalMoney += earned;

            if (earned > 0)
            {
                UpdateMoneyUI(); 
            }

            incomeTimer = 0f; 
        }
    }

    void HandleMouseClicks()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Block spawning food if clicking inside an open menu pane
            if (shopMenuWindow != null && shopMenuWindow.activeSelf) return;

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; 

            if (foodPrefab != null)
            {
                Instantiate(foodPrefab, mousePos, Quaternion.identity);
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

    // Universal purchase function called by TMP grid item slots
    public void SelectDecorationFromShop(GameObject decorationPrefab, int cost)
    {
        if (totalMoney >= cost && decorationPrefab != null && !isPlacingDecoration)
        {
            selectedDecorationPrefab = decorationPrefab;
            selectedDecorationCost = cost;

            CloseShopMenu();

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
}