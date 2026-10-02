using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerSurfaceController))]
[RequireComponent(typeof(PlayerJuice))]
public class PlayerController : MonoBehaviour
{
    [Header("Feature Toggles")]
    [Tooltip("Enable mouse aiming & strafe lock system. Disable for classic movement.")]
    [SerializeField] private bool enableAimLock = false;

    [Header("Kick & Charge Settings")]
    [SerializeField] private KeyCode kickKey = KeyCode.Space;
    [SerializeField] private float maxKickChargeTime = 1.0f;

    [Header("Dash Settings")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("Aim Lock & Strafe Settings")]
    [SerializeField] private KeyCode aimLockKey = KeyCode.LeftControl;
    [SerializeField] private float aimLockSpeedMultiplier = 0.65f; // Strafe speed while aiming/charging
    [SerializeField] private bool useMouseAimForLock = true; // Rotates direction toward mouse while aiming/charging

    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionDistance = 1.0f;
    [SerializeField] private float interactionRadius = 0.5f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Kick Impact Detection")]
    [SerializeField] private LayerMask kickableLayers;
    [SerializeField] private float kickReachDistance = 0.8f;
    [SerializeField] private float kickRadius = 0.4f;

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerSurfaceController surfaceController;
    private PlayerJuice playerJuice;

    private Vector2 movement;
    private Vector2 lastDirection = Vector2.down;

    // State Tracking
    private bool isFrozen = false;
    private bool isDashing;
    private float dashTimer;
    private float cooldownTimer;
    private Vector2 dashDirection;

    private bool isChargingKick;
    private float kickChargeTimer;
    private bool isAimLocked;

