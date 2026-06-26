using UnityEngine;
using System.Collections.Generic;

public class StorefrontRemovalSystem : MonoBehaviour
{
    [Header("Removal Configuration")]
    [Tooltip("The maximum distance (in units) away from the player that an item can be removed.")]
    public float maxRemovalDistance = 6.0f;
    public KeyCode cancelHotkey = KeyCode.Escape;

    [Header("Hover Feedback Colors")]
    [Tooltip("The color tint mixed onto the item when your crosshair focuses on it.")]
    public Color hoverHighlightColor = new Color(1f, 0.3f, 0.3f, 1f); // Bright warning red tint

    private bool isRemovingMode = false;
    
    // --- NEW: VISUAL HOVER TRACKING STATE VARS ---
    private GameObject lastHoveredObject = null;
    private Dictionary<Renderer, Color[]> originalMaterialColors = new Dictionary<Renderer, Color[]>();

    void Update()
    {
        if (!isRemovingMode) return;

        HandleTargetScanning();

        // Left-Click to permanently delete the targeted object
        if (Input.GetMouseButtonDown(0))
        {
            AttemptRemoveItem();
        }

        // Right-Click or Escape to drop out of removal mode cleanly
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

        // Perform raycast check from camera center POV crosshair straight ahead
        if (Physics.Raycast(cameraRay, out surfaceHit, maxRemovalDistance))
        {
            currentlyPointedRoot = FindPlacedRootObject(surfaceHit.collider.gameObject);
        }

        // --- NEW: THE HOVER VISUAL STATES HIGHLIGHT ENGINE ---
        // Case A: You shifted your crosshair to look at a completely different object
        if (currentlyPointedRoot != lastHoveredObject)
        {
            // Wipe clean any highlights on the old item you walked away from
            if (lastHoveredObject != null)
            {
                ClearTargetHighlightVisuals(lastHoveredObject);
            }

            // Assign the new target and switch on its red selection overlay flash
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
                // Cache your original color values safely before modifying them
                if (runtimeSharedMats[i].HasProperty("_Color"))
                {
                    savedBaseColors[i] = runtimeSharedMats[i].color;
                    
                    // Apply a striking reddish highlights overlay multiply tint
                    runtimeSharedMats[i].color = savedBaseColors[i] * hoverHighlightColor;
                }

                // Optional: Turn on an emission glow if the object's shader maps it
                if (runtimeSharedMats[i].HasProperty("_EmissionColor"))
                {
                    runtimeSharedMats[i].EnableKeyword("_EMISSION");
                    runtimeSharedMats[i].SetColor("_EmissionColor", Color.red * 0.4f);
                }
            }

            // Lock values down into dictionary memory logs
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
                    // Restore original flat look textures flawlessly
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
                Debug.Log($"<color=red>[Removal Mode]</color> Successfully destroyed placed item asset: {targetRoot.name}");
                
                // Safety sequence cleanup: wipe cached highlights references immediately BEFORE deleting
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

        // Restore textures instantly on whatever item you were staring at before quitting
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