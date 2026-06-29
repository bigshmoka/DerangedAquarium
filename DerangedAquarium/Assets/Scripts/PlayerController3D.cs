using UnityEngine;

public class PlayerController3D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;

    [Header("Advanced Locomotion Settings")]
    public float sprintSpeed = 8.5f;
    public float crouchSpeed = 2.5f;
    public float jumpForce = 6f;
    public float crouchLerpSpeed = 10f;
    
    // Custom gravity pulls you down smoothly without needing Unity's gravity checkbox!
    public float customGravity = -15f; 

    [Header("References")]
    public Transform playerCamera;

    private float xRotation = 0f;
    private Rigidbody rb;
    private bool isLocked = false;
    private Collider playerCollider;

    // --- NOCLIP FLIGHT VARIABLES ---
    private bool isNoclip = false;

    // --- LOCOMOTION STATE MACHINE TRACKERS ---
    private bool isCrouching = false;
    private bool readyToJump = false;
    private bool isGrounded = false;
    
    // --- CAMERA CROUCH TRACKERS ---
    private float standingCamHeight;
    private float crouchingCamHeight;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        if (playerCollider == null) playerCollider = GetComponentInChildren<Collider>();
        
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.useGravity = false; // Explicitly force Unity's gravity OFF
            rb.WakeUp();
        }

        // Camera heights calculation cache engine
        if (playerCamera != null)
        {
            standingCamHeight = playerCamera.localPosition.y;
            crouchingCamHeight = standingCamHeight * 0.5f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Continuous Crosshair Guard Lock
        if (!isLocked)
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (isLocked) return;

        // 1. Mouse Look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        transform.Rotate(Vector3.up * mouseX);

        // 2. Continually verify if we are standing on the floor
        isGrounded = IsGroundedUniversal();

        // 3. Inputs
        if (!isNoclip)
        {
            isCrouching = Input.GetKey(KeyCode.LeftControl);

            // Fire jump if space is hit AND we are touching the floor
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                readyToJump = true;
            }
        }
        else
        {
            isCrouching = false;
            readyToJump = false;
        }

        // 4. Smooth Camera Crouch (Runs in Update so it works mid-air or on ground!)
        HandleSmoothCameraCrouch();
    }

    void FixedUpdate()
    {
        if (isLocked)
        {
            if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero; 
            return;
        }

        // --- NOCLIP ---
        if (isNoclip)
        {
            float flyX = Input.GetAxisRaw("Horizontal");
            float flyZ = Input.GetAxisRaw("Vertical");
            float flyY = 0f;
            
            if (Input.GetKey(KeyCode.Space)) flyY = 1f;
            if (Input.GetKey(KeyCode.LeftShift)) flyY = -1f;

            Vector3 flyDirection = Vector3.zero;
            if (playerCamera != null) flyDirection = (playerCamera.forward * flyZ + playerCamera.right * flyX).normalized;
            else flyDirection = (transform.forward * flyZ + transform.right * flyX).normalized;

            flyDirection += Vector3.up * flyY;
            if (flyDirection.sqrMagnitude > 0.01f) flyDirection.Normalize();

            transform.position += flyDirection * (moveSpeed * 2.5f) * Time.fixedDeltaTime;
            return;
        }

        // --- MOVEMENT & PHYSICS ---
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.forward * moveZ + transform.right * moveX).normalized;
        
        // ===================================================================
        // --- FIXED: GROUND-GATED STANCE SPEED SELECTION ---
        // Determines target velocity relative to ground contact states.
        // Sprint modifier applies globally, but the crouch speed slow-down penalty 
        // is strictly gated behind being firmly touching the floor plates!
        // ===================================================================
        float currentSpeed = moveSpeed;

        if (Input.GetKey(KeyCode.LeftShift)) 
        {
            currentSpeed = sprintSpeed;
        }

        // If crouching but in mid-air (isGrounded == false), this speed penalty is completely bypassed!
        if (isCrouching && isGrounded) 
        {
            currentSpeed = crouchSpeed;
        }

        if (rb != null && !rb.isKinematic)
        {
            float currentYVelocity = rb.linearVelocity.y;

            if (readyToJump)
            {
                // Shoot upward instantly
                currentYVelocity = jumpForce;
                readyToJump = false; 
            }
            else if (!isGrounded)
            {
                // Pull downward over time if we are in mid-air via our custom gravity engine
                currentYVelocity += customGravity * Time.fixedDeltaTime;
            }
            else 
            {
                // Set velocity to exactly 0 when on the ground to clear stutters
                currentYVelocity = 0f; 
            }

            // Apply the calculated Y velocity alongside your ground-gated horizontal momentum speeds
            Vector3 targetVelocity = new Vector3(moveDirection.x * currentSpeed, currentYVelocity, moveDirection.z * currentSpeed);
            rb.linearVelocity = targetVelocity;
        }
    }

    // ===================================================================
    // --- THE UNIVERSAL GROUND CHECK ---
    // Mathematically locates the absolute bottom of your player collider
    // and checks precisely below it. Works completely independent of pivot points.
    // ===================================================================
    private bool IsGroundedUniversal()
    {
        if (playerCollider == null) return true; 

        // Find the absolute lowest world-space point of your collider (the bottom of your feet)
        Vector3 bottomPoint = new Vector3(transform.position.x, playerCollider.bounds.min.y, transform.position.z);

        // Start checking slightly above the bottom edge so the ray doesn't clip underneath the floor
        Vector3 startPoint = bottomPoint + (Vector3.up * 0.1f);
        float checkDistance = 0.2f;

        // Raycast against EVERYTHING (~0), ignoring invisible triggers
        RaycastHit[] hits = Physics.RaycastAll(startPoint, Vector3.down, checkDistance, ~0, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            // Ignore hitting ourselves
            if (hit.collider == playerCollider) continue;
            if (hit.transform.IsChildOf(transform)) continue;

            // If we hit anything else physical, we are firmly on the ground
            return true; 
        }

        return false;
    }

    // Camera height interpolation engine translates view paths smoothly
    private void HandleSmoothCameraCrouch()
    {
        if (playerCamera == null) return;

        float targetY = isCrouching ? crouchingCamHeight : standingCamHeight;
        Vector3 camPos = playerCamera.localPosition;
        
        camPos.y = Mathf.Lerp(camPos.y, targetY, Time.deltaTime * crouchLerpSpeed);
        playerCamera.localPosition = camPos;
    }

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

    public bool ToggleNoclip()
    {
        isNoclip = !isNoclip;
        if (rb != null)
        {
            if (isNoclip)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            else
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
            }
        }
        if (playerCollider != null) playerCollider.enabled = !isNoclip;
        return isNoclip;
    }
}