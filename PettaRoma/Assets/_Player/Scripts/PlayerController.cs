using UnityEngine;

/// <summary>
/// Third-person player controller using CharacterController.
/// Movement is relative to camera direction.
/// Reads input from InputManager.
///
/// Prefab structure:
///   Player_PettaChef
///     ├── Model_Root   (empty — swap in real model later)
///     ├── Weapon_Root  (empty — fork attachment later)
///     └── [CharacterController] + [PlayerController]
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private PlayerStats stats;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform modelRoot; // for rotation visual

    // ── Internal state ──
    private CharacterController cc;
    private float verticalVelocity;
    private bool isGrounded;
    private Vector3 lastMoveDir;

    // ── Animation state (for future Animator hookup) ──
    public float MoveSpeed01 { get; private set; }  // 0 = idle, 1 = max speed
    public bool IsGrounded => isGrounded;
    public bool IsJumping { get; private set; }

    private Animator animator;

    void Start()
    {
        cc = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }

        // Find Animator on model
        if (modelRoot != null)
            animator = modelRoot.GetComponentInChildren<Animator>();

        // Spawn at checkpoint if available
        if (GameManager.Instance != null)
        {
            Vector3 spawn = GameManager.Instance.GetSpawnPosition();
            cc.enabled = false;
            transform.position = spawn;
            cc.enabled = true;
        }
    }

    void Update()
    {
        if (InputManager.Instance == null || stats == null) return;

        GroundCheck();
        HandleJump();
        HandleMovement();
        ApplyGravity();

        // Final move
        Vector3 motion = Vector3.zero;
        motion.x = lastMoveDir.x;
        motion.z = lastMoveDir.z;
        motion.y = verticalVelocity;
        cc.Move(motion * Time.deltaTime);

        // Update animator
        UpdateAnimator();
    }

    void GroundCheck()
    {
        // CharacterController.isGrounded + sphere cast for reliability
        isGrounded = cc.isGrounded;

        if (!isGrounded)
        {
            // Extra check with small sphere cast downward
            Vector3 origin = transform.position + Vector3.up * stats.groundCheckOffset;
            isGrounded = Physics.SphereCast(origin, stats.groundCheckRadius,
                Vector3.down, out _, stats.groundCheckOffset + 0.1f);
        }

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f; // small push to stay grounded
            IsJumping = false;
        }
    }

    void HandleJump()
    {
        if (InputManager.Instance.ConsumeJump() && isGrounded)
        {
            verticalVelocity = stats.jumpForce;
            IsJumping = true;
        }
    }

    void HandleMovement()
    {
        Vector2 input = InputManager.Instance.MoveInput;

        if (input.sqrMagnitude < 0.01f)
        {
            lastMoveDir = Vector3.zero;
            MoveSpeed01 = 0f;
            return;
        }

        // ── Camera-relative direction ──
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f; camForward.Normalize();
        camRight.y = 0f;   camRight.Normalize();

        Vector3 moveDir = camForward * input.y + camRight * input.x;
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        // Speed
        bool sprinting = InputManager.Instance.SprintHeld;
        float speed = sprinting ? stats.runSpeed : stats.walkSpeed;
        lastMoveDir = moveDir * speed;

        // MoveSpeed01: 0 = idle, 0.5 = walk, 1.0 = run
        MoveSpeed01 = sprinting ? 1f : 0.5f;

        // ── Rotate model to face movement ──
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            Transform rotTarget = modelRoot != null ? modelRoot : transform;
            rotTarget.rotation = Quaternion.RotateTowards(
                rotTarget.rotation, targetRot, stats.rotationSpeed * Time.deltaTime);

            // Also rotate the root if no separate model
            if (modelRoot == null)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot, stats.rotationSpeed * Time.deltaTime);
        }
    }

    void ApplyGravity()
    {
        verticalVelocity += stats.gravity * Time.deltaTime;

        // Clamp terminal velocity
        if (verticalVelocity < -30f) verticalVelocity = -30f;
    }

    /// <summary>
    /// Teleport player to a position (e.g., checkpoint respawn).
    /// </summary>
    public void TeleportTo(Vector3 pos)
    {
        cc.enabled = false;
        transform.position = pos;
        cc.enabled = true;
        verticalVelocity = 0f;
    }

    void UpdateAnimator()
    {
        // Lazy-init: find animator if not yet found (e.g., model applied after Start)
        if (animator == null && modelRoot != null)
            animator = modelRoot.GetComponentInChildren<Animator>();

        if (animator == null) return;
        animator.SetFloat("Speed", MoveSpeed01);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsJumping", IsJumping);
    }
}
