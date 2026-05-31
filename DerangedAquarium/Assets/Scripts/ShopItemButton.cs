using UnityEngine;

public class ShopItemButton : MonoBehaviour
{
    [Header("Item Settings")]
    public GameObject decorationPrefab;
    public int itemCost = 50;

    // This function has 0 parameters, so it WILL show up perfectly in Unity's UI dropdown menu!
    public void BuyThisItem()
    {
        // Find our manager in the scene
        AquariumManager manager = FindFirstObjectByType<AquariumManager>();
        
        if (manager != null)
        {
            // Pass the information safely over to our core placement loop
            manager.SelectDecorationFromShop(decorationPrefab, itemCost);
        }
        else
        {
            Debug.LogError("Could not find the AquariumManager in the scene!");
        }
    }
}