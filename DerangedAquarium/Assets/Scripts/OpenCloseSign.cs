using UnityEngine;
using TMPro;

public class OpenCloseSign : MonoBehaviour
{
    [Header("Linked Spawner Configuration")]
    [Tooltip("Drag your TicketTurnstileGate object containing the ExhibitTicketGate script component here.")]
    public ExhibitTicketGate ticketGate;

    [Header("Visual Feedback Elements")]
    [Tooltip("Drag the TextMeshPro component that displays the text on your sign here.")]
    public TMP_Text signStatusText;
    
    public Color openColor = Color.green;
    public Color closedColor = Color.red;

    void Start()
    {
        // Safety Fallback: Automatically find the gate if you forgot to drag it into the inspector slot
        if (ticketGate == null)
        {
            ticketGate = FindFirstObjectByType<ExhibitTicketGate>();
        }

        // ===================================================================
        // --- UPGRADE 1: CANVAS CAMERA ANCHOR ---
        // If your text is nested inside a World Space UI Canvas, it needs an
        // assigned camera to render consistently from long distances. This
        // automatically binds it to your main camera layout on frame zero!
        // ===================================================================
        Canvas parentCanvas = GetComponentInChildren<Canvas>() ?? GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            if (parentCanvas.worldCamera == null)
            {
                parentCanvas.worldCamera = Camera.main;
            }
        }

        // Initialize the sign text to match the gate's starting status configuration
        RefreshSignVisuals();
    }

    void OnMouseDown()
    {
        InteractWithSign();
    }

    public void InteractWithSign()
    {
        if (ticketGate == null)
        {
            Debug.LogError("[Sign System] Cannot toggle state because no ExhibitTicketGate reference is assigned!");
            return;
        }

        // Flip the gate's boolean operational value state
        ticketGate.isOpen = !ticketGate.isOpen;

        // Repaint text layouts and text coloring to reflect the new state shift
        RefreshSignVisuals();

        string statusWord = ticketGate.isOpen ? "<color=green>OPEN</color>" : "<color=red>CLOSED</color>";
        Debug.Log($"[Sign System] Player clicked the sign. The Aquarium facility is now {statusWord} to the public!");

        // If the player successfully opens the doors while the "open_building" quest is active, register progress
        if (ticketGate.isOpen && QuestManager.Instance != null)
        {
            QuestManager.Instance.ProgressQuest("open_building", 1);
        }
    }

    /// <summary>
    /// Reads the gate's state value and adjusts colors and string values on screen.
    /// </summary>
    private void RefreshSignVisuals()
    {
        if (ticketGate == null || signStatusText == null) return;

        if (ticketGate.isOpen)
        {
            signStatusText.text = "OPEN";
            signStatusText.color = openColor;
        }
        else
        {
            signStatusText.text = "CLOSED";
            signStatusText.color = closedColor;
        }

        // ===================================================================
        // --- UPGRADE 2: FORCE GEOMETRY MESH UPDATE ---
        // Bypasses Unity's frame-delay cache. This forces the TextMeshPro component 
        // to immediately generate its character vertices and triangles right now,
        // making it instantly visible across your entire 3D showroom layout!
        // ===================================================================
        signStatusText.ForceMeshUpdate(true);
    }
}