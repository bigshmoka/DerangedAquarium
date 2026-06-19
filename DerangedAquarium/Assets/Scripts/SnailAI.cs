using UnityEngine;

public class SnailAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float crawlSpeed = 0.5f; // Snails are slow and steady!
    public float floorY = -3.5f;    // The base floor height of your tank

    [Header("Cleaning Settings")]
    public float cleaningPower = 0.15f; // How much algae it cleans per second
    public float eatInterval = 0.5f;    // Time between bites

    private AlgaeManager algaeManager;
    private AlgaeNode currentTargetNode;
    private Vector3 targetPosition;
    private float eatTimer = 0f;

    void Start()
    {
        // Automatically find the AlgaeManager in the tank
        algaeManager = FindFirstObjectByType<AlgaeManager>();
        PickRandomFloorTarget();
    }

    void Update()
    {
        if (algaeManager == null) return;

        // If we don't have a task, check if any walls are dirty
        if (currentTargetNode == null || currentTargetNode.currentAlgaeLevel <= 0.05f)
        {
            currentTargetNode = algaeManager.GetDirtiestAlgaeNode();

            if (currentTargetNode != null)
            {
                // We found a dirty spot! Head toward its X position on the floor first
                targetPosition = new Vector3(currentTargetNode.transform.position.x, floorY, 0f);
            }
            else if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                // If tank is entirely clean, just wander around the floor
                PickRandomFloorTarget();
            }
        }

        // Handle moving or cleaning
        if (currentTargetNode != null && Vector3.Distance(transform.position, currentTargetNode.transform.position) < 0.5f)
        {
            // We have arrived at the dirty node! Start munching
            StartCleaning();
        }
        else
        {
            // Move toward our current target position
            NavigateToTarget();
        }
    }

    void NavigateToTarget()
    {
        Vector3 currentDestination = targetPosition;

        // CLEVER CRAWLING LOGIC: 
        // If we are targeting a wall, we walk along the floor until we match its X position,
        // then we climb vertically up to the node's actual height!
        if (currentTargetNode != null && Mathf.Abs(transform.position.x - currentTargetNode.transform.position.x) < 0.1f)
        {
            currentDestination = currentTargetNode.transform.position;
        }

        transform.position = Vector3.MoveTowards(transform.position, currentDestination, crawlSpeed * Time.deltaTime);
        HandleFacingDirection(currentDestination);
    }

    void StartCleaning()
    {
        eatTimer += Time.deltaTime;

        if (eatTimer >= eatInterval)
        {
            if (currentTargetNode != null)
            {
                // Slowly chew away the transparency of the green overlay
                currentTargetNode.CleanAlgae(cleaningPower * eatInterval);
            }
            eatTimer = 0f;
        }
    }

    void PickRandomFloorTarget()
    {
        // Wander between the left and right safe zones of the floor
        float randomX = Random.Range(-6.5f, 6.5f);
        targetPosition = new Vector3(randomX, floorY, 0f);
    }

    void HandleFacingDirection(Vector3 destination)
    {
        // Simple sprite flipping so it faces where it's crawling
        if (destination.x > transform.position.x + 0.01f)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (destination.x < transform.position.x - 0.01f)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }
}