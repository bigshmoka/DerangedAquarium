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
        
        // Give this specific wall a slightly random speed so it doesn't match the others!
        customGrowthMultiplier = Random.Range(0.7f, 1.4f);
        
        UpdateVisuals();
    }

    void Update()
    {
        if (currentAlgaeLevel < 1f)
        {
            // Grow independently over time
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

    void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = currentAlgaeLevel;
            spriteRenderer.color = c;
        }
    }
}