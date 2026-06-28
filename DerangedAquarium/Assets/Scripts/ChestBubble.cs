using UnityEngine;

public class ChestBubble : MonoBehaviour
{
    [Header("Bubble Settings")]
    public float floatSpeed = 2.0f;
    public float lifetime = 6.0f;

    void Start()
    {
        Destroy(gameObject, lifetime);

        // FIXED MULTI-TANK HOOK: Look up parent tree branches for culling visibility logic
        AquariumManager manager = GetComponentInParent<AquariumManager>();
        if (manager != null && !manager.isTankVisible)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false; // Spawns invisibly in the background safely
        }
    }

    void Update()
    {
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        MoneyDropItem coin = other.GetComponent<MoneyDropItem>();

        if (coin != null && !coin.hasBeenMultiplied)
        {
            coin.hasBeenMultiplied = true;

            int originalValue = coin.cashValue;
            coin.cashValue *= 2; 

            Debug.Log($"<color=yellow>[Treasure Chest]</color> Safe Multiplier: {other.gameObject.name} doubled from ${originalValue} to ${coin.cashValue}!");

            Destroy(gameObject);
        }
    }
}