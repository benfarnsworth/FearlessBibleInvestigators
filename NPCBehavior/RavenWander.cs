
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RavenWander : MonoBehaviour
{
    private enum RavenState { Grounded, Escaping }

    [Header("Player Detection")]
    [SerializeField] private float detectionRadius = 2.5f;
    [SerializeField] private string playerTag = "Player";

    [Header("Ground Behavior")]
    [SerializeField] private float minActionDelay = 1.5f;
    [SerializeField] private float maxActionDelay = 3.5f;
    [SerializeField] private float minActionDuration = 0.5f;
    [SerializeField] private float maxActionDuration = 1.2f;

    [Range(0f, 1f)]
    [SerializeField] private float peckChance = 0.4f;

    [Header("Ground Movement")]
    [SerializeField] private float hopDistance = 0.6f;
    [SerializeField] private float hopMoveDuration = 0.3f;

    [Header("Flying Escape")]
    [SerializeField] private float flightSpeed = 7f;
    [SerializeField] private float screenMargin = 0.15f;
    [SerializeField] private float takeoffHeight = 2f;

    [Tooltip("Minimum upward world-space direction while fleeing.")]
    [SerializeField, Range(0.1f, 1f)]
    private float upwardEscapeBias = 0.55f;

    [Tooltip("Failsafe: deactivate the raven after this many seconds.")]
    [SerializeField] private float maxEscapeDuration = 5f;

    [Header("Facing")]
    [SerializeField] private bool spriteFacesRightByDefault = true;

    [Header("Flying Render Layer")]
    [SerializeField] private string flyingSortingLayer = "Foreground";

    private string originalSortingLayer;
    private SpriteRenderer ravenSprite;

    private Rigidbody2D rb;
    private Collider2D[] ravenColliders;
    private Transform player;
    private Transform visual;
    private Camera gameCamera;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private RavenState currentState = RavenState.Grounded;
    private Vector3 visualStartLocalPosition;
    private Vector2 hopStartPosition;
    private Vector2 hopTargetPosition;
    private Vector2 hopDirection;
    private Vector2 escapeDirection;

    private float actionTimer;
    private float hopTimer;
    private float escapeStartTime;
    private bool isHopping;

    private static readonly int HopHash =
        Animator.StringToHash("Hop");

    private static readonly int PeckHash =
        Animator.StringToHash("Peck");

    private static readonly int TakeOffHash =
        Animator.StringToHash("TakeOff");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ravenColliders = GetComponentsInChildren<Collider2D>(true);

        // Automatically find the player.
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning(
                $"RavenWander: No GameObject found with tag '{playerTag}'.",
                this);
        }

        // Automatically find the camera.
        gameCamera = Camera.main;

        // Find the Visual child, or locate an Animator in a child.
        Transform foundVisual = transform.Find("Visual");

        if (foundVisual == null)
        {
            Animator childAnimator = GetComponentInChildren<Animator>();

            if (childAnimator != null)
                foundVisual = childAnimator.transform;
        }

        if (foundVisual == null)
        {
            Debug.LogError(
                "RavenWander: Could not find the Visual child or its Animator.",
                this);

            enabled = false;
            return;
        }

        visual = foundVisual;
        animator = visual.GetComponent<Animator>();
        spriteRenderer = visual.GetComponent<SpriteRenderer>();

        ravenSprite = visual.GetComponent<SpriteRenderer>();

        if (ravenSprite != null)
        {
            originalSortingLayer = ravenSprite.sortingLayerName;
        }

        if (animator == null || spriteRenderer == null)
        {
            Debug.LogError(
                "RavenWander: Visual needs an Animator and SpriteRenderer.",
                this);

            enabled = false;
            return;
        }

        if (gameCamera == null)
        {
            Debug.LogWarning(
                "RavenWander: No MainCamera found. Check the camera's MainCamera tag.",
                this);
        }

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;

        visualStartLocalPosition = visual.localPosition;
    }

    private void Start()
    {
        ScheduleNextAction();
    }

    private void Update()
    {
        if (currentState == RavenState.Escaping)
        {
            UpdateEscape();
            return;
        }

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag(playerTag);

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player != null)
        {
            float distance =
                Vector2.Distance(rb.position, player.position);

            if (distance <= detectionRadius)
            {
                BeginEscape();
                return;
            }
        }

        UpdateGroundBehavior();
    }

    private void FixedUpdate()
    {
        if (currentState == RavenState.Escaping)
        {
            // Keep moving until the raven is actually off-screen.
            // Do not use a fixed destination that the camera can follow.
            rb.MovePosition(
                rb.position +
                escapeDirection * flightSpeed * Time.fixedDeltaTime);

            return;
        }

        if (isHopping)
        {
            hopTimer += Time.fixedDeltaTime;

            float progress = Mathf.Clamp01(
                hopTimer / Mathf.Max(hopMoveDuration, 0.01f));

            rb.MovePosition(Vector2.Lerp(
                hopStartPosition,
                hopTargetPosition,
                progress));

            if (progress >= 1f)
                isHopping = false;
        }
    }

    private void UpdateGroundBehavior()
    {
        actionTimer -= Time.deltaTime;

        if (actionTimer > 0f)
            return;

        if (Random.value < peckChance)
        {
            animator.ResetTrigger(PeckHash);
            animator.SetTrigger(PeckHash);

            actionTimer = Random.Range(
                minActionDuration,
                maxActionDuration);
        }
        else
        {
            BeginHop();
        }
    }

    private void BeginHop()
    {
        Vector2[] directions =
        {
            Vector2.left,
            Vector2.right,
            new Vector2(-1f, 1f).normalized,
            new Vector2(1f, 1f).normalized,
            new Vector2(-1f, -1f).normalized,
            new Vector2(1f, -1f).normalized,
            Vector2.up,
            Vector2.down
        };

        hopDirection = directions[Random.Range(0, directions.Length)];

        FaceDirection(hopDirection.x);

        hopStartPosition = rb.position;
        hopTargetPosition = hopStartPosition + hopDirection * hopDistance;

        hopTimer = 0f;
        isHopping = true;

        animator.ResetTrigger(HopHash);
        animator.SetTrigger(HopHash);

        ScheduleNextAction();
    }

    private void ScheduleNextAction()
    {
        actionTimer = Random.Range(minActionDelay, maxActionDelay);
    }

    private void BeginEscape()
    {
        currentState = RavenState.Escaping;
        isHopping = false;

        // Disable collisions so the raven can fly away freely.
        foreach (Collider2D col in ravenColliders)
        {
            col.enabled = false;
        }

        if (ravenSprite != null)
        {
            ravenSprite.sortingLayerName = flyingSortingLayer;
        }

        rb.linearVelocity = Vector2.zero;

        // Start by fleeing directly away from the player.
        Vector2 awayFromPlayer = player != null
            ? rb.position - (Vector2)player.position
            : Vector2.up;

        if (awayFromPlayer.sqrMagnitude > 0.001f)
            escapeDirection = awayFromPlayer.normalized;
        else
            escapeDirection = Vector2.up;

        // Always give the raven an upward world-space component.
        // This prevents a purely horizontal escape from matching
        // the camera's horizontal tracking.
        escapeDirection.y = Mathf.Max(
            escapeDirection.y,
            upwardEscapeBias);

        escapeDirection.Normalize();

        FaceDirection(escapeDirection.x);

        escapeStartTime = Time.time;

        animator.ResetTrigger(TakeOffHash);
        animator.SetTrigger(TakeOffHash);
    }

    private void UpdateEscape()
    {
        float elapsed = Time.time - escapeStartTime;

        // Lift the visual for the takeoff effect.
        float progress = 1f - Mathf.Exp(-elapsed * 4f);

        Vector3 localPosition = visualStartLocalPosition;
        localPosition.y += takeoffHeight * progress;
        visual.localPosition = localPosition;

        // Failsafe in case the camera keeps the raven in view.
        if (elapsed >= maxEscapeDuration)
        {
            gameObject.SetActive(false);
            return;
        }

        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gameCamera == null)
            return;

        // Check the current camera view every frame.
        Vector3 viewportPosition =
            gameCamera.WorldToViewportPoint(visual.position);

        bool outsideScreen =
            viewportPosition.x < -screenMargin ||
            viewportPosition.x > 1f + screenMargin ||
            viewportPosition.y < -screenMargin ||
            viewportPosition.y > 1f + screenMargin ||
            viewportPosition.z < 0f;

        if (outsideScreen)
            gameObject.SetActive(false);
    }

    private void FaceDirection(float horizontalDirection)
    {
        if (horizontalDirection > 0.01f)
            spriteRenderer.flipX = !spriteFacesRightByDefault;
        else if (horizontalDirection < -0.01f)
            spriteRenderer.flipX = spriteFacesRightByDefault;
    }

    private void OnDisable()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
