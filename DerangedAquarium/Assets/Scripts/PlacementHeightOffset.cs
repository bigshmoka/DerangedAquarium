using UnityEngine;

public class PlacementHeightOffset : MonoBehaviour
{
    [Header("Modular Offset Adjustments")]
    [Tooltip("Manually add a vertical offset value specifically for this prefab model if its pivot causes it to sink through floors.")]
    public float heightOffset = 0f;
}