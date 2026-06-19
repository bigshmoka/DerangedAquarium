using UnityEngine;

public class SnailAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float crawlSpeed = 0.5f; // Snails are slow and steady!
    public float floorY = -3.5f;    // The base floor height of your tank

    [Header("Cleaning Settings")]
    public float cleaningPower = 0.15f; // How much algae it cleans per second
    public float eatInterval = 0.5f;    // Time between bites

    [Header("Pop Off Customization")]
    [Range(0, 100)] 
    public int popOffChancePercentage = 20; // Change your % chance directly in the inspector!
    public float minPopOffDistance = 3.0f;  // Minimum distance it can pop out
    public float maxPopOffDistance = 5.0f;  // Maximum distance it can pop out
    public float popOffLaunchSpeed = 1.5f;  // How fast it moves away from the wall during the pop phase
    public float floatFallSpeed = 0.8f;     // Sinking speed
    public float tumbleRotationSpeed = 60f; // Spin speed while falling

    [Header("Scraping Animation")]
    public float scrapingSpeed = 2.0f;     // Slowed down default value for a better look!
    public float scrapingDistance = 0.25f; // How far it moves from the center of the algae node

    private AlgaeManager algaeManager;
    private AlgaeNode currentTargetNode;
    private Vector3 targetPosition;
    private float eatTimer = 0f;

    // --- STRETCH PREVENTION ---
    private Vector3 originalScale;

    // --- GROUNDED PATHFINDING ---
    private Vector3 currentWayPoint;
    private bool hasWayPoint = false;

    // --- FLOAT & LAUNCH STATES ---
    private bool isFloatingDown = false;
    private bool isPoppingOut = false;      
    private Vector3 popTargetPosition;      
    private float currentTumbleAngle = 0f;

    // --- SMOOTH LANDING SYSTEM ---
    private float landingZoneThreshold = 0.5f; 
    private Quaternion rotationAtLandingZoneStart;
    private float landingProgress = 0f;
    private bool enteredLandingZone = false;

    void Start()
    {
        originalScale = transform.localScale;

        // Snap the snail's height directly to the floor instantly so it doesn't float in the center
        transform.position = new Vector3(transform.position.x, floorY, transform.position.z);

        algaeManager = FindFirstObjectByType<AlgaeManager>();
        PickRandomFloorTarget();
    }

    void Update()
    {
        // 1. IF FLOATING/POPPING: Handle special animations, bypass standard loops
        if (isFloatingDown)
        {
            HandleFloatingDownEffect();
            return; 
        }

        if (algaeManager == null) return;

        // 2. TARGET ACQUISITION & INSPECTOR-BASED DICE ROLL
        if (currentTargetNode == null || currentTargetNode.currentAlgaeLevel <= 0.05f)
        {
            // If we WERE just eating a node high up on a side wall, roll the custom percentage chance
            if (currentTargetNode != null && Mathf.Abs(transform.position.y - floorY) > 0.5f)
            {
                int randomRoll = Random.Range(0, 100);

                if (randomRoll < popOffChancePercentage) 
                {
                    Debug.Log($"<color=cyan>[Snail Pop Off]</color> SUCCESS! Rolled a {randomRoll} (Needed under {popOffChancePercentage}). Detaching from wall!");
                    InitiatePopOffFloat();
                    return; 
                }
                else
                {
                    Debug.Log($"<color=orange>[Snail Pop Off]</color> MISSED. Rolled a {randomRoll} (Needed under {popOffChancePercentage}). Returning normally.");
                }
            }

            currentTargetNode = algaeManager.GetDirtiestAlgaeNode();

            if (currentTargetNode != null)
            {
                targetPosition = currentTargetNode.transform.position;
            }
            else if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                PickRandomFloorTarget();
            }
            
            CalculateStickyPath();
        }

        // 3. ACTION EXECUTION
        if (currentTargetNode != null && Vector3.Distance(transform.position, currentTargetNode.transform.position) < 0.5f)
        {
            StartCleaning();
        }
        else
        {
            NavigateToTarget();
        }
    }

    void CalculateStickyPath()
    {
        if (Mathf.Abs(transform.position.y - floorY) > 0.1f)
        {
            currentWayPoint = new Vector3(transform.position.x, floorY, 0f);
            hasWayPoint = true;
        }
        else
        {
            currentWayPoint = new Vector3(targetPosition.x, floorY, 0f);
            hasWayPoint = true;
        }
    }

    void NavigateToTarget()
    {
        Vector3 activeDestination = hasWayPoint ? currentWayPoint : targetPosition;
        Vector3 moveDirection = (activeDestination - transform.position).normalized;

        transform.position = Vector3.MoveTowards(transform.position, activeDestination, crawlSpeed * Time.deltaTime);

        if (hasWayPoint && Vector3.Distance(transform.position, currentWayPoint) < 0.05f)
        {
            if (Mathf.Abs(transform.position.y - floorY) < 0.1f && Mathf.Abs(transform.position.x - targetPosition.x) > 0.1f)
            {
                currentWayPoint = new Vector3(targetPosition.x, floorY, 0f);
            }
            else
            {
                hasWayPoint = false;
            }
        }

        // Pass false & clean up direction input
        HandleSnailOrientation(moveDirection, false, 0f);
    }

    // --- UPGRADED: SMOOTH INTERPOLATION ENTRY + WAVE VELOCITY FLIPPING ---
    void StartCleaning()
    {
        hasWayPoint = false; 
        
        eatTimer += Time.deltaTime;
        if (eatTimer >= eatInterval)
        {
            if (currentTargetNode != null)
            {
                currentTargetNode.CleanAlgae(cleaningPower * eatInterval);
            }
            eatTimer = 0f;
        }

        if (currentTargetNode != null)
        {
            Vector3 targetNodePos = currentTargetNode.transform.position;
            
            // Generate the wave offset
            float waveOffset = Mathf.Sin(Time.time * scrapingSpeed) * scrapingDistance;
            
            // Math trick: Using the Cosine wave tells us the EXACT movement direction (velocity) of the sine wave!
            // Positive value = moving Right/Up, Negative value = moving Left/Down
            float waveDirectionVelocity = Mathf.Cos(Time.time * scrapingSpeed);

            bool isOnFloor = Mathf.Abs(targetNodePos.y - floorY) < 0.5f;
            Vector3 calculatedScrapePosition;

            if (isOnFloor)
            {
                // FLOOR: Calculate target coordinate along the base line
                calculatedScrapePosition = new Vector3(targetNodePos.x + waveOffset, floorY, transform.position.z);
                
                // Move towards it smoothly instead of hard snapping
                transform.position = Vector3.MoveTowards(transform.position, calculatedScrapePosition, crawlSpeed * Time.deltaTime);
                
                // Pass velocity direction so it faces the way it is sliding horizontally
                HandleSnailOrientation(Vector3.right, true, waveDirectionVelocity);
            }
            else
            {
                // GLASS WALLS: Calculate target coordinate along the glass pane
                calculatedScrapePosition = new Vector3(transform.position.x, targetNodePos.y + waveOffset, transform.position.z);
                
                // Move towards it smoothly instead of hard snapping
                transform.position = Vector3.MoveTowards(transform.position, calculatedScrapePosition, crawlSpeed * Time.deltaTime);
                
                // Pass velocity direction so it faces the way it is sliding vertically
                HandleSnailOrientation(Vector3.up, true, waveDirectionVelocity);
            }
        }
    }

    void InitiatePopOffFloat()
    {
        isFloatingDown = true;
        isPoppingOut = true; 
        enteredLandingZone = false; 
        currentTargetNode = null;
        hasWayPoint = false;

        float pushDirectionX = (transform.position.x > 0f) ? -1f : 1f;
        
        float chosenDistance = Random.Range(minPopOffDistance, maxPopOffDistance);
        Debug.Log($"<color=teal>[Snail Pop Off]</color> Calculated launch distance: <b>{chosenDistance:F2} units</b>.");

        popTargetPosition = new Vector3(transform.position.x + (pushDirectionX * chosenDistance), transform.position.y, transform.position.z);

        transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        
        currentTumbleAngle = 90f; 
        transform.rotation = Quaternion.Euler(0f, 0f, currentTumbleAngle);
    }

    void HandleFloatingDownEffect()
    {
        if (isPoppingOut)
        {
            currentTumbleAngle += tumbleRotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, 0f, currentTumbleAngle);

            transform.position = Vector3.MoveTowards(transform.position, popTargetPosition, popOffLaunchSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, popTargetPosition) < 0.05f)
            {
                isPoppingOut = false;
            }
        }
        else
        {
            Vector3 newPos = transform.position;
            newPos.y -= floatFallSpeed * Time.deltaTime;

            float heightAboveFloor = newPos.y - floorY;

            if (heightAboveFloor <= landingZoneThreshold)
            {
                if (!enteredLandingZone)
                {
                    rotationAtLandingZoneStart = transform.rotation;
                    landingProgress = 0f;
                    enteredLandingZone = true;
                    transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
                }

                landingProgress = 1f - (heightAboveFloor / landingZoneThreshold);
                transform.rotation = Quaternion.Lerp(rotationAtLandingZoneStart, Quaternion.identity, landingProgress);
            }
            else
            {
                currentTumbleAngle += tumbleRotationSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Euler(0f, 0f, currentTumbleAngle);
            }

            if (newPos.y <= floorY)
            {
                newPos.y = floorY;
                isFloatingDown = false; 

                transform.rotation = Quaternion.identity;
                PickRandomFloorTarget();
                CalculateStickyPath();
            }

            transform.position = newPos;
        }
    }

    void PickRandomFloorTarget()
    {
        float randomX = Random.Range(-6.5f, 6.5f);
        targetPosition = new Vector3(randomX, floorY, 0f);
    }

    // --- UPGRADED DIRECTION MATRIX: SUPPORTS VELOCITY FLIPPING ---
    void HandleSnailOrientation(Vector3 direction, bool isEating, float waveVelocity)
    {
        if (Mathf.Abs(direction.y) > 0.7f)
        {
            if (transform.position.x > 0f)
            {
                // --- RIGHT WALL ---
                if (isEating)
                {
                    // Flip up and down dynamically depending on wave velocity direction
                    float lookDirection = (waveVelocity >= 0f) ? 1f : -1f;
                    transform.localScale = new Vector3(lookDirection * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
                    transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                }
                else
                {
                    bool isMovingUp = direction.y > 0;
                    if (isMovingUp)
                    {
                        transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
                        transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                    }
                    else
                    {
                        transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
                        transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                    }
                }
            }
            else
            {
                // --- LEFT WALL ---
                if (isEating)
                {
                    // Flip up and down dynamically depending on wave velocity direction (inverted matrix for left wall)
                    float lookDirection = (waveVelocity >= 0f) ? -1f : 1f;
                    transform.localScale = new Vector3(lookDirection * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
                    transform.rotation = Quaternion.Euler(0f, 0f, -90f);
                }
                else
                {
                    bool isMovingUp = direction.y > 0;
                    if (isMovingUp)
                    {
                        transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
                        transform.rotation = Quaternion.Euler(0f, 0f, -90f);
                    }
                    else
                    {
                        transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
                        transform.rotation = Quaternion.Euler(0f, 0f, -90f);
                    }
                }
            }
            return;
        }

        // --- STANDARD HORIZONTAL FLOORS ---
        transform.rotation = Quaternion.identity;

        if (isEating)
        {
            // Flip left and right dynamically depending on wave velocity direction
            float lookDirection = (waveVelocity >= 0f) ? 1f : -1f;
            transform.localScale = new Vector3(lookDirection * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            return;
        }

        if (direction.x > 0.01f)
        {
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        else if (direction.x < -0.01f)
        {
            transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }
}