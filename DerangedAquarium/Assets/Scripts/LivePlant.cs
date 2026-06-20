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
    
    // --- NEW LOGIC FOR ANTI-SPAM LOGS ---
    private bool lastFrameHealthy = true;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        algaeManager = FindFirstObjectByType<AlgaeManager>();
        
        if (spriteRenderer != null) spriteRenderer.color = healthyColor;
    }

    void Update()
    {
        if (algaeManager == null) return;

        bool foundAlgaeChoke = false;
        
        AlgaeNode[] allNodes = FindObjectsByType<AlgaeNode>(FindObjectsSortMode.None);
        foreach (AlgaeNode node in allNodes)
        {
            float distance = Vector3.Distance(transform.position, node.transform.position);
            if (distance <= searchRadius && node.currentAlgaeLevel > algaeChokeThreshold)
            {
                foundAlgaeChoke = true;
                break;
            }
        }

        isHealthy = !foundAlgaeChoke;

        // --- ANTI-SPAM CONDITIONAL DEBUG LOGS ---
        // Only print a console log the exact single frame the health state shifts!
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
            
            lastFrameHealthy = isHealthy; // Save the updated state
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