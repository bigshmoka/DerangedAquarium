using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemButton3D : MonoBehaviour
{
    [Header("Item Sale Details")]
    public string itemDisplayName = "Luxury Armchair";
    public int itemCost = 120;

    [Header("Target Prefab Asset")]
    [Tooltip("Drag the raw 3D item prefab file straight from your project folders here (NOT a scene object!).")]
    public GameObject itemPrefab;

    [Header("Internal UI Components")]
    public TMP_Text costDisplayText;
    private Button itemBuyButton;

    void Start()
    {
        itemBuyButton = GetComponent<Button>();

        if (costDisplayText != null)
        {
            costDisplayText.text = $"{itemDisplayName}\n${itemCost}";
        }

        if (itemBuyButton != null)
        {
            itemBuyButton.onClick.AddListener(LaunchStorefrontPlacement);
        }
    }

    void LaunchStorefrontPlacement()
    {
        if (itemPrefab == null) return;

        StorefrontPlacementSystem placementSystem = FindFirstObjectByType<StorefrontPlacementSystem>();
        if (placementSystem != null)
        {
            // Hands off the asset file data to trigger ghost preview tracking loops cleanly
            placementSystem.StartPlacement(itemPrefab, itemCost);
        }
        else
        {
            Debug.LogError("[3D Shop] StorefrontPlacementSystem component could not be found in active scenes!");
        }
    }
}