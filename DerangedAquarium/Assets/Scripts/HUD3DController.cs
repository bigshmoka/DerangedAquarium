using UnityEngine;
using TMPro;

public class HUD3DController : MonoBehaviour
{
    [Header("3D Storefront HUD Setup")]
    public TMP_Text storefrontMoneyText;

    // --- NEW: CROSSHAIR SLOT ---
    [Tooltip("Drag your UI Crosshair Dot Image game object here.")]
    public GameObject crosshairVisualObject;

    void Start()
    {
        // Register your first person HUD text components with the master wallet router automatically
        if (storefrontMoneyText != null && GlobalEconomyManager.Instance != null)
        {
            GlobalEconomyManager.Instance.RegisterWalletDisplay(storefrontMoneyText);
        }
    }

    void OnDestroy()
    {
        // Safe disconnection hook to clean up memory layout references during reloads
        if (storefrontMoneyText != null && GlobalEconomyManager.Instance != null)
        {
            GlobalEconomyManager.Instance.UnregisterWalletDisplay(storefrontMoneyText);
        }
    }

    // Controls HUD visibility contextually when jumping into the 2D fish tank view
    public void SetMoneyTextVisibility(bool isVisible)
    {
        if (storefrontMoneyText != null)
        {
            storefrontMoneyText.gameObject.SetActive(isVisible);
        }

        // --- NEW: AUTO-TOGGLE CROSSHAIR VISIBILITY ---
        // This makes sure the crosshair disappears when looking into the 2D tank
        // and returns seamlessly when you press Q to walk around the 3D store!
        if (crosshairVisualObject != null)
        {
            crosshairVisualObject.SetActive(isVisible);
        }
    }
}