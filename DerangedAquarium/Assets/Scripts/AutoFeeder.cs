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
        // Dynamically locate the food prefab from your main Aquarium Manager
        AquariumManager manager = FindFirstObjectByType<AquariumManager>();
        if (manager != null)
        {
            foodPrefab = manager.foodPrefab;
        }
        else
        {
            Debug.LogError("[AutoFeeder] AquariumManager could not be found in the scene!");
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
        
        // Drop the food pellet into the aquarium
        GameObject newFood = Instantiate(foodPrefab, dropPosition, Quaternion.identity);
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