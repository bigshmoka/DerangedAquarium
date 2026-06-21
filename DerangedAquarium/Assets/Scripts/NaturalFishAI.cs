using UnityEngine;

public class NaturalFishAI : MonoBehaviour
{
    // --- CONFIGURABLE FISH TYPES ---
    public enum FishType { Goldfish, Tetra, Angelfish, Shark, Pufferfish }

    [Header("Species Settings")]
    public FishType species = FishType.Goldfish; 

    [Header("Movement Tweaks")]
    public float swimSpeed = 1.2f;
    public float rushSpeedMultiplier = 1.5f; 

    [Header("Tank Boundaries (Safe Zones)")]
    public Vector2 minBounds = new Vector2(-7.5f, -3.5f);
    public Vector2 maxBounds = new Vector2(7.5f, 3.5f);

    [Header("Hunger & Survival Settings")]
    public float maxHunger = 30f;       
    public float hungerWarningTime = 15f; 
    [SerializeField] private float currentHunger = 0f; 
    private bool isDead = false;

    [Header("Satiety Settings")]
    [Tooltip("How many seconds a fish stays full and ignores food after eating.")]
    public float fullDuration = 10f; 
    [SerializeField] private bool isFull = false;
    private float fullnessTimer = 0f;

    [Header("Growth Settings")]
    public float startingScale = 0.5f;     
    public float maxScale = 1.5f;          
    public float growthPerBite = 0.1f;     
    private float currentScaleModifier;

    // --- PUFFERFISH MECHANICS ---
    [Header("Pufferfish Mechanics")]
    public float puffInflationMultiplier = 1.8f; 
    public float puffLerpSpeed = 5f;            
    private float currentPuffFactor = 1f;       

    [Header("Click Interaction Settings")]
    public float clickPuffDuration = 2.0f; 
    private bool isStartledByClick = false;
    private float clickPuffTimer = 0f;

    [Header("Poop Economy")]
    public GameObject moneyDropPrefab; 

    private Color originalColor;
    private Color sickColor = new Color(0.8f, 0.8f, 0.3f, 1.0f); 

    private Vector3 targetDestination;
    private float closeEnoughThreshold = 0.3f;
    
    private FishFood currentFoodTarget;
    private bool isChasingFood = false;
    private SpriteRenderer spriteRenderer;

    private Vector3 baseScale;
    private float facingDirectionSign = 1f;

    // --- ANTI-SPAM LOG COOLDOWN TIMER ---
    private float logCooldownTimer = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.rotation = Quaternion.identity;

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
        HandleFullness();   
        FindClosestFood();
        HandleClickTimer(); 
        HandleInflation(); 
        NavigateTank();
        HandleVisualFacing();
    }

    void HandleHunger()
    {
        // If the fish is completely full, freeze hunger progression at 0
        if (isFull)
        {
            currentHunger = 0f;
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
            return;
        }

        // Gather ecosystem dynamic buffering reduction from healthy plants in scene
        LivePlant[] allPlants = FindObjectsByType<LivePlant>(FindObjectsSortMode.None);
        float totalHungerReduction = 0f;

        foreach (LivePlant plant in allPlants)
        {
            if (plant.isHealthy)
            {
                totalHungerReduction += plant.hungerSlowdownPercent;
            }
        }

        if (totalHungerReduction > 0.75f) totalHungerReduction = 0.75f; 

        float effectiveHungerDrain = Time.deltaTime * (1f - totalHungerReduction);
        currentHunger += effectiveHungerDrain;

        // Anti-spam diagnostic log system
        if (totalHungerReduction >= 0.10f)
        {
            logCooldownTimer += Time.deltaTime;
            if (logCooldownTimer >= 5.0f)
            {
                Debug.Log($"[Ecosystem] {gameObject.name} hunger is accumulating at " +
                          $"<color=cyan>{(1f - totalHungerReduction) * 100f:F0}% speed</color> thanks to healthy plants!");
                logCooldownTimer = 0f; 
            }
        }

        if (currentHunger >= maxHunger)
        {
            Die();
        }
        else if (currentHunger >= hungerWarningTime)
        {
            float starvationProgress = (currentHunger - hungerWarningTime) / (maxHunger - hungerWarningTime);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(originalColor, sickColor, starvationProgress);
            }
        }
        else
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    void HandleFullness()
    {
        if (isFull)
        {
            fullnessTimer -= Time.deltaTime;
            if (fullnessTimer <= 0f)
            {
                isFull = false; // Digestion complete! Fish can hunt for food pellets again
            }
        }
    }

    // --- RE-IMPLEMENTED: INTERACTION TAPS ---
    void OnMouseDown()
    {
        if (species == FishType.Pufferfish && !isDead)
        {
            isStartledByClick = true;
            clickPuffTimer = clickPuffDuration; 
        }
    }

    void HandleClickTimer()
    {
        if (isStartledByClick)
        {
            clickPuffTimer -= Time.deltaTime;
            if (clickPuffTimer <= 0f)
            {
                isStartledByClick = false;
            }
        }
    }

    // --- RE-IMPLEMENTED: SMOOTH INTERPOLATION INFLATION ---
    void HandleInflation()
    {
        if (species == FishType.Pufferfish && (isChasingFood || isStartledByClick))
        {
            currentPuffFactor = Mathf.Lerp(currentPuffFactor, puffInflationMultiplier, Time.deltaTime * puffLerpSpeed);
            UpdateFishScale();
        }
        else if (currentPuffFactor > 1f)
        {
            currentPuffFactor = Mathf.Lerp(currentPuffFactor, 1f, Time.deltaTime * puffLerpSpeed);
            UpdateFishScale();
        }
    }

    void FindClosestFood()
    {
        if (isFull) 
        {
            if (isChasingFood)
            {
                if (currentFoodTarget != null) currentFoodTarget.isTargeted = false;
                currentFoodTarget = null;
                isChasingFood = false;
                PickNewDestination();
            }
            return; 
        }

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
                
                // Engage fullness protection immediately upon digestion
                currentHunger = 0f; 
                isFull = true;
                fullnessTimer = fullDuration; 

                GrowFish();
                SpawnMoneyReward();
                PickNewDestination(); 
            }
        }
        else if (distanceToTarget < closeEnoughThreshold)
        {
            PickNewDestination();
        }
    }

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

    // --- RE-IMPLEMENTED WITH PUFF FACTORS ---
    void UpdateFishScale()
    {
        transform.localScale = new Vector3(
            baseScale.x * currentScaleModifier * facingDirectionSign * currentPuffFactor, 
            baseScale.y * currentScaleModifier * currentPuffFactor, 
            baseScale.z
        );
    }

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
                case FishType.Pufferfish: // Handles puffer economy
                    calculatedPayout = 30;
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