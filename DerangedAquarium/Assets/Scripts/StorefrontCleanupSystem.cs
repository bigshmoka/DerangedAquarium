using UnityEngine;
using TMPro;

public class StorefrontCleanupSystem : MonoBehaviour
{
    [Header("Sweeping Reach Config")]
    public float maxCleanDistance = 3.5f;

    [Header("UI Prompt Integration")]
    [Tooltip("Drag your small TextMeshPro text element here (e.g., 'Press Left-Click to sweep trash').")]
    public TMP_Text cleanupPromptText;

    void Start()
    {
        if (cleanupPromptText != null) 
            cleanupPromptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Camera.main == null) return;

        // Shoot a laser straight forward out of the center crosshair dot path
        Ray crosshairRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hitInfo;

        // Swapped out the LayerMask requirement so it naturally checks whatever your face is pointing at!
        if (Physics.Raycast(crosshairRay, out hitInfo, maxCleanDistance))
        {
            // Scan the hit collider object to check if it's structural trash
            TrashObject targetedTrash = hitInfo.collider.GetComponent<TrashObject>();
            if (targetedTrash == null) targetedTrash = hitInfo.collider.GetComponentInParent<TrashObject>();

            if (targetedTrash != null)
            {
                // Display context sensitive interaction prompt HUD card
                if (cleanupPromptText != null)
                {
                    cleanupPromptText.text = $"[Left-Click] Sweep up {targetedTrash.trashName}";
                    cleanupPromptText.gameObject.SetActive(true);
                }

                // Process cleanup on left-click input parameters
                if (Input.GetMouseButtonDown(0))
                {
                    targetedTrash.SweepUp();
                    if (cleanupPromptText != null) cleanupPromptText.gameObject.SetActive(false);
                }
                return; 
            }
        }

        // Clear out text display elements if the player looks away into empty air spaces
        if (cleanupPromptText != null && cleanupPromptText.gameObject.activeSelf)
        {
            cleanupPromptText.gameObject.SetActive(false);
        }
    }
}