    public bool IsFrozen => isFrozen;
    public bool IsDashing => isDashing;
    public bool IsAimLocked => isAimLocked;
    public bool IsChargingKick => isChargingKick;
    public Vector2 LastDirection => lastDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        surfaceController = GetComponent<PlayerSurfaceController>();
        playerJuice = GetComponent<PlayerJuice>();
    }

    private void Start()
    {
        DialogueManager.OnDialogueOpened += FreezePlayer;
        DialogueManager.OnDialogueClosed += UnfreezePlayer;
    }

    private void OnDisable()
    {
        movement = Vector2.zero;
        isDashing = false;
        isChargingKick = false;
        isAimLocked = false;

        if (surfaceController != null) surfaceController.ResetVelocity();
        if (playerJuice != null) playerJuice.ResetJuice();

        // Only zero out speed if the GameObject itself is actually being disabled/destroyed
        if (animator != null && !gameObject.activeInHierarchy) 
        {
            animator.SetFloat("Speed", 0f);
        }

        if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
    }

    private void OnDestroy()
    {
        DialogueManager.OnDialogueOpened -= FreezePlayer;
        DialogueManager.OnDialogueClosed -= UnfreezePlayer;
    }

    private void Update()
    {
        // Ignore all player inputs if frozen for a cutscene or dialogue
        if (isFrozen) return;

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (!isDashing)
        {
            // 1. Read Movement Inputs
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");

            // 2. Handle Aiming & Facing Direction based on Toggle
            if (enableAimLock)
            {
                // Aim Lock mode: mouse aiming / strafe override
                isAimLocked = Input.GetKey(aimLockKey) || isChargingKick;

                if (isAimLocked)
                {
                    if (useMouseAimForLock && Camera.main != null)
                    {
                        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        Vector2 aimDir = ((Vector2)mousePos - (Vector2)transform.position).normalized;
                        if (aimDir != Vector2.zero) lastDirection = aimDir;
                    }
                }
                else if (movement != Vector2.zero)
                {
                    lastDirection = movement.normalized;
                }
            }
            else
            {
                // Classic mode: standard movement direction tracking
                isAimLocked = false;

                if (movement != Vector2.zero)
                {
                    lastDirection = movement.normalized;
                }
            }

            // 3. Dash Trigger (Shift Key)
            if (Input.GetKeyDown(dashKey) && cooldownTimer <= 0f && !isChargingKick)
            {
                StartDash();
            }

            // 4. Kick Charge Handling
            HandleKickCharging();
        }

        UpdateAnimator();
        UpdateInteractionPrompt();

        if (Input.GetKeyDown(interactKey) && !isDashing && !isChargingKick)
        {
            TryInteract();
        }
    }

    private void FixedUpdate()
    {
        // Don't process physics movement if frozen
        if (isFrozen) return;

        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            surfaceController.PerformDash(dashDirection, dashSpeed, dashDuration);

            if (dashTimer <= 0f) EndDash();
        }
        else
        {
            // Apply strafe speed multiplier ONLY if aim lock feature is active AND active
            bool applyStrafeSpeed = enableAimLock && isAimLocked;
            Vector2 processedMovement = applyStrafeSpeed ? movement * aimLockSpeedMultiplier : movement;
            
            surfaceController.ProcessMovement(processedMovement);
        }
    }

    private void HandleKickCharging()
    {
        if (Input.GetKeyDown(kickKey))
        {
            isChargingKick = true;
            kickChargeTimer = 0f;
        }

        if (isChargingKick)
        {
            if (Input.GetKey(kickKey))
            {
                kickChargeTimer += Time.deltaTime;
                float chargeRatio = Mathf.Clamp01(kickChargeTimer / maxKickChargeTime);
                playerJuice.ApplyChargeWindup(chargeRatio);
            }

            if (Input.GetKeyUp(kickKey))
            {
                float finalRatio = Mathf.Clamp01(kickChargeTimer / maxKickChargeTime);
                
                playerJuice.TriggerKickRelease(lastDirection, finalRatio);
                PerformKickImpactCheck(finalRatio);

                if (animator != null) animator.SetTrigger("Kick");

                isChargingKick = false;
                kickChargeTimer = 0f;
            }
        }
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        cooldownTimer = dashCooldown;
        dashDirection = movement != Vector2.zero ? movement.normalized : lastDirection;

        playerJuice.TriggerDashJuice(dashDirection);

        if (animator != null) animator.SetTrigger("Dash");
    }

    private void EndDash()
    {
        isDashing = false;
    }

    private void UpdateAnimator()
    {
        if (animator != null)
        {
            animator.SetFloat("MoveX", lastDirection.x);
            animator.SetFloat("MoveY", lastDirection.y);
            animator.SetFloat("Speed", isDashing ? dashSpeed : movement.sqrMagnitude);
            animator.SetBool("IsDashing", isDashing);
        }
    }

    private void UpdateInteractionPrompt()
    {
        if (isDashing || isChargingKick)
        {
            if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
            return;
        }

        IInteractable target = GetBestInteractable();

        if (target != null && InteractionUI.Instance != null)
        {
            string promptText = target.GetInteractionPrompt();
            if (!string.IsNullOrEmpty(promptText))
            {
                InteractionUI.Instance.ShowPrompt(promptText);
                return;
            }
        }

        if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
    }

    private IInteractable GetBestInteractable()
    {
        Vector2 interactionPoint = (Vector2)transform.position + (lastDirection.normalized * interactionDistance);
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(interactionPoint, interactionRadius, interactableLayer);

        IInteractable bestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D col in hitColliders)
        {
            if (col.TryGetComponent<IInteractable>(out var interactable))
            {
                if (string.IsNullOrEmpty(interactable.GetInteractionPrompt())) continue;

                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    bestTarget = interactable;
                }
            }
        }

        return bestTarget;
    }

    private void PerformKickImpactCheck(float chargeRatio)
    {
        Vector2 impactPoint = (Vector2)transform.position + (lastDirection.normalized * kickReachDistance);
        Collider2D[] hits = Physics2D.OverlapCircleAll(impactPoint, kickRadius, kickableLayers);

        if (hits.Length > 0)
        {
            float shakeIntensity = Mathf.Lerp(0.08f, 0.35f, chargeRatio);
            float shakeDuration = Mathf.Lerp(0.1f, 0.25f, chargeRatio);
            float hitStopDuration = Mathf.Lerp(0.03f, 0.12f, chargeRatio);

            if (CameraJuice.Instance != null)
            {
                CameraJuice.Instance.TriggerImpactJuice(shakeIntensity, shakeDuration, hitStopDuration);
            }

            foreach (Collider2D col in hits)
            {
                if (col.TryGetComponent<IKickable>(out var kickable))
                {
                    kickable.OnKicked(lastDirection, chargeRatio);
                }
            }
        }
    }

    private void TryInteract()
    {
        IInteractable target = GetBestInteractable();
        if (target != null) target.Interact();
    }

    /// <summary>
    /// Call this from Timeline / CutsceneManager to freeze player input during cutscenes.
    /// </summary>
    public void FreezePlayer()
    {
        isFrozen = true;
        movement = Vector2.zero;
        isDashing = false;
        isChargingKick = false;
        isAimLocked = false;

        if (surfaceController != null) surfaceController.ResetVelocity();
        if (playerJuice != null) playerJuice.ResetJuice();

        if (InteractionUI.Instance != null) InteractionUI.Instance.HidePrompt();
    }

    /// <summary>
    /// Call this when the cutscene ends to give control back to the player.
    /// </summary>
    public void UnfreezePlayer()
    {
        isFrozen = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector2 interactionPoint = (Vector2)transform.position + (lastDirection.normalized * interactionDistance);
        Gizmos.DrawWireSphere(interactionPoint, interactionRadius);
    }

    public void SetFacingDirection(Vector2 newDirection)
    {
        if (newDirection == Vector2.zero) return;

        lastDirection = newDirection.normalized;

        if (animator != null)
        {
            animator.SetFloat("MoveX", lastDirection.x);
            animator.SetFloat("MoveY", lastDirection.y);
        }
    }

    public void SetFacingDirection(FacingDirection direction)
    {
        Vector2 dirVector = Vector2.down;

        switch (direction)
        {
            case FacingDirection.Up:    dirVector = Vector2.up; break;
            case FacingDirection.Down:  dirVector = Vector2.down; break;
            case FacingDirection.Left:  dirVector = Vector2.left; break;
            case FacingDirection.Right: dirVector = Vector2.right; break;
        }

        SetFacingDirection(dirVector);
    }
}