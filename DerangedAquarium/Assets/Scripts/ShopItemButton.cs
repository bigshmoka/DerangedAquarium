using UnityEngine;

public class ShopItemButton : MonoBehaviour
{
    public enum ItemType { Decoration, Fish, Item }

    // --- UPDATED TO SCRIPTABLE OBJECT DATA TYPE ---
    [Header("Data Profile Source")]
    [Tooltip("Drag the matching AquariumItemData asset profile card from your project assets window here!")]
    public AquariumItemData itemData;

    public void BuyThisItem()
    {
        // Safety validation to prevent inspector layout assembly oversight errors
        if (itemData == null)
        {
            Debug.LogError($"[Aquarium Shop] Button '{gameObject.name}' is missing an assigned AquariumItemData object profile asset source!", this);
            return;
        }

        // Locates the master manager in the current scene
        AquariumManager manager = FindFirstObjectByType<AquariumManager>();
        
        if (manager != null)
        {
            // 1. Check if it belongs to the Fish/Creature category
            if (itemData.itemCategory == ItemType.Fish)
            {
                // Unique Snail population limiter
                if (itemData.itemPrefab != null && itemData.itemPrefab.name.Contains("Snail"))
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

                // If the check passes (or it's just a normal fish), spawn it using the data container profile attributes!
                manager.BuyFishFromShop(itemData.itemPrefab, itemData.itemCost);
            }
            // 2. Check if it belongs to the Item category (like the Automatic Feeder)
            else if (itemData.itemCategory == ItemType.Item)
            {
                manager.SelectItemFromShop(itemData.itemPrefab, itemData.itemCost);
            }
            // 3. Run standard mouse-placement loop for decorations
            else 
            {
                manager.SelectDecorationFromShop(itemData.itemPrefab, itemData.itemCost);
            }
        }
    }
}