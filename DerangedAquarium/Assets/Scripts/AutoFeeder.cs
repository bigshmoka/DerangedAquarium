using UnityEngine;

public class AutoFeeder : MonoBehaviour
{
    [Header("Feeder Settings")]
    [Tooltip("How many seconds between each automatic food drop.")]
    public float dropInterval = 8.0f;
    
    [Tooltip("The vertical offset below the feeder where the food pellet will appear.")]
    public float dropYOffset = -0.5f;

    private GameObject foodPrefab;
    private float timer = 0f;

    void Start()
    {
        // FIXED MULTI-TANK HOOK: Look in parent hierarchy context instead of global scene search
        AquariumManager manager = GetComponentInParent<AquariumManager>();
        if (manager != null)
        {
            foodPrefab = manager.foodPrefab;
        }
        else
        {
            Debug.LogError("[AutoFeeder] AquariumManager could not be found in parent hierarchy branch structures!");
        }

        // Initialize the timer randomly so if players buy multiple feeders, 
        // they don't all drop food at the exact same millisecond.
        timer = Random.Range(0f, dropInterval);
    }

    void Update()
    {
        if (foodPrefab == null) return;

        timer += Time.deltaTime;
        if (timer >= dropInterval)
        {
            DropPellet();
            timer = 0f; // Reset the interval timer
        }
    }

    void DropPellet()
    {
        // Calculate the drop position slightly below the feeder's center sprite anchor
        Vector3 dropPosition = new Vector3(transform.position.x, transform.position.y + dropYOffset, transform.position.z);
        
        // FIXED MULTI-TANK HOOK: Route food spawning through the local parent manager folder tree branch
        AquariumManager manager = GetComponentInParent<AquariumManager>();
        Transform container = (manager != null) ? manager.GetFoodContainer() : null;

        // Drop the food pellet into the aquarium nested safely under the container folder
        GameObject newFood = Instantiate(foodPrefab, dropPosition, Quaternion.identity, container);
        newFood.name = "AutoFed_FoodPellet";
    }

    // Draws a small blue indicator circle in the Unity Editor to help you visualize the drop point
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 dropPosition = new Vector3(transform.position.x, transform.position.y + dropYOffset, transform.position.z);
        Gizmos.DrawWireSphere(dropPosition, 0.15f);
    }
}