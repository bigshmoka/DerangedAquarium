using UnityEngine;
using System.Collections.Generic;

public class AlgaeManager : MonoBehaviour
{
    [Header("Node Tracking")]
    [Tooltip("The list of AlgaeNode objects controlled by this specific manager.")]
    public AlgaeNode[] algaeNodes;

    void Start()
    {
        // ===================================================================
        // --- FIXED: AUTOMATIC LOCAL CHILD DISCOVERY ---
        // Instead of relying on manual inspector drag-and-drops (which fail on duplication),
        // this automatically finds every AlgaeNode inside this specific tank's branch.
        // ===================================================================
        algaeNodes = GetComponentsInChildren<AlgaeNode>(true);
        
        if (algaeNodes.Length == 0)
        {
            Debug.LogWarning($"[AlgaeManager] {gameObject.name} could not find any AlgaeNode children! Check your hierarchy.");
        }
    }

    public AlgaeNode GetDirtiestAlgaeNode()
    {
        AlgaeNode dirtiest = null;
        float highestAlgae = -1f;

        foreach (AlgaeNode node in algaeNodes)
        {
            if (node != null && node.currentAlgaeLevel > highestAlgae)
            {
                highestAlgae = node.currentAlgaeLevel;
                dirtiest = node;
            }
        }
        return dirtiest;
    }
}