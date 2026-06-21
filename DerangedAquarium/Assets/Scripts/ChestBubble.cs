using UnityEngine;

public class ChestBubble : MonoBehaviour
{
    [Header("Bubble Settings")]
    public float floatSpeed = 2.0f;
    public float lifetime = 6.0f;

    void Start()
    {
        // Automatically destroy the bubble after a few seconds so they don't float forever
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Move the bubble straight up over time
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);
    }

    // Detects when another object enters this bubble's trigger zone
    void OnTriggerEnter2D(Collider2D other)
    {
        MoneyDropItem coin = other.GetComponent<MoneyDropItem>();

        // IF IT'S A COIN: Only interact if it hasn't been multiplied yet!
        if (coin != null && !coin.hasBeenMultiplied)
        {
            // 1. Lock the coin so it can never be multiplied again
            coin.hasBeenMultiplied = true;

            // 2. Double the reward value
            int originalValue = coin.cashValue;
            coin.cashValue *= 2; 

            Debug.Log($"<color=yellow>[Treasure Chest]</color> Safe Multiplier: {other.gameObject.name} doubled from ${originalValue} to ${coin.cashValue}!");

            // 3. Pop the bubble because it successfully spent its power on a fresh coin
            Destroy(gameObject);
        }
        // --- FIXED: IF THE COIN IS ALREADY MULTIPLIED ---
        // We do absolutely nothing here! No Destroy(gameObject) is called.
        // The bubble will visually glide straight through the coin and keep rising!
    }
}