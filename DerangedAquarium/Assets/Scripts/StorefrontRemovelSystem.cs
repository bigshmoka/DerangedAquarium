using UnityEngine;
using System.Collections.Generic;

public class StorefrontRemovalSystem : MonoBehaviour
{
    [Header("Removal Configuration")]
    [Tooltip("The maximum distance (in units) away from the player that an item can be removed.")]
    public float maxRemovalDistance = 6.0f;
    public KeyCode cancelHotkey = KeyCode.Escape;

    [Header("Hover Feedback Colors")]
    public Color hoverHighlightColor = new Color(1f, 0.3f, 0.3f, 1f); 

    private bool isRemovingMode = false;
    private GameObject lastHoveredObject = null;
    private Dictionary<Renderer, Color[]> originalMaterialColors = new Dictionary<Renderer, Color[]>();

    void Update()
    {
        if (!isRemovingMode) return;

        HandleTargetScanning();

        if (Input.GetMouseButtonDown(0))
        {
            AttemptRemoveItem();
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(cancelHotkey))
        {
            ExitRemovalMode();
        }
    }

    public void StartRemovalMode()
    {
        StorefrontShopUI shopUI = FindFirstObjectByType<StorefrontShopUI>();
        if (shopUI != null) shopUI.ForceCloseShop();

        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        if (player != null) player.SetPlayerLockState(false);

        isRemovingMode = true;
        lastHoveredObject = null;
        originalMaterialColors.Clear();
        
        Debug.Log("<color=yellow>[Removal Mode]</color> Activated! Hover over an item and Left-Click to remove it.");
    }

    private void HandleTargetScanning()
    {
        if (Camera.main == null) return;

        Ray cameraRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit surfaceHit;

        GameObject currentlyPointedRoot = null;

        if (Physics.Raycast(cameraRay, out surfaceHit, maxRemovalDistance))
        {
            currentlyPointedRoot = FindPlacedRootObject(surfaceHit.collider.gameObject);
        }

        if (currentlyPointedRoot != lastHoveredObject)
        {
            if (lastHoveredObject != null)
            {
                ClearTargetHighlightVisuals(lastHoveredObject);
            }

            lastHoveredObject = currentlyPointedRoot;

            if (lastHoveredObject != null)
            {
                ApplyTargetHighlightVisuals(lastHoveredObject);
            }
        }
    }

    private void ApplyTargetHighlightVisuals(GameObject target)
    {
        if (target == null) return;

        originalMaterialColors.Clear();
        Renderer[] targetRenderers = target.GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in targetRenderers)
        {
            Material[] runtimeSharedMats = rend.materials;
            Color[] savedBaseColors = new Color[runtimeSharedMats.Length];

            for (int i = 0; i < runtimeSharedMats.Length; i++)
            {
                if (runtimeSharedMats[i].HasProperty("_Color"))
                {
                    savedBaseColors[i] = runtimeSharedMats[i].color;
                    runtimeSharedMats[i].color = savedBaseColors[i] * hoverHighlightColor;
                }

                if (runtimeSharedMats[i].HasProperty("_EmissionColor"))
                {
                    runtimeSharedMats[i].EnableKeyword("_EMISSION");
                    runtimeSharedMats[i].SetColor("_EmissionColor", Color.red * 0.4f);
                }
            }

            if (!originalMaterialColors.ContainsKey(rend))
            {
                originalMaterialColors.Add(rend, savedBaseColors);
            }
        }
    }

    private void ClearTargetHighlightVisuals(GameObject target)
    {
        if (target == null) return;

        Renderer[] targetRenderers = target.GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in targetRenderers)
        {
            if (originalMaterialColors.TryGetValue(rend, out Color[] cachedColors))
            {
                Material[] runtimeSharedMats = rend.materials;
                for (int i = 0; i < runtimeSharedMats.Length && i < cachedColors.Length; i++)
                {
                    if (runtimeSharedMats[i].HasProperty("_Color"))
                    {
                        runtimeSharedMats[i].color = cachedColors[i];
                    }

                    if (runtimeSharedMats[i].HasProperty("_EmissionColor"))
                    {
                        runtimeSharedMats[i].SetColor("_EmissionColor", Color.black);
                        runtimeSharedMats[i].DisableKeyword("_EMISSION");
                    }
                }
            }
        }
        originalMaterialColors.Clear();
    }

    private void AttemptRemoveItem()
    {
        if (Camera.main == null) return;

        Ray cameraRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit surfaceHit;

        if (Physics.Raycast(cameraRay, out surfaceHit, maxRemovalDistance))
        {
            GameObject targetRoot = FindPlacedRootObject(surfaceHit.collider.gameObject);
            if (targetRoot != null)
            {
                // --- NEW: THE REFUND PROCESSING ENGINE ---
                // Query the target item to see if it tracks an active price tag script component
                PlacedItemData itemData = targetRoot.GetComponent<PlacedItemData>();
                if (itemData != null && GlobalEconomyManager.Instance != null)
                {
                    // Calculate exactly 50% refund value (using integer division)
                    int refundAmount = itemData.originalCost / 2;
                    
                    if (refundAmount > 0)
                    {
                        GlobalEconomyManager.Instance.AddMoney(refundAmount);
                        Debug.Log($"<color=green>[Refund]</color> Item cost ${itemData.originalCost}. Refounded 50%: +${refundAmount} to your wallet.");
                    }
                }
                else
                {
                    Debug.LogWarning("[Refund] This object did not track a PlacedItemData script component. Deleting with $0 refund.");
                }

                Debug.Log($"[Removal Mode] Successfully destroyed placed item asset: {targetRoot.name}");
                
                originalMaterialColors.Clear();
                lastHoveredObject = null;

                Destroy(targetRoot);
            }
        }
    }

    private GameObject FindPlacedRootObject(GameObject hitObj)
    {
        Transform currentFolderNode = hitObj.transform;
        while (currentFolderNode != null)
        {
            if (currentFolderNode.name.Contains("_Placed"))
            {
                return currentFolderNode.gameObject;
            }
            currentFolderNode = currentFolderNode.parent;
        }
        return null;
    }

    public void ExitRemovalMode()
    {
        if (!isRemovingMode) return;

        if (lastHoveredObject != null)
        {
            ClearTargetHighlightVisuals(lastHoveredObject);
        }

        isRemovingMode = false;
        lastHoveredObject = null;
        Debug.Log("<color=yellow>[Removal Mode]</color> Deactivated.");

        PlayerController3D player = FindFirstObjectByType<PlayerController3D>();
        if (player != null) player.SetPlayerLockState(false);
    }
}