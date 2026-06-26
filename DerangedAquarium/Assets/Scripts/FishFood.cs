using UnityEngine;

public class FishFood : MonoBehaviour
{
    public float fallSpeed = 1.0f;
    public float floorYValue = -4.0f; 
    public float lifeTimeAfterFloor = 5.0f; 

    [HideInInspector] public bool isTargeted = false;

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
            transform.Translate(Vector2.down * fallSpeed * Time.deltaTime);
        }
        else
        {
            Destroy(gameObject, lifeTimeAfterFloor);
        }
    }
}