using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    [Header("Bubble Spawning Settings")]
    public GameObject bubblePrefab;
    public float openInterval = 10.0f;
    public int bubblesPerBurst = 3;
    public float burstSpreadWidth = 0.5f;

    [Header("Visual Feedback Settings")]
    public float popScaleMultiplier = 1.2f;
    public float visualLerpSpeed = 8f;

    private float timer = 0f;
    private Vector3 originalScale;
    private Vector3 targetScale;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        
        // Start the timer with a bit of a random delay so multiple chests don't open simultaneously
        timer = Random.Range(0f, openInterval * 0.5f);
    }

    void Update()
    {
        // 1. Handle looping timer
        timer += Time.deltaTime;
        if (timer >= openInterval)
        {
            ReleaseBubbles();
            timer = 0f;
        }

        // 2. Smooth visual animation (scales up when opening, pops back to normal size)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * visualLerpSpeed);
        
        if (targetScale != originalScale && Vector3.Distance(transform.localScale, targetScale) < 0.05f)
        {
            targetScale = originalScale; // Reset back down to normal size smoothly
        }
    }

    void ReleaseBubbles()
    {
        if (bubblePrefab == null) return;

        // Visual "pop" animation feedback
        transform.localScale = originalScale * 0.9f; // Slight squish before the pop
        targetScale = originalScale * popScaleMultiplier; // Big expansion

        // FIXED MULTI-TANK HOOK: Fetches the local parent manager folder container instead of global first match
        AquariumManager manager = GetComponentInParent<AquariumManager>();
        Transform container = (manager != null) ? manager.GetBubbleContainer() : null;

        // Spawn a small cluster of bubbles with slight horizontal offsets
        for (int i = 0; i < bubblesPerBurst; i++)
        {
            float randomXOffset = Random.Range(-burstSpreadWidth, burstSpreadWidth);
            Vector3 spawnPosition = new Vector3(transform.position.x + randomXOffset, transform.position.y + 0.3f, transform.position.z);
            
            // Instantiates safely parented inside the hierarchy tracker folder tree branch
            Instantiate(bubblePrefab, spawnPosition, Quaternion.identity, container);
        }
    }
}