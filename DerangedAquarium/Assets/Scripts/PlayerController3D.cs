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

    [Header("Head Bobbing Settings")]
    [Tooltip("How fast the head bobs up and down while walking.")]
    public float walkBobFrequency = 14f;
    [Tooltip("How high/low the camera bobs while walking.")]
    public float walkBobAmount = 0.04f;

    [Tooltip("How fast the head bobs up and down while sprinting.")]
    public float sprintBobFrequency = 18f;
    [Tooltip("How high/low the camera bobs while sprinting.")]
    public float sprintBobAmount = 0.08f;

    [Tooltip("How fast the head bobs up and down while crouching.")]
    public float crouchBobFrequency = 9f;
    [Tooltip("How high/low the camera bobs while crouching.")]
    public float crouchBobAmount = 0.02f;

    [Tooltip("How fast the camera returns to center when you stop moving.")]
    public float bobResetReturnSpeed = 15f;

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
    
    // --- CAMERA EFFECTS ENGINE FIELDS ---
    private float standingCamHeight;
    private float crouchingCamHeight;
    private float baseCameraY;
    private float bobTimer = 0f;

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
            baseCameraY = standingCamHeight; // Initialize base slider tracker
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

        // 4. Combined Camera Couch Stance & Head Bobbing Engine
        HandleCameraEffectsAndBobbing();
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
        
        float currentSpeed = moveSpeed;

        if (Input.GetKey(KeyCode.LeftShift)) 
        {
            currentSpeed = sprintSpeed;
        }

        if (isCrouching && isGrounded) 
        {
            currentSpeed = crouchSpeed;
        }

        if (rb != null && !rb.isKinematic)
        {
            float currentYVelocity = rb.linearVelocity.y;

            if (readyToJump)
            {
                currentYVelocity = jumpForce;
                readyToJump = false; 
            }
            else if (!isGrounded)
            {
                currentYVelocity += customGravity * Time.fixedDeltaTime;
            }
            else 
            {
                currentYVelocity = 0f; 
            }

            Vector3 targetVelocity = new Vector3(moveDirection.x * currentSpeed, currentYVelocity, moveDirection.z * currentSpeed);
            rb.linearVelocity = targetVelocity;
        }
    }

    private bool IsGroundedUniversal()
    {
        if (playerCollider == null) return true; 

        Vector3 bottomPoint = new Vector3(transform.position.x, playerCollider.bounds.min.y, transform.position.z);
        Vector3 startPoint = bottomPoint + (Vector3.up * 0.1f);
        float checkDistance = 0.2f;

        RaycastHit[] hits = Physics.RaycastAll(startPoint, Vector3.down, checkDistance, ~0, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == playerCollider) continue;
            if (hit.transform.IsChildOf(transform)) continue;

            return true; 
        }

        return false;
    }

    // ===================================================================
    // --- ADVANCED CAMERA EFFECTS ENGINE (Crouch Stance + Head Bob) ---
    // Simulates natural organic weight shifting while walking.
    // Seamlessly handles transitioning heights without fighting crouch code arrays!
    // ===================================================================
    private void HandleCameraEffectsAndBobbing()
    {
        if (playerCamera == null) return;

        // 1. STANCE COMPILER: Smoothly slide the baseline height anchor based on crouch states
        float targetBaseY = isCrouching ? crouchingCamHeight : standingCamHeight;
        baseCameraY = Mathf.Lerp(baseCameraY, targetBaseY, Time.deltaTime * crouchLerpSpeed);

        // Prepare our baseline targeted point vector
        Vector3 targetLocalPosition = new Vector3(0f, baseCameraY, playerCamera.localPosition.z);

        // 2. CHECK MOTION INPUTS: Are we moving the joystick or WASD keys?
        float moveInputX = Input.GetAxisRaw("Horizontal");
        float moveInputZ = Input.GetAxisRaw("Vertical");
        bool isInputMoving = (Mathf.Abs(moveInputX) > 0.1f || Mathf.Abs(moveInputZ) > 0.1f);

        // Gating conditions: Only play head bobs if grounded, not floating in noclip, and actively moving
        if (isGrounded && !isNoclip && isInputMoving)
        {
            // Pick our configuration modifiers matching our active locomotion state
            float activeFrequency = walkBobFrequency;
            float activeAmount = walkBobAmount;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                activeFrequency = sprintBobFrequency;
                activeAmount = sprintBobAmount;
            }
            else if (isCrouching)
            {
                activeFrequency = crouchBobFrequency;
                activeAmount = crouchBobAmount;
            }

            // Advance the periodic timer tracking step intervals
            bobTimer += Time.deltaTime * activeFrequency;

            // Generate structural wave displacements
            float waveOffsetY = Mathf.Sin(bobTimer) * activeAmount; 
            float waveOffsetX = Mathf.Cos(bobTimer * 0.5f) * (activeAmount * 0.6f); // Shifting weight side-to-side

            // Inject structural offsets into target transformation matrix positions
            targetLocalPosition.x += waveOffsetX;
            targetLocalPosition.y += waveOffsetY;
        }
        else
        {
            // Reset the internal clock smoothly when sitting completely still so steps don't look stuttered on initialization
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * bobResetReturnSpeed);
        }

        // 3. APPLY LAYER SHIFT: Interpolate view transforms seamlessly to filter out physics tremors
        playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, targetLocalPosition, Time.deltaTime * bobResetReturnSpeed);
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