using UnityEngine;

public class StorefrontTabSwitcher : MonoBehaviour
{
    [Header("Storefront Grid Layout Panels")]
    public GameObject furnitureGridPanel;   // Drag your Furniture Grid panel here
    public GameObject decorationsGridPanel; // Drag your Decorations Grid panel here
    public GameObject lightingGridPanel;    // Drag your Lighting Grid panel here

    void Start()
    {
        // Forces the shop menu layout to default straight to Furniture on bootup
        ShowFurnitureGrid();
    }

    public void ShowFurnitureGrid()
    {
        ToggleGridViews(true, false, false);
    }

    public void ShowDecorationsGrid()
    {
        ToggleGridViews(false, true, false);
    }

    public void ShowLightingGrid()
    {
        ToggleGridViews(false, false, true);
    }

    private void ToggleGridViews(bool furniture, bool decorations, bool lighting)
    {
        if (furnitureGridPanel != null) furnitureGridPanel.SetActive(furniture);
        if (decorationsGridPanel != null) decorationsGridPanel.SetActive(decorations);
        if (lightingGridPanel != null) lightingGridPanel.SetActive(lighting);
    }
}