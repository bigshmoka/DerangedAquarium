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
        AquariumManager manager = FindFirstObjectByType<AquariumManager>();
        
        if (manager != null)
        {
            // If it's a fish, bypass cursor drag placement and spawn it right in the water!
            if (itemCategory == ItemType.Fish)
            {
                manager.BuyFishFromShop(itemPrefab, itemCost);
            }
            else // Otherwise, run our standard mouse-placement ghost loop
            {
                manager.SelectDecorationFromShop(itemPrefab, itemCost);
            }
        }
    }
}