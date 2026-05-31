using UnityEngine;

public class NaturalFishAI : MonoBehaviour
{
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

    private Color originalColor;
    private Color sickColor = new Color(0.8f, 0.8f, 0.3f, 1.0f); 

    private Vector3 targetDestination;
    private float closeEnoughThreshold = 0.3f;
    
    private FishFood currentFoodTarget;
    private bool isChasingFood = false;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.rotation = Quaternion.identity;

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

        if (targetDestination.x > transform.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (targetDestination.x < transform.position.x)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
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

    void UpdateFishScale()
    {
        float baseWidth = 1.5f;
        float baseHeight = 0.6f;
        float directionSign = transform.localScale.x < 0 ? -1f : 1f;

        transform.localScale = new Vector3(
            baseWidth * currentScaleModifier * directionSign, 
            baseHeight * currentScaleModifier, 
            1f
        );
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 topL = new Vector3(minBounds.x, maxBounds.y, 0);
        Vector3 topR = new Vector3(maxBounds.x, maxBounds.y, 0);
        Vector3 botL = new Vector3(minBounds.x, minBounds.y, 0);
        Vector3 botR = new Vector3(maxBounds.x, minBounds.y, 0);

        Gizmos.DrawLine(topL, topR);
        Gizmos.DrawLine(topR, botR);
        Gizmos.DrawLine(botR, botL);
        Gizmos.DrawLine(botL, topL);
    }
}