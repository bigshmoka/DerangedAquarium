using UnityEngine;

public class PlayerController3D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;

    [Header("References")]
    public Transform playerCamera;

    private float xRotation = 0f;
    private Rigidbody rb;
    private bool isLocked = false;

    // --- NOCLIP FLIGHT VARIABLES ---
    private bool isNoclip = false;
    private Collider playerCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        
        // Freeze Rigidbody rotations so physics forces don't knock the player over
        rb.freezeRotation = true;

        // Lock the mouse cursor to the center of the game screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // ===================================================================
        // --- THE FIX: THE CONTINUOUS CROSSHAIR GUARD LOCK ---
        // If the player profile is unlocked (meaning you are walking the 3D room), 
        // force the cursor to lock and remain hidden every frame. This completely
        // overrides and suppresses next-frame anomalies from TextMeshPro input fields!
        // ===================================================================
        if (!isLocked)
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (isLocked) return;

        // 1. Handle Mouse Look Look Up/Down & Left/Right
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Prevents flipping upside down

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        transform.Rotate(Vector3.up * mouseX);
    }

    void FixedUpdate()
    {
        if (isLocked)
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero; 
            }
            return;
        }

        // --- NOCLIP FLIGHT NAVIGATION CONTROL ENGINE ---
        if (isNoclip)
        {
            float flyX = Input.GetAxisRaw("Horizontal");
            float flyZ = Input.GetAxisRaw("Vertical");
            
            // Explicit vertical keyboard lift mechanics
            float flyY = 0f;
            if (Input.GetKey(KeyCode.Space)) flyY = 1f;
            if (Input.GetKey(KeyCode.LeftShift)) flyY = -1f;

            Vector3 flyDirection = Vector3.zero;

            // Drive travel vector along the exact pitch and yaw direction of the player camera look vector
            if (playerCamera != null)
            {
                flyDirection = (playerCamera.forward * flyZ + playerCamera.right * flyX).normalized;
            }
            else
            {
                flyDirection = (transform.forward * flyZ + transform.right * flyX).normalized;
            }

            // Combine standard horizontal vector mappings with manual elevation values
            flyDirection += Vector3.up * flyY;
            if (flyDirection.sqrMagnitude > 0.01f) flyDirection.Normalize();

            // Translate player position through space smoothly (with a speed multiplier for comfort)
            transform.position += flyDirection * (moveSpeed * 2.5f) * Time.fixedDeltaTime;
            return;
        }

        // 2. Handle Keyboard WASD Movement Relative to Facing Direction (Standard Ground Physics)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.forward * moveZ + transform.right * moveX).normalized;
        
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);
        }
    }

    // Call this from other scripts to freeze the player when looking at the fish tank
    public void SetPlayerLockState(bool shouldLock)
    {
        isLocked = shouldLock;
        
        if (shouldLock)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // --- SYSTEM INTERFACE TOGGLE FOR NOCLIP CHEAT ENGINE ---
    public bool ToggleNoclip()
    {
        isNoclip = !isNoclip;

        if (rb != null)
        {
            if (isNoclip)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            else
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
            }
        }

        // Turning off the collider allows passing completely through wall/floor meshes
        if (playerCollider == null) playerCollider = GetComponent<Collider>();
        if (playerCollider != null)
        {
            playerCollider.enabled = !isNoclip;
        }

        return isNoclip;
    }
}