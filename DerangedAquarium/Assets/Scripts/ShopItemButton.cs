using UnityEngine;

public class ShopItemButton : MonoBehaviour
{
    // --- UPDATED: Added Item to the category list ---
    public enum ItemType { Decoration, Fish, Item }

    [Header("Type Assignment")]
    public ItemType itemCategory = ItemType.Decoration;

    [Header("Item Settings")]
    public GameObject itemPrefab;
    public int itemCost = 50;

    public void BuyThisItem()
    {
        // Locates the master manager in the current scene
        AquariumManager manager = FindFirstObjectByType<AquariumManager>();
        
        if (manager != null)
        {
            // 1. Check if it belongs to the Fish/Creature category
            if (itemCategory == ItemType.Fish)
            {
                // Unique Snail population limiter
                if (itemPrefab != null && itemPrefab.name.Contains("Snail"))
                {
                    // Scan the tank to see if a snail already exists
                    SnailAI existingSnail = FindFirstObjectByType<SnailAI>();
                    
                    if (existingSnail != null)
                    {
                        // Fire off the screen alert on our manager UI and block the purchase
                        manager.TriggerNotificationAlert("Snail Limit Reached!");
                        return; 
                    }
                }

                // If the check passes (or it's just a normal fish), spawn it!
                manager.BuyFishFromShop(itemPrefab, itemCost);
            }
            // --- NEW: 2. Check if it belongs to the Item category (like the Automatic Feeder) ---
            else if (itemCategory == ItemType.Item)
            {
                manager.SelectItemFromShop(itemPrefab, itemCost);
            }
            // 3. Run standard mouse-placement loop for decorations
            else 
            {
                manager.SelectDecorationFromShop(itemPrefab, itemCost);
            }
        }
    }
}