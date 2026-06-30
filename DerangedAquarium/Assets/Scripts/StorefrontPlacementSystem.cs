using UnityEngine;

public class StorefrontPlacementSystem : MonoBehaviour
{
    [Header("Placement Configuration")]
    public LayerMask floorSurfaceLayer;
    public float maxPlacementDistance = 6.0f;

    [Header("Hierarchy Anchor")]
    public Transform storefrontItemContainer;

    [Header("Rotation Keybinds")]
    public KeyCode rotateHotkey = KeyCode.R;

    private GameObject ghostPreviewInstance;
    private GameObject selectedPrefab;
    private int currentItemCost;
    private bool isPlacing = false;
    private float customRotationY = 0f;

    void Update()
    {
        if (!isPlacing) return;

        HandleRotationInput();
        HandleGhostFollowMovement();

        if (Input.GetMouseButtonDown(0))
        {
            ConfirmPlacement();
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    public void StartPlacement(GameObject prefab, int cost)
    {
        if (prefab == null) return;

        // --- UNIQUE INSTANCE PRE-PLACEMENT LOCKOUT CHECK ---
        TankInteraction3D tankComp = prefab.GetComponentInChildren<TankInteraction3D>();
        if (tankComp != null && tankComp.tankID != "Unassigned_Tank")
        {
            TankInteraction3D[] existingTanks = FindObjectsByType<TankInteraction3D>(FindObjectsSortMode.None);
            foreach (TankInteraction3D existing in existingTanks)
            {
                if (existing.tankID == tankComp.tankID)
                {
                    StorefrontShopUI shopUIInstance = FindFirstObjectByType<StorefrontShopUI>();
                    if (shopUIInstance != null)
                    {
                        shopUIInstance.TriggerNotificationAlert($"You already own unique aquarium variant '<b>{tankComp.tankID}</b>'!");
                    }
                    return;
                }
            }
        }

        StorefrontRemovalSystem removalSystem = FindFirstObjectByType<StorefrontRemovalSystem>();
        if (removalSystem != null)
        {
            removalSystem.ExitRemovalMode();
        }

        StorefrontShopUI shopUI = FindFirstObjectByType<StorefrontShopUI>();
        if (shopUI != null) shopUI.ForceCloseShop();

        selectedPrefab = prefab;
        currentItemCost = cost;
        customRotationY = 0f;

        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        if (player != null) player.SetPlayerLockState(false);

        ghostPreviewInstance = Instantiate(selectedPrefab);
        ApplyGhostTransparency(ghostPreviewInstance, 0.4f);

        Collider[] ghostColliders = ghostPreviewInstance.GetComponentsInChildren<Collider>();
        foreach (Collider col in ghostColliders)
        {
            col.enabled = false;
        }

        TankInteraction3D ghostTankComp = ghostPreviewInstance.GetComponentInChildren<TankInteraction3D>();
        if (ghostTankComp != null)
        {
            ghostTankComp.enabled = false;
        }

        isPlacing = true;
    }

    private void HandleRotationInput()
    {
        if (Input.GetKeyDown(rotateHotkey)) customRotationY += 90f;

        float scrollDelta = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scrollDelta > 0f) customRotationY += 90f;
        else if (scrollDelta < 0f) customRotationY -= 90f;

        if (customRotationY >= 360f) customRotationY -= 360f;
        if (customRotationY < 0f) customRotationY += 360f;
    }

    private void HandleGhostFollowMovement()
    {
        if (ghostPreviewInstance == null || Camera.main == null) return;

        Ray cameraRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit surfaceHit;

        // Modular per-prefab height offset detection engine
        float localPrefabOffset = 0f;
        PlacementHeightOffset offsetComp = ghostPreviewInstance.GetComponent<PlacementHeightOffset>();
        if (offsetComp == null) offsetComp = ghostPreviewInstance.GetComponentInChildren<PlacementHeightOffset>();
        if (offsetComp != null) localPrefabOffset = offsetComp.heightOffset;

        Vector3 targetPosition;
        Vector3 targetNormal = Vector3.up;
        bool foundSurface = false;

        // 1. Primary Check: Direct crosshair raycast hit on your floor layer surface
        if (Physics.Raycast(cameraRay, out surfaceHit, maxPlacementDistance, floorSurfaceLayer))
        {
            targetPosition = surfaceHit.point;
            targetNormal = surfaceHit.normal;
            foundSurface = true;
        }
        else
        {
            // 2. Fallback: Intersect with a mathematical flat floor plane to avoid sky/wall glitching
            float floorBaselineY = 0f;
            if (StorefrontBoundaryShield.Instance != null && StorefrontBoundaryShield.Instance.shopBoundaryCollider != null)
            {
                floorBaselineY = StorefrontBoundaryShield.Instance.shopBoundaryCollider.bounds.min.y;
            }

            Plane floorPlane = new Plane(Vector3.up, new Vector3(0f, floorBaselineY, 0f));
            float rayDistance;

            if (floorPlane.Raycast(cameraRay, out rayDistance) && rayDistance <= maxPlacementDistance)
            {
                targetPosition = cameraRay.GetPoint(rayDistance);
                foundSurface = true;
            }
            else
            {
                // If looking directly up into empty space, project a clean horizontal horizon vector forward
                Vector3 horizontalForward = Camera.main.transform.forward;
                horizontalForward.y = 0f; // Force vertical flattening
                
                if (horizontalForward.sqrMagnitude < 0.01f) 
                {
                    horizontalForward = Camera.main.transform.up;
                    horizontalForward.y = 0f;
                }
                horizontalForward.Normalize();

                // Lock position directly onto your floor height at max cursor reach boundaries
                targetPosition = new Vector3(Camera.main.transform.position.x, floorBaselineY, Camera.main.transform.position.z) + (horizontalForward * maxPlacementDistance);
                foundSurface = true;
            }
        }

        if (foundSurface)
        {
            ghostPreviewInstance.SetActive(true);

            // Apply specific asset model pivot height offsets
            targetPosition.y += localPrefabOffset;

            // ===================================================================
            // --- THE ROTATION FIX SYSTEM ---
            // 1. Calculate and apply the rotation transformations FIRST so the ghost
            //    preview shifts physical orientations before calculating boundaries.
            // ===================================================================
            Quaternion floorSlopeAlignment = Quaternion.FromToRotation(Vector3.up, targetNormal);
            Quaternion playerRotationOffset = Quaternion.Euler(0f, customRotationY, 0f);
            ghostPreviewInstance.transform.rotation = floorSlopeAlignment * playerRotationOffset;

            // 2. Extract the true real-time world size bounding box from the rotated object
            Vector3 realTimeRotatedSize = GetGhostWorldSize();

            // 3. Run coordinates through the boundary clamp shield using the dynamic size
            if (StorefrontBoundaryShield.Instance != null)
            {
                targetPosition = StorefrontBoundaryShield.Instance.GetClampedPlacementPosition(targetPosition, realTimeRotatedSize);
            }

            ghostPreviewInstance.transform.position = targetPosition;
        }
        else
        {
            ghostPreviewInstance.SetActive(false);
        }
    }

    /// <summary>
    /// Calculates the dynamic axis-aligned world size of the ghost preview asset based on its current rotation.
    /// </summary>
    private Vector3 GetGhostWorldSize()
    {
        if (ghostPreviewInstance == null) return Vector3.one;

        Renderer[] renderers = ghostPreviewInstance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return Vector3.one;

        // Encapsulate all child renderers to compute total structural thickness along world axes
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        return combinedBounds.size;
    }

    private void ConfirmPlacement()
    {
        if (ghostPreviewInstance == null || !ghostPreviewInstance.activeSelf) return;
        if (GlobalEconomyManager.Instance == null) return;

        TankInteraction3D tankComp = selectedPrefab.GetComponentInChildren<TankInteraction3D>();
        if (tankComp != null && tankComp.tankID != "Unassigned_Tank")
        {
            TankInteraction3D[] existingTanks = FindObjectsByType<TankInteraction3D>(FindObjectsSortMode.None);
            foreach (TankInteraction3D existing in existingTanks)
            {
                if (ghostPreviewInstance != null && existing.transform.IsChildOf(ghostPreviewInstance.transform)) continue;
                if (existing.tankID == tankComp.tankID)
                {
                    StorefrontShopUI shopUI = FindFirstObjectByType<StorefrontShopUI>();
                    if (shopUI != null)
                    {
                        shopUI.TriggerNotificationAlert($"You already own unique aquarium variant '<b>{tankComp.tankID}</b>'!");
                    }
                    CancelPlacement();
                    return;
                }
            }
        }

        if (GlobalEconomyManager.Instance.TrySpendMoney(currentItemCost))
        {
            Debug.Log($"[Placement] Successfully placed {selectedPrefab.name} for ${currentItemCost}.");

            Transform activeParentContainer = storefrontItemContainer;
            if (activeParentContainer == null)
            {
                GameObject dynamicFind = GameObject.Find("--- PLACED 3D ITEMS ---");
                if (dynamicFind != null) activeParentContainer = dynamicFind.transform;
            }

            GameObject placedObject = Instantiate(selectedPrefab, ghostPreviewInstance.transform.position, ghostPreviewInstance.transform.rotation, activeParentContainer);
            placedObject.name = selectedPrefab.name + "_Placed";
            
            PlacedItemData itemData = placedObject.AddComponent<PlacedItemData>();
            itemData.originalCost = currentItemCost;

            TankInteraction3D placedTankComp = placedObject.GetComponentInChildren<TankInteraction3D>();
            if (placedTankComp != null)
            {
                placedTankComp.enabled = true;
                if (placedTankComp.tankID != "Unassigned_Tank")
                {
                    placedTankComp.InitializeRuntimeTank(placedTankComp.tankID);
                }
            }

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.ProgressQuest("place_decor", 1);
            }

            EndPlacementWorkflow();
        }
        else
        {
            Debug.LogWarning("[Placement] Insufficient wallet balance totals!");
            CancelPlacement();
        }
    }

    private void CancelPlacement()
    {
        Debug.Log("[Placement] Item construction canceled by user.");
        EndPlacementWorkflow();
    }

    private void EndPlacementWorkflow()
    {
        if (ghostPreviewInstance != null) Destroy(ghostPreviewInstance);
        isPlacing = false;
        selectedPrefab = null;

        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        if (player != null) player.SetPlayerLockState(false);
    }

    private void ApplyGhostTransparency(GameObject target, float alphaValue)
    {
        Renderer[] targetRenderers = target.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in targetRenderers)
        {
            foreach (Material mat in rend.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color baseCol = mat.color;
                    baseCol.a = alphaValue;
                    mat.color = baseCol;
                }
            }
        }
    }
}