
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ArmadilloWander : MonoBehaviour
{
    private enum ArmadilloState
    {
        Idle,
        Walking,
        Sniffing,
        Rolling
    }

    [Header("Player Discovery")]
    [SerializeField] private string playerTag = "Player";

    [Header("Idle and Action Timing")]
    [SerializeField] private float minIdleTime = 1f;
    [SerializeField] private float maxIdleTime = 3f;

    [Header("Walking")]
    [SerializeField] private float walkSpeed = 0.8f;
    [SerializeField] private float minWalkTime = 1f;
    [SerializeField] private float maxWalkTime = 2.5f;

    [Header("Sniffing and Rooting")]
    [SerializeField] private float sniffChance = 0.3f;
    [SerializeField] private float minSniffTime = 1.5f;
    [SerializeField] private float maxSniffTime = 3f;

    [Header("Rolling")]
    [SerializeField] private float rollChance = 0.2f;
    [SerializeField] private float rollSpeed = 2f;
    [SerializeField] private float minRollTime = 1f;
    [SerializeField] private float maxRollTime = 2.5f;

    [Header("Facing")]
    [SerializeField] private bool spriteFacesRightByDefault = true;

    private Rigidbody2D rb;
    private Transform visual;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private ArmadilloState currentState = ArmadilloState.Idle;

    private Vector2 movementDirection;
    private float stateTimer;

    private static readonly int IsWalkingHash =
        Animator.StringToHash("IsWalking");

    private static readonly int SniffHash =
        Animator.StringToHash("Sniff");

    private static readonly int RollHash =
        Animator.StringToHash("Roll");

    private static readonly int EndRollHash =
        Animator.StringToHash("EndRoll");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Find the Visual child automatically.
        visual = transform.Find("Visual");

        if (visual == null)
        {
            Animator childAnimator = GetComponentInChildren<Animator>();

            if (childAnimator != null)
                visual = childAnimator.transform;
        }

        if (visual == null)
        {
            Debug.LogError(
                "ArmadilloWander: Could not find the Visual child.",
                this);

            enabled = false;
            return;
        }

        animator = visual.GetComponent<Animator>();
        spriteRenderer = visual.GetComponent<SpriteRenderer>();

        if (animator == null || spriteRenderer == null)
        {
            Debug.LogError(
                "ArmadilloWander: Visual needs an Animator and SpriteRenderer.",
                this);

            enabled = false;
            return;
        }

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;
    }

    private void Start()
    {
        EnterIdle();
    }

    private void Update()
    {
        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case ArmadilloState.Idle:
                if (stateTimer <= 0f)
                    ChooseNextAction();
                break;

            case ArmadilloState.Walking:
                if (stateTimer <= 0f)
                    EnterIdle();
                break;

            case ArmadilloState.Sniffing:
                if (stateTimer <= 0f)
                    EnterIdle();
                break;

            case ArmadilloState.Rolling:
                if (stateTimer <= 0f)
                    FinishRolling();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (currentState == ArmadilloState.Walking)
        {
            rb.MovePosition(
                rb.position +
                movementDirection * walkSpeed * Time.fixedDeltaTime);
        }
        else if (currentState == ArmadilloState.Rolling)
        {
            rb.MovePosition(
                rb.position +
                movementDirection * rollSpeed * Time.fixedDeltaTime);
        }
    }

    private void ChooseNextAction()
    {
        float roll = Random.value;

        // Roll is checked first, then sniff, otherwise walk.
        if (roll < rollChance)
        {
            BeginRolling();
        }
        else if (roll < rollChance + sniffChance)
        {
            BeginSniffing();
        }
        else
        {
            BeginWalking();
        }
    }

    private void EnterIdle()
    {
        currentState = ArmadilloState.Idle;
        movementDirection = Vector2.zero;

        animator.SetBool(IsWalkingHash, false);

        stateTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    private void BeginWalking()
    {
        currentState = ArmadilloState.Walking;

        movementDirection = GetRandomDirection();
        FaceDirection(movementDirection.x);

        animator.SetBool(IsWalkingHash, true);

        stateTimer = Random.Range(minWalkTime, maxWalkTime);
    }

    private void BeginSniffing()
    {
        currentState = ArmadilloState.Sniffing;
        movementDirection = Vector2.zero;

        animator.SetBool(IsWalkingHash, false);

        animator.ResetTrigger(SniffHash);
        animator.SetTrigger(SniffHash);

        stateTimer = Random.Range(minSniffTime, maxSniffTime);
    }

    private void BeginRolling()
    {
        currentState = ArmadilloState.Rolling;

        // Choose a random direction for the rolling movement.
        movementDirection = GetRandomDirection();
        FaceDirection(movementDirection.x);

        animator.SetBool(IsWalkingHash, false);

        animator.ResetTrigger(EndRollHash);
        animator.ResetTrigger(RollHash);
        animator.SetTrigger(RollHash);

        stateTimer = Random.Range(minRollTime, maxRollTime);
    }

    private void FinishRolling()
    {
        animator.ResetTrigger(RollHash);
        animator.SetTrigger(EndRollHash);

        EnterIdle();
    }

    private Vector2 GetRandomDirection()
    {
        Vector2[] directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            new Vector2(-1f, 1f).normalized,
            new Vector2(1f, 1f).normalized,
            new Vector2(-1f, -1f).normalized,
            new Vector2(1f, -1f).normalized
        };

        return directions[Random.Range(0, directions.Length)];
    }

    private void FaceDirection(float horizontalDirection)
    {
        if (horizontalDirection > 0.01f)
        {
            spriteRenderer.flipX = !spriteFacesRightByDefault;
        }
        else if (horizontalDirection < -0.01f)
        {
            spriteRenderer.flipX = spriteFacesRightByDefault;
        }
    }

    private void OnDisable()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}