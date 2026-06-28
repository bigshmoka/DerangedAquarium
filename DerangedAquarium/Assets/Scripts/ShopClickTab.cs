using UnityEngine;
using UnityEngine.UI;

public class ShopClickTab : MonoBehaviour
{
    [Header("References")]
    public AquariumManager aquariumManager;

    [Header("Slide Settings")]
    public RectTransform tabRectTransform;
    public float slideSpeed = 10f;
    
    public Vector2 hiddenPosition;
    public Vector2 visiblePosition;

    private Vector2 targetPosition;

    void Start()
    {
        if (tabRectTransform == null)
            tabRectTransform = GetComponent<RectTransform>();

        tabRectTransform.anchoredPosition = hiddenPosition;
        targetPosition = hiddenPosition;

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(ToggleShop);
        }
    }

    void Update()
    {
        // ===================================================================
        // --- FIXED: MASTER STATE DIRECTIONAL DRIVE ---
        // The sliding location is driven directly by the manager's state.
        // This removes conflicting local booleans that break on view changes!
        // ===================================================================
        if (aquariumManager != null)
        {
            targetPosition = aquariumManager.isShopOpen ? visiblePosition : hiddenPosition;
        }

        tabRectTransform.anchoredPosition = Vector2.Lerp(
            tabRectTransform.anchoredPosition, 
            targetPosition, 
            Time.deltaTime * slideSpeed
        );
    }

    public void ToggleShop()
    {
        if (aquariumManager != null)
        {
            // Reverse the master shop tracking state directly
            aquariumManager.isShopOpen = !aquariumManager.isShopOpen;
            
            // Synchronize the internal UI layout tracker flags
            if (aquariumManager.isShopOpen)
            {
                aquariumManager.OpenShopMenu();
            }
            else
            {
                aquariumManager.CloseShopMenu();
            }
        }
    }

    public void ForceClose()
    {
        if (aquariumManager != null)
        {
            aquariumManager.isShopOpen = false;
            aquariumManager.CloseShopMenu();
        }
    }
}