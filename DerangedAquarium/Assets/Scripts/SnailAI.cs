using UnityEngine;

public class SnailAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float crawlSpeed = 0.5f; 
    public float floorY = -3.5f;    

    [Header("Cleaning Settings")]
    public float cleaningPower = 0.15f; 
    public float eatInterval = 0.5f;    
    public int coinsPerSwipe = 5;       

    [Header("Pop Off Customization")]
    [Range(0, 100)] 
    public int popOffChancePercentage = 20; 
    public float minPopOffDistance = 3.0f;  
    public float maxPopOffDistance = 5.0f;  
    public float popOffLaunchSpeed = 1.5f;  
    public float floatFallSpeed = 0.8f;     
    public float tumbleRotationSpeed = 60f; 

    [Header("Scraping Animation")]
    public float scrapingSpeed = 2.0f;     
    public float scrapingDistance = 0.25f; 

    private AlgaeManager algaeManager;
    private AlgaeNode currentTargetNode;
    private Vector3 targetPosition;
    private float eatTimer = 0f;

    private AquariumManager aquariumManager;

    // --- FIXED: EXPOSED DATA SCOPES FOR CONSOLE STORAGE STORAGE LINKAGES ---
    [HideInInspector] public Vector3 originalScale;

    private Vector3 currentWayPoint;
    private bool hasWayPoint = false;

    private bool isFloatingDown = false;
    private bool isPoppingOut = false;      
    private Vector3 popTargetPosition;      
    private float currentTumbleAngle = 0f;

    private float landingZoneThreshold = 0.5f; 
    private Quaternion rotationAtLandingZoneStart;
    private float landingProgress = 0f;
    private bool enteredLandingZone = false;

    void Start()
    {
        if (originalScale == Vector3.zero)
        {
            originalScale = transform.localScale;
        }

        transform.position = new Vector3(transform.position.x, floorY, transform.position.z);
        
        // FIXED MULTI-TANK MANAGER HOOK: Locates the local sibling manager components
        aquariumManager = GetComponentInParent<AquariumManager>();
        if (aquariumManager != null)
        {
            algaeManager = aquariumManager.algaeManager;
            if (algaeManager == null) algaeManager = aquariumManager.GetComponentInChildren<AlgaeManager>();
        }
        else
        {
            algaeManager = FindFirstObjectByType<AlgaeManager>();
            aquariumManager = FindFirstObjectByType<AquariumManager>();
        }

        PickRandomFloorTarget();
    }

    void Update()
    {
        if (isFloatingDown)
        {
            HandleFloatingDownEffect();
            return; 
        }

        if (algaeManager == null) return;

        if (currentTargetNode == null || currentTargetNode.currentAlgaeLevel <= 0.05f)
        {
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

        HandleSnailOrientation(moveDirection, false, 0f);
    }

    void StartCleaning()
    {
        hasWayPoint = false; 
        
        eatTimer += Time.deltaTime;
        if (eatTimer >= eatInterval)
        {
            if (currentTargetNode != null)
            {
                currentTargetNode.CleanAlgae(cleaningPower * eatInterval);

                if (aquariumManager != null)
                {
                    aquariumManager.totalMoney += coinsPerSwipe;
                    aquariumManager.UpdateMoneyUI(); 
                }
            }
            eatTimer = 0f;
        }

        if (currentTargetNode != null)
        {
            Vector3 targetNodePos = currentTargetNode.transform.position;
            
            float waveOffset = Mathf.Sin(Time.time * scrapingSpeed) * scrapingDistance;
            float waveDirectionVelocity = Mathf.Cos(Time.time * scrapingSpeed);

            bool isOnFloor = Mathf.Abs(targetNodePos.y - floorY) < 0.5f;
            Vector3 calculatedScrapePosition;

            if (isOnFloor)
            {
                calculatedScrapePosition = new Vector3(targetNodePos.x + waveOffset, floorY, transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, calculatedScrapePosition, crawlSpeed * Time.deltaTime);
                HandleSnailOrientation(Vector3.right, true, waveDirectionVelocity);
            }
            else
            {
                calculatedScrapePosition = new Vector3(transform.position.x, targetNodePos.y + waveOffset, transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, calculatedScrapePosition, crawlSpeed * Time.deltaTime);
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

        float parentX = transform.parent != null ? transform.parent.position.x : 0f;
        float pushDirectionX = (transform.position.x > parentX) ? -1f : 1f;
        
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
        // --- FIXED SNAIL MULTI-TANK FLOOR BOUNDARY ---
        float parentX = transform.parent != null ? transform.parent.position.x : 0f;
        float randomX = parentX + Random.Range(-6.5f, 6.5f);
        targetPosition = new Vector3(randomX, floorY, 0f);
    }

    void HandleSnailOrientation(Vector3 direction, bool isEating, float waveVelocity)
    {
        if (Mathf.Abs(direction.y) > 0.7f)
        {
            float checkThresholdX = transform.parent != null ? transform.parent.position.x : 0f;
            if (transform.position.x > checkThresholdX)
            {
                if (isEating)
                {
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
                if (isEating)
                {
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

        transform.rotation = Quaternion.identity;

        if (isEating)
        {
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