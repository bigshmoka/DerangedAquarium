using UnityEngine;
using TMPro;

public class HUD3DController : MonoBehaviour
{
    [Header("3D Storefront HUD Setup")]
    public TMP_Text storefrontMoneyText;

    // --- NEW: PROGRESSION HUD TEXT ELEMENTS ---
    [Tooltip("Drag your TextMeshPro element for Museum Level here (e.g. 'Level 1').")]
    public TMP_Text storefrontLevelText;
    [Tooltip("Drag your TextMeshPro element for XP tracking here (e.g. 'XP: 45 / 100').")]
    public TMP_Text storefrontXPText;

    [Header("Crosshair Configuration")]
    [Tooltip("Drag your UI Crosshair Dot Image game object here.")]
    public GameObject crosshairVisualObject;

    void Start()
    {
        // Register your first person HUD text components with the master wallet router automatically
        if (storefrontMoneyText != null && GlobalEconomyManager.Instance != null)
        {
            GlobalEconomyManager.Instance.RegisterWalletDisplay(storefrontMoneyText);
        }

        // --- NEW: FETCH INITIAL VALUES UPON SPAWNING ---
        UpdatePrestigeVisuals();
    }

    void OnDestroy()
    {
        // Safe disconnection hook to clean up memory layout references during reloads
        if (storefrontMoneyText != null && GlobalEconomyManager.Instance != null)
        {
            GlobalEconomyManager.Instance.UnregisterWalletDisplay(storefrontMoneyText);
        }
    }

    // ===================================================================
    // --- NEW: DYNAMIC PROGRESSION REPAINT ENGINE ---
    // Reads directly from your global level manager to redraw numbers live on screen!
    // ===================================================================
    public void UpdatePrestigeVisuals()
    {
        if (ExhibitPrestigeManager.Instance == null) return;

        int currentLvl = ExhibitPrestigeManager.Instance.currentLevel;
        int currentXP = ExhibitPrestigeManager.Instance.currentPrestigePoints;
        int requiredXP = currentLvl * ExhibitPrestigeManager.Instance.pointsPerLevelMultiplier;

        if (storefrontLevelText != null)
        {
            storefrontLevelText.text = $"Level {currentLvl}";
        }

        if (storefrontXPText != null)
        {
            storefrontXPText.text = $"XP: {currentXP} / {requiredXP}";
        }
    }

    // Controls HUD visibility contextually when jumping into the 2D fish tank view
    public void SetMoneyTextVisibility(bool isVisible)
    {
        if (storefrontMoneyText != null)
        {
            storefrontMoneyText.gameObject.SetActive(isVisible);
        }

        // --- NEW: AUTOMATICALLY FLUSH PROGRESSION LABELS ON VIEW SWITCHES ---
        // Ensures your level readouts don't float clumsily over your 2D decorations!
        if (storefrontLevelText != null) storefrontLevelText.gameObject.SetActive(isVisible);
        if (storefrontXPText != null) storefrontXPText.gameObject.SetActive(isVisible);

        if (crosshairVisualObject != null)
        {
            crosshairVisualObject.SetActive(isVisible);
        }
    }
}