using UnityEngine;

[CreateAssetMenu(fileName = "NewStorefrontItem", menuName = "Storefront/Shop Item Data", order = 1)]
public class StorefrontItemData : ScriptableObject
{
    [Header("Visual Inventory Tracking")]
    public string itemDisplayName = "Luxury Sofa";

    [Header("Item Configuration")]
    public GameObject itemPrefab;
    public int itemCost = 200;
}