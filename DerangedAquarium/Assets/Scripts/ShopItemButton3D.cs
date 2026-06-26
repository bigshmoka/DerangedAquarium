using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemButton3D : MonoBehaviour
{
    [Header("Data Profile Source")]
    [Tooltip("Drag the matching StorefrontItemData asset profile card from your project assets window here!")]
    public StorefrontItemData itemData;

    [Header("Internal UI Components")]
    public TMP_Text costDisplayText;
    private Button itemBuyButton;

    void Start()
    {
        itemBuyButton = GetComponent<Button>();

        if (itemData == null)
        {
            Debug.LogError($"[3D Shop] Button '{gameObject.name}' is missing an assigned StorefrontItemData object profile asset source!", this);
            return;
        }

        // Automatically format and display the price tag dynamically from the ScriptableObject file
        if (costDisplayText != null)
        {
            costDisplayText.text = $"{itemData.itemDisplayName}\n${itemData.itemCost}";
        }

        if (itemBuyButton != null)
        {
            itemBuyButton.onClick.AddListener(LaunchStorefrontPlacement);
        }
    }

    void LaunchStorefrontPlacement()
    {
        if (itemData == null || itemData.itemPrefab == null) return;

        StorefrontPlacementSystem placementSystem = FindFirstObjectByType<StorefrontPlacementSystem>();
        if (placementSystem != null)
        {
            // Hands off the raw prefab file asset and price point data straight from the profile card!
            placementSystem.StartPlacement(itemData.itemPrefab, itemData.itemCost);
        }
        else
        {
            Debug.LogError("[3D Shop] StorefrontPlacementSystem component could not be found in active scenes!");
        }
    }
}