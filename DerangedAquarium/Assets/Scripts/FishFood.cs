using UnityEngine;

public class FishFood : MonoBehaviour
{
    public float fallSpeed = 1.0f;
    public float floorYValue = -4.0f; 
    public float lifeTimeAfterFloor = 5.0f; 

    // This flag tells the fish if another fish is already about to eat it
    [HideInInspector] public bool isTargeted = false;

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