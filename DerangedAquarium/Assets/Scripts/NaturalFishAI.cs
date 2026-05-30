using UnityEngine;

public class NaturalFishAI : MonoBehaviour
{
    [Header("Movement Tweaks")]
    public float swimSpeed = 1.2f;
    public float rushSpeedMultiplier = 1.5f; // Fast swim for food chase

    [Header("Tank Boundaries (Safe Zones)")]
    public Vector2 minBounds = new Vector2(-7.5f, -3.5f);
    public Vector2 maxBounds = new Vector2(7.5f, 3.5f);

    private Vector3 targetDestination;
    private float closeEnoughThreshold = 0.3f;
    
    // Food tracking variables
    private FishFood currentFoodTarget;
    private bool isChasingFood = false;

    // Visual Component
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // Grab the sprite renderer attached to this fish so we can flip it
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Force rotation to be completely flat at start
        transform.rotation = Quaternion.identity;
        
        PickNewDestination();
    }

    void Update()
    {
        FindClosestFood();
        NavigateTank();
        HandleVisualFacing();
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

        // Move directly towards the target point without rotating the model
        transform.position = Vector3.MoveTowards(transform.position, targetDestination, currentSpeed * Time.deltaTime);

        // Check if we reached our target
        float distanceToTarget = Vector3.Distance(transform.position, targetDestination);
        
        if (isChasingFood && currentFoodTarget != null)
        {
            if (distanceToTarget < 0.2f)
            {
                Destroy(currentFoodTarget.gameObject); 
                currentFoodTarget = null;
                isChasingFood = false;
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

        // Check if the target destination is to the left or right of the fish
        if (targetDestination.x > transform.position.x)
        {
            // Target is to the right. 
            // ASSUMPTION: Your original artwork sprite faces RIGHT by default.
            spriteRenderer.flipX = false; 
        }
        else if (targetDestination.x < transform.position.x)
        {
            // Target is to the left. Flip the sprite horizontally!
            spriteRenderer.flipX = true;
        }
    }

    void PickNewDestination()
    {
        float targetX = Random.Range(minBounds.x, maxBounds.x);
        float targetY = Random.Range(minBounds.y, maxBounds.y);
        targetDestination = new Vector3(targetX, targetY, 0f);
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