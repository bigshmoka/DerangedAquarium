using UnityEngine;

public class MoneyDropItem : MonoBehaviour
{
    [HideInInspector] public int cashValue = 5; // Configured dynamically by the species of the fish that spawned it
    public float fallSpeed = 0.5f;
    public float floorYValue = -3.5f; // The bottom boundary line where the coin stops falling

    void Update()
    {
        // Continuously drift the coin downward toward the floor of the aquarium
        if (transform.position.y > floorYValue)
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);
        }
    }

    // --- HOVER TO COLLECT MECHANIC ---
    // This triggers automatically the exact moment the cursor glides over the 2D Collider
    void OnMouseEnter()
    {
        AquariumManager manager = FindFirstObjectByType<AquariumManager>();
        if (manager != null)
        {
            // Add the dynamic species reward to the global wallet balance
            manager.totalMoney += cashValue;
            
            // Forces the UI text component layout to refresh immediately
            manager.DeductPlantedCash(0); 
            
            // Cleanly remove the coin from the simulation loop
            Destroy(gameObject);
        }
    }
}