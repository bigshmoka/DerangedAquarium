using UnityEngine;
using UnityEngine.EventSystems;

public class TankInputHandler : MonoBehaviour
{
    [HideInInspector] public GameObject foodPrefab;
    
    private TankShopUI shopUI;
    private TankPlacementSystem placementSystem;
    private TankHierarchyTracker hierarchyTracker;

    public void Initialize(TankShopUI targetShopUI, TankPlacementSystem targetPlacement, TankHierarchyTracker targetTracker)
    {
        shopUI = targetShopUI;
        placementSystem = targetPlacement;
        hierarchyTracker = targetTracker;
    }

    void Update()
    {
        if (placementSystem != null && (placementSystem.isPlacingDecoration || placementSystem.isPlacingItem)) return;

        HandleMouseClicks();
    }

    private void HandleMouseClicks()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (shopUI.isShopOpen) return;
            if (Input.mousePosition.y < 120) return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (Input.mousePosition.x < 0 || Input.mousePosition.x > 1920 ||
                Input.mousePosition.y < 0 || Input.mousePosition.y > 1080) return;

            if (shopUI.isFeedToolActive && !shopUI.isSpongeToolActive)
            {
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0f; 

                if (foodPrefab != null && hierarchyTracker != null)
                {
                    Instantiate(foodPrefab, mousePos, Quaternion.identity, hierarchyTracker.foodContainer);
                }
            }
        }
    }
}