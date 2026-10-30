
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class AlligatorWander : MonoBehaviour
{
    private enum AlligatorState
    {
        Moving,
        Idle,
        Snapping,
        Napping,
        Sunning
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float minMoveTime = 1.5f;
    [SerializeField] private float maxMoveTime = 4f;
    [SerializeField] private float minIdleTime = 1f;
    [SerializeField] private float maxIdleTime = 3f;

    [Header("Special Behaviors")]
    [Range(0f, 1f)]
    [SerializeField] private float specialActionChance = 0.25f;

    [SerializeField] private float snapDuration = 0.8f;
    [SerializeField] private float minNapTime = 5f;
    [SerializeField] private float maxNapTime = 10f;
    [SerializeField] private float minSunTime = 4f;
    [SerializeField] private float maxSunTime = 8f;

    [Header("Animation")]
    [SerializeField] private bool spriteFacesRightByDefault = true;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private AlligatorState currentState;
    private Vector2 moveDirection;
    private float stateTimer;

    private static readonly int IsWalkingHash =
        Animator.StringToHash("IsWalking");

    private static readonly int SnapHash =
        Animator.StringToHash("Snap");

    private static readonly int NapHash =
        Animator.StringToHash("Nap");

    private static readonly int SunHash =
        Animator.StringToHash("Sun");

    private static readonly int WakeUpHash =
        Animator.StringToHash("WakeUp");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        rb.freezeRotation = true;
        rb.gravityScale = 0f;
    }

    private void Start()
    {
        BeginIdle(Random.Range(minIdleTime, maxIdleTime));
    }

    private void Update()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
            return;

        switch (currentState)
        {
            case AlligatorState.Moving:
                BeginIdle(Random.Range(minIdleTime, maxIdleTime));
                break;

            case AlligatorState.Idle:
                ChooseNextBehavior();
                break;

            case AlligatorState.Snapping:
                BeginIdle(Random.Range(minIdleTime, maxIdleTime));
                break;

            case AlligatorState.Napping:
            case AlligatorState.Sunning:
                EndRest();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (currentState == AlligatorState.Moving)
            rb.linearVelocity = moveDirection * moveSpeed;
        else
            rb.linearVelocity = Vector2.zero;
    }

    private void ChooseNextBehavior()
    {
        if (Random.value > specialActionChance)
        {
            BeginMoving();
            return;
        }

        int choice = Random.Range(0, 3);

        switch (choice)
        {
            case 0:
                BeginSnapping();
                break;

            case 1:
                BeginNapping();
                break;

            case 2:
                BeginSunning();
                break;
        }
    }

    private void BeginMoving()
    {
        currentState = AlligatorState.Moving;
        stateTimer = Random.Range(minMoveTime, maxMoveTime);

        Vector2[] directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            new Vector2(1f, 1f).normalized,
            new Vector2(1f, -1f).normalized,
            new Vector2(-1f, 1f).normalized,
            new Vector2(-1f, -1f).normalized
        };

        moveDirection = directions[Random.Range(0, directions.Length)];

        UpdateFacing();

        animator.SetBool(IsWalkingHash, true);
    }

    private void BeginIdle(float duration)
    {
        currentState = AlligatorState.Idle;
        stateTimer = duration;
        moveDirection = Vector2.zero;

        animator.SetBool(IsWalkingHash, false);
    }

    private void BeginSnapping()
    {
        currentState = AlligatorState.Snapping;
        stateTimer = snapDuration;
        moveDirection = Vector2.zero;

        animator.SetBool(IsWalkingHash, false);

        animator.ResetTrigger(SnapHash);
        animator.SetTrigger(SnapHash);
    }

    private void BeginNapping()
    {
        currentState = AlligatorState.Napping;
        stateTimer = Random.Range(minNapTime, maxNapTime);
        moveDirection = Vector2.zero;

        animator.SetBool(IsWalkingHash, false);

        animator.ResetTrigger(NapHash);
        animator.SetTrigger(NapHash);
    }

    private void BeginSunning()
    {
        currentState = AlligatorState.Sunning;
        stateTimer = Random.Range(minSunTime, maxSunTime);
        moveDirection = Vector2.zero;

        animator.SetBool(IsWalkingHash, false);

        animator.ResetTrigger(SunHash);
        animator.SetTrigger(SunHash);
    }

    private void EndRest()
    {
        // Start the idle timer, but let the Animator transition
        // out of Nap/Sun using the WakeUp trigger.
        currentState = AlligatorState.Idle;
        stateTimer = Random.Range(minIdleTime, maxIdleTime);
        moveDirection = Vector2.zero;

        animator.SetBool(IsWalkingHash, false);

        animator.ResetTrigger(WakeUpHash);
        animator.SetTrigger(WakeUpHash);
    }

    private void UpdateFacing()
    {
        // Vertical movement preserves the previous facing direction.
        if (moveDirection.x > 0.01f)
            spriteRenderer.flipX = !spriteFacesRightByDefault;
        else if (moveDirection.x < -0.01f)
            spriteRenderer.flipX = spriteFacesRightByDefault;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState == AlligatorState.Moving)
            BeginMoving();
    }
}
