using UnityEngine;

public class StorefrontShopUI : MonoBehaviour
{
    [Header("UI Panel References")]
    public GameObject shopMenuPanel; 

    [Header("Menu Controls")]
    public KeyCode shopHotkey = KeyCode.B; 

    [HideInInspector] public bool isShopOpen = false;

    void Start()
    {
        if (shopMenuPanel != null)
        {
            shopMenuPanel.SetActive(false);
        }
    }

    void Update()
    {
        TankInteraction3D tankView = FindFirstObjectByType<TankInteraction3D>();
        if (tankView != null && Cursor.lockState == CursorLockMode.None && !isShopOpen)
        {
            return; 
        }

        if (Input.GetKeyDown(shopHotkey))
        {
            ToggleStorefrontShop();
        }
    }

    public void ToggleStorefrontShop()
    {
        isShopOpen = !isShopOpen;

        if (shopMenuPanel != null)
        {
            shopMenuPanel.SetActive(isShopOpen);
        }

        // --- NEW: AUTO-CANCEL DECONSTRUCTION ON OPEN ---
        // If the shop panel menu is opened, automatically exit out of any active removal states
        if (isShopOpen)
        {
            StorefrontRemovalSystem removalSystem = FindFirstObjectByType<StorefrontRemovalSystem>();
            if (removalSystem != null)
            {
                removalSystem.ExitRemovalMode();
            }
        }

        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        if (player != null)
        {
            player.SetPlayerLockState(isShopOpen);
        }
    }

    public void ForceCloseShop()
    {
        isShopOpen = false;
        if (shopMenuPanel != null)
        {
            shopMenuPanel.SetActive(false);
        }
    }
}