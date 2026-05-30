using UnityEngine;

public class AquariumManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject foodPrefab;
    public GameObject fishPrefab;
    public GameObject plantPrefab;

    [Header("Economy Settings")]
    public int totalMoney = 100;
    public float incomeInterval = 3f; // Give money every 3 seconds
    private float incomeTimer;

    // --- THIS IS THE CRITICAL FUNCTION I ACCIDENTALLY LEFT OUT! ---
    void Start()
    {
        // Automatically spawn 3 starting fish when the game begins
        if (fishPrefab != null)
        {
            Instantiate(fishPrefab, new Vector3(-2f, 0f, 0f), Quaternion.identity);
            Instantiate(fishPrefab, new Vector3(0f, 2f, 0f), Quaternion.identity);
            Instantiate(fishPrefab, new Vector3(2f, -1f, 0f), Quaternion.identity);
            Debug.Log("Aquarium Manager successfully spawned 3 starting fish!");
        }
        else
        {
            Debug.LogError("Oops! The Fish Prefab slot is empty on the _AquariumManager object!");
        }
    }

    void Update()
    {
        HandlePassiveIncome();
        HandleMouseClicks();
    }

    void HandlePassiveIncome()
    {
        incomeTimer += Time.deltaTime;
        if (incomeTimer >= incomeInterval)
        {
            // Modern, fast method to find all fish in your scene
            NaturalFishAI[] allFish = FindObjectsByType<NaturalFishAI>(FindObjectsSortMode.None);
            
            // Each fish generates 5 currency units per interval
            int earned = allFish.Length * 5;
            totalMoney += earned;

            if (earned > 0)
            {
                Debug.Log($"Collected ${earned} from your fish! Total Money: ${totalMoney}");
            }

            incomeTimer = 0f; // Reset the income clock
        }
    }

    void HandleMouseClicks()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Convert mouse screen pixels directly into 2D world units
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; 

            Debug.Log($"Mouse clicked at world position: {mousePos}");

            if (foodPrefab != null)
            {
                Instantiate(foodPrefab, mousePos, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning($"Click registered, but the FoodPrefab slot is empty on the GameObject named: '{gameObject.name}'!");
            }
        }
    }

    // Public functions designed to be linked to UI Shop Buttons later
    public void BuyNewFish()
    {
        if (totalMoney >= 30 && fishPrefab != null)
        {
            totalMoney -= 30;
            Instantiate(fishPrefab, Vector3.zero, Quaternion.identity);
        }
    }

    public void BuyDecoration()
    {
        if (totalMoney >= 50 && plantPrefab != null)
        {
            totalMoney -= 50;
            float randomX = Random.Range(-5f, 5f);
            Vector3 floorPosition = new Vector3(randomX, -3.5f, 0f);
            Instantiate(plantPrefab, floorPosition, Quaternion.identity);
        }
    }
}