using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    [Header("Trash Blueprint")]
    [Tooltip("Drag your small placeholder Cube or trash prefab here!")]
    public GameObject trashPrefab;

    [Header("Initial Spawn Settings")]
    [Tooltip("How much trash spawns at the very beginning of the game for the tutorial quest.")]
    public int startingTrashAmount = 5;

    [Header("Continuous Spawning Settings")]
    [Tooltip("How many seconds between new passive trash drops? (e.g., 120 = 2 minutes)")]
    public float spawnInterval = 120f;
    [Tooltip("How many pieces of trash can spawn per interval tick.")]
    public int trashPerInterval = 1;
    [Tooltip("The absolute maximum amount of trash allowed on the floor at once.")]
    public int maxTrashCap = 12;

    [Header("Floor Boundaries (Match your ProBuilder Floor scale!)")]
    public float minX = -5f;
    public float maxX = 5f;
    public float minZ = -5f;
    public float maxZ = 5f;
    [Tooltip("The exact Y height coordinate of your storefront floor mesh.")]
    public float floorY = 0.1f; 

    private Transform containerFolder;
    private float spawnTimer = 0f;

    void Start()
    {
        if (trashPrefab == null)
        {
            Debug.LogWarning("[Trash Spawner] No trash prefab or placeholder assigned! Spawning aborted.");
            return;
        }

        // Generate the structural tracking folder at runtime
        GameObject containerObj = new GameObject("--- SPAWNED TRASH CLUTTER ---");
        containerFolder = containerObj.transform;

        // Verify if the player is currently on the initial cleanup tutorial quest step
        bool isCurrentlyOnCleanupQuest = false;
        if (QuestManager.Instance != null && QuestManager.Instance.activeQuests.Count > 0)
        {
            if (QuestManager.Instance.activeQuests[0].questID == "clean_trash")
            {
                isCurrentlyOnCleanupQuest = true;
            }
        }

        // Only seed the initial dirty storefront disaster if they haven't beaten the tutorial yet
        if (isCurrentlyOnCleanupQuest)
        {
            for (int i = 0; i < startingTrashAmount; i++)
            {
                SpawnSingleTrashItem();
            }
            Debug.Log($"[Trash Spawner] Initialized showroom with {startingTrashAmount} pieces of tutorial clutter.");
        }
        else
        {
            Debug.Log("[Trash Spawner] Cleanup quest completed or inactive. Skipping initial tutorial clutter spawn.");
        }
    }

    void Update()
    {
        if (trashPrefab == null || containerFolder == null) return;

        // Run the background simulation clock
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f; // Reset interval clock

            // Read the real-time child count inside our container folder
            int currentTrashCount = containerFolder.childCount;

            // Enforce our maximum clutter safety threshold
            if (currentTrashCount < maxTrashCap)
            {
                // Determine exactly how many slots are open before hitting the cap limit
                int spawnCount = Mathf.Min(trashPerInterval, maxTrashCap - currentTrashCount);

                for (int i = 0; i < spawnCount; i++)
                {
                    SpawnSingleTrashItem();
                }
                Debug.Log($"[Trash Spawner] Passive interval tick triggered: Spawned {spawnCount} new trash item(s). Current Total: {containerFolder.childCount}");
            }
        }
    }

    private void SpawnSingleTrashItem()
    {
        // Calculate randomized coordinates inside your physical storefront boundaries
        float randomX = Random.Range(minX, maxX);
        float randomZ = Random.Range(minZ, maxZ);
        Vector3 spawnPosition = new Vector3(randomX, floorY, randomZ);

        // Spin the item organically so pieces don't look identically blocky
        Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Instantiate parented to the folder container for exact clean childCount tracking
        GameObject spawnedTrash = Instantiate(trashPrefab, spawnPosition, randomRotation, containerFolder);
        spawnedTrash.name = "Storefront_Clutter_Trash";
    }
}