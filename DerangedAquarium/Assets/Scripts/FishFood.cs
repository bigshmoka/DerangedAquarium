using UnityEngine;

public class FishFood : MonoBehaviour
{
    public float fallSpeed = 1.0f;
    public float floorYValue = -4.0f; 
    public float lifeTimeAfterFloor = 5.0f; 

    [HideInInspector] public bool isTargeted = false;

    // REMOVED: The independent Start() visibility check.
    // The AquariumManager will now handle toggling this renderer.

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