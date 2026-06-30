using UnityEngine;

public class ExhibitTicketGate : MonoBehaviour
{
    [Header("Physical Visitor Spawning Engine")]
    [Tooltip("Drag your blue placeholder Capsule Prefab asset file here.")]
    public GameObject visitorPrefab;
    [Tooltip("Create a blank empty game object on the street sidewalk out past your front doors and drag it here.")]
    public Transform spawnPointAnchor;

    [Header("Lobby Arrival Scaling (Slower Spawning)")]
    [Tooltip("How many seconds between new visitor arrival ticks? (e.g., 25-30 seconds feels very clean)")]
    public float arrivalIntervalSec = 25.0f;
    
    [Tooltip("The minimum number of people that can arrive per tick.")]
    public int minGroupSize = 1;
    [Tooltip("The maximum number of people that can arrive per tick. Set both to 1 for a strict solo trickle!")]
    public int maxGroupSize = 2;

    private float spawnTimer = 0f;

    void Update()
    {
        if (visitorPrefab == null || spawnPointAnchor == null) return;

        // Run the background arrival clock loop
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= arrivalIntervalSec)
        {
            spawnTimer = 0f;
            SpawnPhysicalVisitorNPC();
        }
    }

    private void SpawnPhysicalVisitorNPC()
    {
        // Pick a random clustering size based on your new inspector constraints
        int groupSize = Random.Range(minGroupSize, maxGroupSize + 1);

        for (int i = 0; i < groupSize; i++)
        {
            // Generate minor positional variations so multiple spawned shapes don't fuse inside each other
            Vector3 scatteredOffset = new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));
            Vector3 finalSpawnLocation = spawnPointAnchor.position + scatteredOffset;

            // Materialize the shape model out on the street mesh bounds
            GameObject spawnedVisitor = Instantiate(visitorPrefab, finalSpawnLocation, Quaternion.identity);
            spawnedVisitor.name = $"Exhibit_Visitor_{Random.Range(100, 999)}";

            // Attach our pathfinding behavioral mind logic component down onto the clone dynamically
            spawnedVisitor.AddComponent<VisitorAI>().viewDuration = Random.Range(6f, 10f);
        }
    }

    /// <summary>
    /// CENTRALIZED LOBBY TRANSACTION: Fires instantly the exact millisecond a physical visitor capsule 
    /// box collider crosses your turnstile gate's invisible trigger box zone mesh fields.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;

        VisitorAI activeVisitor = other.GetComponent<VisitorAI>() ?? other.GetComponentInParent<VisitorAI>();

        if (activeVisitor != null)
        {
            if (ExhibitPrestigeManager.Instance != null && GlobalEconomyManager.Instance != null)
            {
                int ticketIncome = ExhibitPrestigeManager.Instance.currentEntranceFee;
                GlobalEconomyManager.Instance.AddMoney(ticketIncome);
                Debug.Log($"<color=#66FF66>[Lobby Gate]</color> {other.gameObject.name} crossed turnstiles! Deposited Entrance Ticket Price: <b>+${ticketIncome}</b>");
            }
        }
    }
}