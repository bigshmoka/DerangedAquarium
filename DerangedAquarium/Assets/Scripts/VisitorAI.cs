using UnityEngine;
using UnityEngine.AI; // Pulls in Unity's built-in Navigation framework modules

public class VisitorAI : MonoBehaviour
{
    public enum VisitorState { Entering, WalkingToTank, AdmiringTank, Leaving }
    
    [Header("AI State Machine")]
    public VisitorState currentState = VisitorState.Entering;

    [Header("Movement Reach Parameters")]
    public float stoppingDistance = 0.5f; 
    [Tooltip("How many seconds the capsule stands still looking closely at your fish tank.")]
    public float viewDuration = 6.0f;

    // PUBLIC PROPERTY: Allows other arriving NPCs to read this guest's destination and prevent overlapping
    [HideInInspector] public Vector3 currentTargetSpot;

    private NavMeshAgent agent;
    private Transform targetTank;
    private AquariumManager targetTankManager;
    private Vector3 exitPosition;
    private float viewTimer = 0f;
    private float stuckTimer = 0f; // Tracks bottleneck deadlocks

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.stoppingDistance = this.stoppingDistance;
            
            // Assigns a random priority ranking to prevent equal head-on deadlocks
            agent.avoidancePriority = Random.Range(35, 65);
        }

        // Remember the entrance door location so they know how to exit the facility later
        exitPosition = transform.position;

        // Automatically survey the shop to lock targets onto an aquarium installation frame
        FindNextDestinationExhibit();
    }

    void Update()
    {
        // Tracks if an agent that is supposed to be moving gets trapped or blocked.
        if (agent != null && agent.enabled && !agent.isStopped)
        {
            if (!agent.pathPending && agent.velocity.sqrMagnitude < 0.1f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= 1.5f)
                {
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                }
            }
            else
            {
                stuckTimer = 0f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            }
        }

        switch (currentState)
        {
            case VisitorState.Entering:
            case VisitorState.WalkingToTank:
                HandleNavigationCheck();
                break;

            case VisitorState.AdmiringTank:
                HandleViewingState();
                break;

            case VisitorState.Leaving:
                // Forgiving distance range ensures they clean up reliably outside even in crowded spawn points
                if (agent != null && agent.enabled && !agent.pathPending && agent.remainingDistance <= 1.5f)
                {
                    Debug.Log($"[Museum AI] {gameObject.name} left the facility gallery. Clearing instance.");
                    Destroy(gameObject);
                }
                break;
        }
    }

    private void HandleNavigationCheck()
    {
        if (agent == null || !agent.enabled) return;

        bool arrivedByDistance = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
        bool arrivedByStall = !agent.pathPending && agent.remainingDistance <= 2.5f && agent.velocity.sqrMagnitude < 0.1f;

        if (arrivedByDistance || arrivedByStall)
        {
            if (targetTank != null)
            {
                currentState = VisitorState.AdmiringTank;
                viewTimer = 0f;

                // Pivot the capsule's rotation layout to face the front glass surface cleanly
                Vector3 lookDirection = (targetTank.position - transform.position).normalized;
                lookDirection.y = 0f; // Block any vertical tipping anomalies
                if (lookDirection.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDirection);
                }

                // Disable the agent component entirely while looking at fish so aisles don't jam
                agent.isStopped = true;
                agent.enabled = false; 

                Debug.Log($"[Museum AI] {gameObject.name} successfully locked position near exhibit: {targetTank.name}");
            }
            else
            {
                CommandExitWorkflow();
            }
        }
    }

    private void HandleViewingState()
    {
        viewTimer += Time.deltaTime;
        if (viewTimer >= viewDuration)
        {
            // ===================================================================
            // --- FIXED LIVE XP ROUTER ---
            // Directly receives the un-diluted processed integer XP from your prestige
            // script and flushes it straight onto your level tracker HUD layout!
            // ===================================================================
            if (targetTankManager != null && ExhibitPrestigeManager.Instance != null)
            {
                int earnedXP = ExhibitPrestigeManager.Instance.CalculateTankRatingScore(targetTankManager);

                if (earnedXP > 0)
                {
                    ExhibitPrestigeManager.Instance.AddPrestigePoints(earnedXP);
                    Debug.Log($"<color=cyan>[Exhibit Survey]</color> {gameObject.name} checked out tank <b>{targetTankManager.tankID}</b>! Awarded <b>+{earnedXP} Prestige XP</b>.");
                }
            }

            CommandExitWorkflow();
        }
    }

    private void FindNextDestinationExhibit()
    {
        TankInteraction3D[] availableTankShells = FindObjectsByType<TankInteraction3D>(FindObjectsSortMode.None);
        
        var validTargets = new System.Collections.Generic.List<TankInteraction3D>();
        foreach (var shell in availableTankShells)
        {
            if (shell != null && shell.gameObject.activeInHierarchy)
            {
                validTargets.Add(shell);
            }
        }

        if (validTargets.Count > 0)
        {
            int randomSelectionIndex = Random.Range(0, validTargets.Count);
            TankInteraction3D selectedTargetShell = validTargets[randomSelectionIndex];

            targetTank = selectedTargetShell.transform;

            AquariumManager[] managers = FindObjectsByType<AquariumManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var mgr in managers)
            {
                if (mgr != null && mgr.tankID == selectedTargetShell.tankID)
                {
                    targetTankManager = mgr;
                    break;
                }
            }

            currentState = VisitorState.WalkingToTank;

            // Loops up to 15 times attempting to find a unique viewing angle.
            Vector3 isolatedDestinationPoint = targetTank.position;
            bool foundClearSpot = false;
            VisitorAI[] allVisitorsInScene = FindObjectsByType<VisitorAI>(FindObjectsSortMode.None);

            for (int attempt = 0; attempt < 15; attempt++)
            {
                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float viewingRadiusOffset = Random.Range(1.4f, 2.3f); 
                
                Vector3 standingOffsetVector = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle)) * viewingRadiusOffset;
                Vector3 testPoint = targetTank.position + standingOffsetVector;

                NavMeshHit navigationSurfaceHit;
                if (NavMesh.SamplePosition(testPoint, out navigationSurfaceHit, 3.0f, NavMesh.AllAreas))
                {
                    testPoint = navigationSurfaceHit.position;
                }

                if (attempt == 14)
                {
                    isolatedDestinationPoint = testPoint;
                }

                bool spotIsOccupied = false;
                foreach (VisitorAI otherVisitor in allVisitorsInScene)
                {
                    if (otherVisitor == this || otherVisitor == null) continue;

                    float distanceToBody = Vector3.Distance(otherVisitor.transform.position, testPoint);
                    
                    float distanceToClaimedTarget = 999f;
                    if (otherVisitor.currentState == VisitorState.WalkingToTank || otherVisitor.currentState == VisitorState.Entering)
                    {
                        distanceToClaimedTarget = Vector3.Distance(otherVisitor.currentTargetSpot, testPoint);
                    }

                    if (distanceToBody < 1.1f || distanceToClaimedTarget < 1.1f)
                    {
                        spotIsOccupied = true;
                        break;
                    }
                }

                if (!spotIsOccupied)
                {
                    isolatedDestinationPoint = testPoint;
                    foundClearSpot = true;
                    break;
                }
            }

            if (!foundClearSpot)
            {
                Debug.LogWarning($"[Museum AI] {gameObject.name} could not allocate an isolated personal space around {targetTank.name} (Exhibit is very crowded!). Enforcing a close-proximity backup spot.");
            }

            currentTargetSpot = isolatedDestinationPoint;

            if (agent != null) 
            {
                agent.enabled = true;
                agent.isStopped = false; 
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                if (agent.isOnNavMesh) agent.SetDestination(currentTargetSpot);
            }
        }
        else
        {
            CommandExitWorkflow();
        }
    }

    private void CommandExitWorkflow()
    {
        currentState = VisitorState.Leaving;
        targetTank = null;
        targetTankManager = null;
        stuckTimer = 0f; 
        
        currentTargetSpot = exitPosition;
        
        if (agent != null) 
        {
            agent.enabled = true;
            agent.isStopped = false; 
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            if (agent.isOnNavMesh) agent.SetDestination(exitPosition);
        }
    }
}