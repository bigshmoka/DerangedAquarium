using UnityEngine;

public class ShopTabSwitcher : MonoBehaviour
{
    [Header("Grid Panels")]
    public GameObject decorGrid; // Drag DecorGrid here
    public GameObject fishGrid;  // Drag FishGrid here

    // --- NEW: AUTO-RUNS WHEN THE GAME WAKES UP ---
    void Start()
    {
        // Force the shop to default straight to the Fish tab on bootup
        ShowFish();
    }

    // Called when the Decorations tab is clicked
    public void ShowDecorations()
    {
        if (decorGrid != null && fishGrid != null)
        {
            decorGrid.SetActive(true);
            fishGrid.SetActive(false);
        }
    }

    // Called when the Fish Species tab is clicked
    public void ShowFish()
    {
        if (decorGrid != null && fishGrid != null)
        {
            decorGrid.SetActive(false);
            fishGrid.SetActive(true);
        }
    }
}