using UnityEngine;

public class TankHierarchyTracker : MonoBehaviour
{
    public Transform foodContainer { get; private set; }
    public Transform bubbleContainer { get; private set; }

    void Awake()
    {
        // Creates clean runtime folders grouped directly under this script
        GameObject foodFolder = new GameObject("--- SPAWNED FOOD ---");
        foodFolder.transform.SetParent(this.transform);
        foodFolder.transform.localPosition = Vector3.zero;
        foodContainer = foodFolder.transform;

        GameObject bubbleFolder = new GameObject("--- SPAWNED BUBBLES ---");
        bubbleFolder.transform.SetParent(this.transform);
        bubbleFolder.transform.localPosition = Vector3.zero;
        bubbleContainer = bubbleFolder.transform;
    }
}