using UnityEngine;

public class ShopItemButton : MonoBehaviour
{
    public enum ItemType { Decoration, Fish }

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
            // If it belongs to the Fish/Creature category, run population checks
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
            else // Run standard mouse-placement loop for decorations
            {
                manager.SelectDecorationFromShop(itemPrefab, itemCost);
            }
        }
    }
}