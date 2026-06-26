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

        // --- NEW: AUTO-CANCEL DECONSTRUCTION ON PLACEMENT ---
        // Ensure any running removal loops are turned off before entering placement mode
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

        if (Physics.Raycast(cameraRay, out surfaceHit, maxPlacementDistance, floorSurfaceLayer))
        {
            ghostPreviewInstance.SetActive(true);
            ghostPreviewInstance.transform.position = surfaceHit.point;
            
            Quaternion floorSlopeAlignment = Quaternion.FromToRotation(Vector3.up, surfaceHit.normal);
            Quaternion playerRotationOffset = Quaternion.Euler(0f, customRotationY, 0f);
            
            ghostPreviewInstance.transform.rotation = floorSlopeAlignment * playerRotationOffset;
        }
        else
        {
            Vector3 fallbackTarget = cameraRay.GetPoint(maxPlacementDistance);
            Ray downRay = new Ray(new Vector3(fallbackTarget.x, Camera.main.transform.position.y + 4f, fallbackTarget.z), Vector3.down);
            RaycastHit downHit;

            if (Physics.Raycast(downRay, out downHit, 25f, floorSurfaceLayer))
            {
                ghostPreviewInstance.SetActive(true);
                ghostPreviewInstance.transform.position = downHit.point;
                
                Quaternion floorSlopeAlignment = Quaternion.FromToRotation(Vector3.up, downHit.normal);
                Quaternion playerRotationOffset = Quaternion.Euler(0f, customRotationY, 0f);
                
                ghostPreviewInstance.transform.rotation = floorSlopeAlignment * playerRotationOffset;
            }
            else
            {
                ghostPreviewInstance.SetActive(false);
            }
        }
    }

    private void ConfirmPlacement()
    {
        if (ghostPreviewInstance == null || !ghostPreviewInstance.activeSelf) return;
        if (GlobalEconomyManager.Instance == null) return;

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
            
            // Critical Identifier: StorefrontRemovalSystem looks for this specific name suffix to target items!
            placedObject.name = selectedPrefab.name + "_Placed";

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