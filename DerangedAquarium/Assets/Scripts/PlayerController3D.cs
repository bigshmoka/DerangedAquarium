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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Freeze Rigidbody rotations so physics forces don't knock the player over
        rb.freezeRotation = true;

        // Lock the mouse cursor to the center of the game screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
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
            rb.linearVelocity = Vector3.zero; // Stops all physical momentum when interacting
            return;
        }

        // 2. Handle Keyboard WASD Movement Relative to Facing Direction
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.forward * moveZ + transform.right * moveX).normalized;
        rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);
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
}