using UnityEngine;

public class MoneyDropItem : MonoBehaviour
{
    [HideInInspector] public int cashValue = 5; 
    public float fallSpeed = 0.5f;
    public float floorYValue = -3.5f; 
    
    [HideInInspector] 
    public bool hasBeenMultiplied = false;

    // --- NEW: CHECK STATE AT BIRTH WORKFLOW ---
    void Start()
    {
        AquariumManager manager = FindFirstObjectByType<AquariumManager>();
        if (manager != null && !manager.isTankVisible)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false; // Spawns invisibly in the background
        }
    }

    void Update()
    {
        if (transform.position.y > floorYValue)
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
        }
    }

    void OnMouseEnter()
    {
        AquariumManager manager = FindFirstObjectByType<AquariumManager>();
        if (manager != null)
        {
            manager.totalMoney += cashValue;
            manager.DeductPlantedCash(0);
            Destroy(gameObject);
        }
    }
}