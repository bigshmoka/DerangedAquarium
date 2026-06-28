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

    public void CleanAlgae(float amount)
    {
        currentAlgaeLevel -= amount;
        if (currentAlgaeLevel < 0f) currentAlgaeLevel = 0f;
        UpdateVisuals();
    }

    void OnMouseEnter()
    {
        AquariumManager manager = GetComponentInParent<AquariumManager>();
        
        if (manager == null)
        {
            Debug.LogError($"<color=red>[Algae Hierarchy Error]</color> Node asset <b>{gameObject.name}</b> can't locate an AquariumManager script component anywhere in its parent tree layout branch!");
            return;
        }

        if (manager.IsSpongeToolActive())
        {
            if (currentAlgaeLevel > 0.05f)
            {
                currentAlgaeLevel -= 0.30f;
                if (currentAlgaeLevel < 0f) currentAlgaeLevel = 0f;
                UpdateVisuals();
                
                // RESTORED: Cleaning Success Metric Diagnostic Log
                Debug.Log($"<color=cyan>[Sponge Scrub]</color> Wiped node <b>{gameObject.name}</b> in container <b>{manager.tankID}</b>! Current dirty accumulation level dropped to: <b>{currentAlgaeLevel:P0}</b>.");

                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.ProgressQuest("clean_algae", 1);
                }
            }
        }
    }

    public void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = currentAlgaeLevel;
            spriteRenderer.color = c;
        }
    }

    public void InitializeAlgaeLevel(float level)
    {
        currentAlgaeLevel = level;
        UpdateVisuals();
    }
}