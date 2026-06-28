using UnityEngine;

public class MoneyDropItem : MonoBehaviour
{
    [HideInInspector] public int cashValue = 5; 
    public float fallSpeed = 0.5f;
    public float floorYValue = -3.5f; 
    
    [HideInInspector] 
    public bool hasBeenMultiplied = false;

    // REMOVED: The independent Start() visibility check.
    // The AquariumManager's Toggle2DAquariumVisibility loop will now 
    // control this renderer automatically, preventing the invisible-on-spawn glitch.

    void Update()
    {
        if (transform.position.y > floorYValue)
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
        }
    }

    void OnMouseEnter()
    {
        AquariumManager manager = GetComponentInParent<AquariumManager>();
        if (manager != null)
        {
            manager.totalMoney += cashValue;
            manager.DeductPlantedCash(0);
            Destroy(gameObject);
        }
    }
}