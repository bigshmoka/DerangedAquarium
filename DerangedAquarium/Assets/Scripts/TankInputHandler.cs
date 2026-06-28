using UnityEngine;
using UnityEngine.EventSystems;

public class TankInputHandler : MonoBehaviour
{
    [HideInInspector] public GameObject foodPrefab;
    
    private TankShopUI shopUI;
    private TankPlacementSystem placementSystem;
    private TankHierarchyTracker hierarchyTracker;
    
    private Camera localTankCamera;
    private AquariumManager manager;

    public void Initialize(TankShopUI targetShopUI, TankPlacementSystem targetPlacement, TankHierarchyTracker targetTracker)
    {
        shopUI = targetShopUI;
        placementSystem = targetPlacement;
        hierarchyTracker = targetTracker;
        
        manager = GetComponent<AquariumManager>();
        
        // Inherit the exact camera matched by spatial coordinates in AquariumManager
        if (manager != null)
        {
            localTankCamera = manager.tankCamera;
        }
    }

    void Update()
    {
        if (manager != null && !manager.isTankVisible) return;
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

            // ===================================================================
            // --- FIXED: DYNAMIC MONITOR SCALE PROTECTION ENGINE ---
            // Swapped hardcoded '1920' and '1080' bounds for active viewport limits.
            // Spawns food flawlessly across ultra-wide, 1440p, 4K, or resized test windows!
            // ===================================================================
            if (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width || Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height) return;

            if (shopUI.isFeedToolActive && !shopUI.isSpongeToolActive)
            {
                // Guarantee coordinate mappings look up the correct orthographic lens
                Camera activeCam = (localTankCamera != null) ? localTankCamera : Camera.main;
                if (activeCam == null) return;

                Vector3 mousePos = activeCam.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0f; 

                if (foodPrefab != null && hierarchyTracker != null)
                {
                    Instantiate(foodPrefab, mousePos, Quaternion.identity, hierarchyTracker.foodContainer);
                }
            }
        }
    }
}