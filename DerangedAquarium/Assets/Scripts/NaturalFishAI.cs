using UnityEngine;

public class NaturalFishAI : MonoBehaviour
{
    // --- NEW: CONFIGURABLE FISH TYPES ---
    public enum FishType { Goldfish, Tetra, Angelfish, Shark }

    [Header("Species Settings")]
    public FishType species = FishType.Goldfish; // Shows as a clean dropdown menu in the Unity Inspector

    [Header("Movement Tweaks")]
    public float swimSpeed = 1.2f;
    public float rushSpeedMultiplier = 1.5f; 

    [Header("Tank Boundaries (Safe Zones)")]
    public Vector2 minBounds = new Vector2(-7.5f, -3.5f);
    public Vector2 maxBounds = new Vector2(7.5f, 3.5f);

    [Header("Hunger & Survival Settings")]
    public float maxHunger = 30f;       
    public float hungerWarningTime = 15f; 
    private float currentHunger = 0f;
    private bool isDead = false;

    [Header("Growth Settings")]
    public float startingScale = 0.5f;     
    public float maxScale = 1.5f;          
    public float growthPerBite = 0.1f;     
    private float currentScaleModifier;

    [Header("Poop Economy")]
    public GameObject moneyDropPrefab; 

    private Color originalColor;
    private Color sickColor = new Color(0.8f, 0.8f, 0.3f, 1.0f); 

    private Vector3 targetDestination;
    private float closeEnoughThreshold = 0.3f;
    
    private FishFood currentFoodTarget;
    private bool isChasingFood = false;
    private SpriteRenderer spriteRenderer;

    // --- STRETCH PREVENTION VARIABLES ---
    private Vector3 baseScale;
    private float facingDirectionSign = 1f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.rotation = Quaternion.identity;

        // 1. Capture the exact scale proportions you assigned this fish in the inspector
        baseScale = transform.localScale;

        currentScaleModifier = startingScale;
        UpdateFishScale();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            originalColor = Color.white;
        }

        PickNewDestination();
    }

    void Update()
    {
        if (isDead)
        {
            FloatToSurface();
            return;
        }

        HandleHunger();
        FindClosestFood();
        NavigateTank();
        HandleVisualFacing();
    }

    void HandleHunger()
    {
        currentHunger += Time.deltaTime;

        if (currentHunger >= maxHunger)
        {
            Die();
        }
        else if (currentHunger >= hungerWarningTime)
        {
            float starvationProgress = (currentHunger - hungerWarningTime) / (maxHunger - hungerWarningTime);
            spriteRenderer.color = Color.Lerp(originalColor, sickColor, starvationProgress);
        }
        else
        {
            spriteRenderer.color = originalColor;
        }
    }

    void FindClosestFood()
    {
        if (currentFoodTarget != null) return;

        FishFood[] availableFood = FindObjectsByType<FishFood>(FindObjectsSortMode.None);
        
        float closestDistance = Mathf.Infinity;
        FishFood nearestFood = null;

        foreach (FishFood food in availableFood)
        {
            if (food.transform.position.y <= food.floorYValue || food.isTargeted) continue;

            float distanceToFood = Vector3.Distance(transform.position, food.transform.position);
            if (distanceToFood < closestDistance)
            {
                closestDistance = distanceToFood;
                nearestFood = food;
            }
        }

        if (nearestFood != null)
        {
            currentFoodTarget = nearestFood;
            currentFoodTarget.isTargeted = true; 
            isChasingFood = true;
        }
        else
        {
            isChasingFood = false;
        }
    }

    void NavigateTank()
    {
        float currentSpeed = swimSpeed;
        
        if (isChasingFood && currentFoodTarget != null)
        {
            targetDestination = currentFoodTarget.transform.position;
            currentSpeed = swimSpeed * rushSpeedMultiplier;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetDestination, currentSpeed * Time.deltaTime);

        float distanceToTarget = Vector3.Distance(transform.position, targetDestination);
        
        if (isChasingFood && currentFoodTarget != null)
        {
            if (distanceToTarget < 0.2f)
            {
                Destroy(currentFoodTarget.gameObject); 
                currentFoodTarget = null;
                isChasingFood = false;
                currentHunger = 0f; 

                GrowFish();
                
                // --- SPAWN THE SPECIES-SPECIFIC REWARD ---
                SpawnMoneyReward();

                PickNewDestination(); 
            }
        }
        else if (distanceToTarget < closeEnoughThreshold)
        {
            PickNewDestination();
        }
    }

    // --- CLEANED UP: ADJUST DIRECTION TRACKING ONLY ---
    void HandleVisualFacing()
    {
        if (spriteRenderer == null) return;

        bool directionChanged = false;

        if (targetDestination.x > transform.position.x && facingDirectionSign != 1f)
        {
            facingDirectionSign = 1f;
            directionChanged = true;
        }
        else if (targetDestination.x < transform.position.x && facingDirectionSign != -1f)
        {
            facingDirectionSign = -1f;
            directionChanged = true;
        }

        // Only recalculate the transform vector if the fish actually turns around
        if (directionChanged)
        {
            UpdateFishScale();
        }
    }

    void GrowFish()
    {
        currentScaleModifier += growthPerBite;
        if (currentScaleModifier > maxScale)
        {
            currentScaleModifier = maxScale;
        }

        UpdateFishScale();
    }

    // --- FIXED: STRETCH-PROOF SCALE CALCULATOR ---
    void UpdateFishScale()
    {
        // Instead of hardcoded numbers, we use the custom base scale values 
        // you defined in the editor, ensuring X and Y expand perfectly together!
        transform.localScale = new Vector3(
            baseScale.x * currentScaleModifier * facingDirectionSign, 
            baseScale.y * currentScaleModifier, 
            baseScale.z
        );
    }

    // --- UPDATED: CALCULATE PAYOUT STRICTLY BY SPECIES TYPE ---
    void SpawnMoneyReward()
    {
        if (moneyDropPrefab != null)
        {
            int calculatedPayout = 5; 

            switch (species)
            {
                case FishType.Goldfish:
                    calculatedPayout = 15;
                    break;
                case FishType.Tetra:
                    calculatedPayout = 8;
                    break;
                case FishType.Angelfish:
                    calculatedPayout = 45;
                    break;
                case FishType.Shark:
                    calculatedPayout = 120;
                    break;
            }

            GameObject groundCoin = Instantiate(moneyDropPrefab, transform.position, Quaternion.identity);
            
            MoneyDropItem coinScript = groundCoin.GetComponent<MoneyDropItem>();
            if (coinScript != null)
            {
                coinScript.cashValue = calculatedPayout;
            }
        }
    }

    void PickNewDestination()
    {
        float targetX = Random.Range(minBounds.x, maxBounds.x);
        float targetY = Random.Range(minBounds.y, maxBounds.y);
        targetDestination = new Vector3(targetX, targetY, 0f);
    }

    void Die()
    {
        isDead = true;
        spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
        transform.rotation = Quaternion.Euler(0, 0, 180f);

        AquariumManager manager = FindFirstObjectByType<AquariumManager>();
        if (manager != null)
        {
            manager.DeductPlantedCash(15); 
        }

        if (currentFoodTarget != null)
        {
            currentFoodTarget.isTargeted = false;
        }

        Destroy(gameObject, 8f);
    }

    void FloatToSurface()
    {
        if (transform.position.y < maxBounds.y)
        {
            transform.Translate(Vector3.down * 0.8f * Time.deltaTime, Space.Self); 
        }
    }
}