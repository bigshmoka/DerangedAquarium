using UnityEngine;

public class ShopTabSwitcher : MonoBehaviour
{
    [Header("Grid Panels")]
    public GameObject fishGrid;   // Drag FishGrid here
    public GameObject decorGrid;  // Drag DecorGrid here
    public GameObject itemsGrid;  // --- NEW: Drag ItemsGrid here

    void Start()
    {
        // Force the shop to default straight to the Fish tab on bootup
        ShowFish();
    }

    // Called when the Fish Species tab is clicked
    public void ShowFish()
    {
        SetGridStates(true, false, false);
    }

    // Called when the Decorations tab is clicked
    public void ShowDecorations()
    {
        SetGridStates(false, true, false);
    }

    // --- NEW: Called when the Items tab is clicked ---
    public void ShowItems()
    {
        SetGridStates(false, false, true);
    }

    // Helper function to cleanly swap panel visibilities without copy-pasting code blocks
    private void SetGridStates(bool fish, bool decor, bool items)
    {
        if (fishGrid != null) fishGrid.SetActive(fish);
        if (decorGrid != null) decorGrid.SetActive(decor);
        if (itemsGrid != null) itemsGrid.SetActive(items);
    }
}