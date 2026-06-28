using UnityEngine;
using TMPro;

public class StorefrontShopUI : MonoBehaviour
{
    [Header("UI Panel References")]
    public GameObject shopMenuPanel; 

    // ===================================================================
    // --- NEW: STOREFRONT NOTIFICATION OVERLAY ---
    // Drag a TextMeshPro text component here to display custom warning alerts
    // straight inside your open 3D shop window interface!
    // ===================================================================
    [Header("Notification Settings")]
    [Tooltip("Drag a TextMeshPro text element here to act as your error warning display card.")]
    public TMP_Text errorNotificationText;

    [Header("Menu Controls")]
    public KeyCode shopHotkey = KeyCode.B; 

    [HideInInspector] public bool isShopOpen = false;

    void Start()
    {
        if (shopMenuPanel != null)
        {
            shopMenuPanel.SetActive(false);
        }

        if (errorNotificationText != null)
        {
            errorNotificationText.gameObject.SetActive(false);
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

        if (isShopOpen)
        {
            StorefrontRemovalSystem removalSystem = FindFirstObjectByType<StorefrontRemovalSystem>();
            if (removalSystem != null)
            {
                removalSystem.ExitRemovalMode();
            }
            
            // Clean up old alerts on fresh menu open triggers
            if (errorNotificationText != null) errorNotificationText.gameObject.SetActive(false);
        }

        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        if (player != null)
        {
            player.SetPlayerLockState(isShopOpen);
        }
    }

    // ===================================================================
    // --- NEW: STOREFRONT NOTIFICATION ACTIONS ---
    // Safely manages text warning triggers with temporary visibility invoke cards.
    // ===================================================================
    public void TriggerNotificationAlert(string message)
    {
        if (errorNotificationText != null)
        {
            errorNotificationText.text = message;
            errorNotificationText.gameObject.SetActive(true);
            
            CancelInvoke(nameof(HideNotificationAlert));
            Invoke(nameof(HideNotificationAlert), 3.0f);
        }
    }

    private void HideNotificationAlert()
    {
        if (errorNotificationText != null)
        {
            errorNotificationText.gameObject.SetActive(false);
        }
    }

    public void ForceCloseShop()
    {
        isShopOpen = false;
        if (shopMenuPanel != null)
        {
            shopMenuPanel.SetActive(false);
        }

        // --- FIXED: CURSOR LEAK SAFETY GUARD ---
        // Guarantees player lock state properties drop down synchronously
        // if this method executes from unexpected structural exceptions!
        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        if (player != null)
        {
            player.SetPlayerLockState(false);
        }
    }
}