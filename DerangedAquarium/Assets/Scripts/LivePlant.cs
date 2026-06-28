using UnityEngine;

public class LivePlant : MonoBehaviour
{
    [Header("Plant Health Settings")]
    public float searchRadius = 1.5f;       
    public float algaeChokeThreshold = 0.5f; 

    [Header("Ecosystem Benefits")]
    [Range(0f, 1f)]
    public float hungerSlowdownPercent = 0.15f; 

    [Header("Visual Feedback")]
    public Color healthyColor = Color.white;
    public Color chokedColor = new Color(0.55f, 0.45f, 0.3f, 1f); 
    public float colorLerpSpeed = 2f;

    [HideInInspector] public bool isHealthy = true;

    private SpriteRenderer spriteRenderer;
    private AlgaeManager algaeManager;
    private bool lastFrameHealthy = true;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // ===================================================================
        // --- FIXED: LOCAL PARENT-BRANCH DISCOVERY ---
        // This ensures the plant only talks to the AlgaeManager that is physically
        // inside its own Tank's folder/hierarchy.
        // ===================================================================
        AquariumManager manager = GetComponentInParent<AquariumManager>();
        if (manager != null)
        {
            algaeManager = manager.algaeManager;
        }
        
        if (spriteRenderer != null) spriteRenderer.color = healthyColor;
    }

    void Update()
    {
        if (algaeManager == null) return;

        bool foundAlgaeChoke = false;
        
        if (algaeManager.algaeNodes != null)
        {
            foreach (AlgaeNode node in algaeManager.algaeNodes)
            {
                if (node == null) continue;

                float distance = Vector3.Distance(transform.position, node.transform.position);
                if (distance <= searchRadius && node.currentAlgaeLevel > algaeChokeThreshold)
                {
                    foundAlgaeChoke = true;
                    break;
                }
            }
        }

        isHealthy = !foundAlgaeChoke;

        if (isHealthy != lastFrameHealthy)
        {
            if (!isHealthy)
            {
                Debug.LogWarning($"[Ecosystem] {gameObject.name} is being CHOKED by nearby algae! (Hunger buff deactivated)");
            }
            else
            {
                Debug.Log($"<color=green>[Ecosystem] {gameObject.name} is now CLEAN and healthy!</color> (Hunger buff restored)");
            }
            lastFrameHealthy = isHealthy; 
        }

        if (spriteRenderer != null)
        {
            Color targetColor = isHealthy ? healthyColor : chokedColor;
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, Time.deltaTime * colorLerpSpeed);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}