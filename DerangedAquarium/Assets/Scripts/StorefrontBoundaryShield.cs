using UnityEngine;

public class StorefrontBoundaryShield : MonoBehaviour
{
    public static StorefrontBoundaryShield Instance { get; private set; }

    [Header("Boundary Target Zone")]
    [Tooltip("Drag the invisible BoxCollider game object here.")]
    public BoxCollider shopBoundaryCollider;

    [Header("Object Dimensions Padding")]
    [Tooltip("Enable this to prevent large furniture edges from clipping through the walls.")]
    public bool useObjectSizePadding = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Filters an incoming target coordinate and forces it to remain entirely within the shop boundaries.
    /// </summary>
    public Vector3 GetClampedPlacementPosition(Vector3 rawTargetPosition, Vector3 itemSize)
    {
        if (shopBoundaryCollider == null)
        {
            Debug.LogWarning("[Boundary Shield] No boundary box assigned! Passing raw position.");
            return rawTargetPosition;
        }

        // Capture the absolute world edges of our invisible trigger box
        Bounds shopBounds = shopBoundaryCollider.bounds;

        // Establish half-size padding offsets so outer edges don't break the wall
        float paddingX = useObjectSizePadding ? (itemSize.x * 0.5f) : 0f;
        float paddingY = useObjectSizePadding ? (itemSize.y * 0.5f) : 0f;
        float paddingZ = useObjectSizePadding ? (itemSize.z * 0.5f) : 0f;

        // Dynamically calculate the safe zone limits
        float minAllowedX = shopBounds.min.x + paddingX;
        float maxAllowedX = shopBounds.max.x - paddingX;

        float minAllowedY = shopBounds.min.y + paddingY;
        float maxAllowedY = shopBounds.max.y - paddingY;

        float minAllowedZ = shopBounds.min.z + paddingZ;
        float maxAllowedZ = shopBounds.max.z - paddingZ;

        // Force the coordinate parameters to stay within our safe limits
        float constrainedX = Mathf.Clamp(rawTargetPosition.x, minAllowedX, maxAllowedX);
        float constrainedY = Mathf.Clamp(rawTargetPosition.y, minAllowedY, maxAllowedY);
        float constrainedZ = Mathf.Clamp(rawTargetPosition.z, minAllowedZ, maxAllowedZ);

        return new Vector3(constrainedX, constrainedY, constrainedZ);
    }
}