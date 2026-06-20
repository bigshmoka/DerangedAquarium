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
    private bool isOpen = false;

    void Start()
    {
        if (tabRectTransform == null)
            tabRectTransform = GetComponent<RectTransform>();
            
        if (aquariumManager == null)
            aquariumManager = FindFirstObjectByType<AquariumManager>();

        // Ensure the layout window container stays awake so it can move seamlessly
        if (aquariumManager != null && aquariumManager.shopMenuWindow != null)
        {
            aquariumManager.shopMenuWindow.SetActive(true);
        }

        tabRectTransform.anchoredPosition = hiddenPosition;
        targetPosition = hiddenPosition;

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(ToggleShop);
        }
    }

    void Update()
    {
        // Smoothly slide toward target positioning definitions
        tabRectTransform.anchoredPosition = Vector2.Lerp(
            tabRectTransform.anchoredPosition, 
            targetPosition, 
            Time.deltaTime * slideSpeed
        );

        // --- FIX: Automatically slide away if the manager flags the shop as closed (e.g. after buying a fish) ---
        if (aquariumManager != null && !aquariumManager.isShopOpen && isOpen)
        {
            ForceClose();
        }
    }

    public void ToggleShop()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            targetPosition = visiblePosition;
            if (aquariumManager != null)
            {
                aquariumManager.isShopOpen = true;
            }
        }
        else
        {
            ForceClose();
        }
    }

    public void ForceClose()
    {
        isOpen = false;
        targetPosition = hiddenPosition;
        if (aquariumManager != null)
        {
            aquariumManager.isShopOpen = false;
        }
    }
}