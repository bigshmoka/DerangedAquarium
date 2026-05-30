using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AquariumManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject foodPrefab;
    public GameObject fishPrefab;
    public GameObject plantPrefab;

    [Header("Economy Settings")]
    public int totalMoney = 100;
    public float incomeInterval = 3f; 
    private float incomeTimer;

    [Header("UI Reference")]
    public TMP_Text moneyText; 

    // --- NEW PLACEMENT VARIABLES ---
    private GameObject activePlantPreview;
    private bool isPlacingPlant = false;

    void Start()
    {
        UpdateMoneyUI(); 

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
        
        // If we are placing a plant, run the placement loop; otherwise, handle standard clicks
        if (isPlacingPlant)
        {
            HandlePlantPlacement();
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
            // Don't spawn food if clicking the UI bar at the bottom
            if (Input.mousePosition.y < 150) return;

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; 

            if (foodPrefab != null)
            {
                Instantiate(foodPrefab, mousePos, Quaternion.identity);
            }
        }
    }

    // --- NEW PLACEMENT LOGIC ---
    void HandlePlantPlacement()
    {
        // 1. Get current mouse world position
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        // 2. Make the ghost preview follow the mouse smoothly
        if (activePlantPreview != null)
        {
            activePlantPreview.transform.position = mousePos;
        }

        // 3. Right-click or Escape cancels placement mode
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Destroy(activePlantPreview);
            isPlacingPlant = false;
            Debug.Log("Plant placement canceled.");
            return;
        }

        // 4. Left-click places the plant permanently
        if (Input.GetMouseButtonDown(0))
        {
            // Block placing if player accidentally clicks down in the UI shop panel area
            //if (Input.mousePosition.y < 150) return;

            // Finalize placement: Deduct cash
            totalMoney -= 50;
            UpdateMoneyUI();

            // Turn the preview object into a fully physical plant item by resetting its opacity
            SpriteRenderer sr = activePlantPreview.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1.0f); // Make fully solid
            }

            // Let go of the reference so it stays right there in the scene permanently
            activePlantPreview = null;
            isPlacingPlant = false;
            Debug.Log("Plant placed successfully!");
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

    // --- MODIFIED DECORATION FUNCTION ---
    public void BuyDecoration()
    {
        // Check if player has cash AND isn't already holding a plant
        if (totalMoney >= 50 && plantPrefab != null && !isPlacingPlant)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;

            // Spawn a temporary instance to act as our cursor ghost
            activePlantPreview = Instantiate(plantPrefab, mousePos, Quaternion.identity);
            
            // Give the preview a translucent "ghostly" look (50% see-through)
            SpriteRenderer sr = activePlantPreview.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.5f);
            }

            isPlacingPlant = true;
        }
    }
    // Call this to penalize the player when a fish starves
public void DeductPlantedCash(int amount)
{
    totalMoney -= amount;
    if (totalMoney < 0) totalMoney = 0; // Prevent debt!
    UpdateMoneyUI(); // Refresh numbers on screen instantly
}
}