using UnityEngine;

public class AlgaeNode : MonoBehaviour
{
    [Header("Growth Settings")]
    public float baseGrowthRate = 0.02f;
    private float customGrowthMultiplier;
    
    [Range(0f, 1f)]
    public float currentAlgaeLevel = 0f;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        customGrowthMultiplier = Random.Range(0.7f, 1.4f);
        UpdateVisuals();
    }

    void Update()
    {
        if (currentAlgaeLevel < 1f)
        {
            currentAlgaeLevel += (baseGrowthRate * customGrowthMultiplier) * Time.deltaTime;
            if (currentAlgaeLevel > 1f) currentAlgaeLevel = 1f;

            UpdateVisuals();
        }
    }

    // This handles the automated cleaning from the Snail
    public void CleanAlgae(float amount)
    {
        currentAlgaeLevel -= amount;
        if (currentAlgaeLevel < 0f) currentAlgaeLevel = 0f;

        UpdateVisuals();
    }

    // --- NEW: NO-CLICK HOVER WIPING ---
    // This triggers the EXACT instant the mouse cursor crosses into the wall's collider box
    void OnMouseEnter()
    {
        AquariumManager manager = FindFirstObjectByType<AquariumManager>();
        
        // Check if the manager exists and the Sponge tool is currently toggled ON
        if (manager != null && manager.IsSpongeToolActive())
        {
            // Instantly wipe away 30% of the algae just by sliding the mouse over it!
            currentAlgaeLevel -= 0.30f;
            if (currentAlgaeLevel < 0f) currentAlgaeLevel = 0f;

            UpdateVisuals();
            Debug.Log(gameObject.name + " scrubbed via hover! Current Algae: " + currentAlgaeLevel);
            QuestManager.Instance.ProgressQuest("clean_algae", 1);
        }
    }

    // --- FIXED: EXPOSED AS PUBLIC TO ALLOW DYNAMIC REBINDING RECONSTRUCTIONS ---
    public void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = currentAlgaeLevel;
            spriteRenderer.color = c;
        }
    }

    // --- NEW: REGISTRY INITIALIZATION INJECTION ENTRY ---
    public void InitializeAlgaeLevel(float level)
    {
        currentAlgaeLevel = level;
        UpdateVisuals();
    }
}