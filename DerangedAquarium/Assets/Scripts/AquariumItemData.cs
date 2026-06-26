using UnityEngine;

[CreateAssetMenu(fileName = "NewAquariumItem", menuName = "Aquarium/Shop Item Data", order = 1)]
public class AquariumItemData : ScriptableObject
{
    [Header("Visual Inventory Tracking")]
    public string itemDisplayName = "Goldfish";
    
    [Header("Item Configuration")]
    public ShopItemButton.ItemType itemCategory = ShopItemButton.ItemType.Decoration;
    public GameObject itemPrefab;
    public int itemCost = 50;
}