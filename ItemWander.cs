using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ItemWander : MonoBehaviour
{
    [Header("Wander Movement Settings")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float minWalkTime = 0.8f;
    [SerializeField] private float maxWalkTime = 2.5f;
    [SerializeField] private float minIdleTime = 1.5f;
    [SerializeField] private float maxIdleTime = 4.0f;

    [Header("Kick & Impact Settings")]
    [Tooltip("Fixed velocity magnitude applied when Starr kicks the animal.")]
    [SerializeField] private float kickSpeed = 6f; 
    [Tooltip("Maximum velocity the animal can reach under any circumstance.")]
    [SerializeField] private float maxKickVelocity = 8f; 
    [Tooltip("How long wander velocity is paused after being kicked so physics momentum plays out.")]
    [SerializeField] private float kickPauseDuration = 0.8f;
    [Tooltip("Multiplier applied to walk speed immediately after recovering from a kick.")]
    [SerializeField] private float fleeSpeedMultiplier = 1.8f;
    [SerializeField] private float fleeDuration = 2.0f;

    [Header("Juice & Visual Polish")]
    [Tooltip("If true, flips the SpriteRenderer along X based on movement direction.")]
    [SerializeField] private bool flipSpriteOnX = true;
    [Tooltip("Chance (0-100%) that the animal turns around during idle state.")]
    [Range(0, 100)]
    [SerializeField] private int idleLookAroundChance = 40;
    [Tooltip("Percentage chance (0 to 100) that the animal pecks/inspects the ground when going idle.")]
    [Range(0, 100)]
    [SerializeField] private int peckChanceWhenIdle = 60;
    [SerializeField] private string peckAnimTrigger = "Peck";

    [Header("Obstacle Avoidance")]
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Juice Prefabs & Audio (Optional)")]
    [SerializeField] private ParticleSystem kickDustParticles;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip kickSound;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D itemCollider;

    private Vector2 direction;
    private float timer;
    private bool walking;
    private float knockbackTimer;
    private float fleeTimer;

    private Vector3 originalScale;
    private Coroutine squashCoroutine;

    private readonly Vector2[] possibleDirections = new Vector2[]
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(1f, 1f).normalized,
        new Vector2(-1f, 1f).normalized,
        new Vector2(1f, -1f).normalized,
        new Vector2(-1f, -1f).normalized
    };

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        itemCollider = GetComponent<Collider2D>();

        originalScale = transform.localScale;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 1.0f;
            rb.linearDamping = 3.0f;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void Start()
    {
        ChooseIdle();
    }

    private void Update()
    {
        if (knockbackTimer > 0)
        {
            knockbackTimer -= Time.deltaTime;
        }

        if (fleeTimer > 0)
        {
            fleeTimer -= Time.deltaTime;
        }

        timer -= Time.deltaTime;

        if (timer <= 0 && knockbackTimer <= 0)
        {
            if (walking) ChooseIdle();
            else ChooseDirection();
        }

        UpdateAnimatorAndFacing();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Clamp maximum velocity
        if (rb.linearVelocity.magnitude > maxKickVelocity)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxKickVelocity;
        }

        if (knockbackTimer > 0) return;

        if (walking)
        {
            if (IsHeadingIntoWall(direction))
            {
                ChooseDirection();
                return;
            }

            float currentSpeed = moveSpeed * (fleeTimer > 0 ? fleeSpeedMultiplier : 1f);
            rb.linearVelocity = direction * currentSpeed;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (knockbackTimer > 0) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Vector2 kickDirection = (transform.position - collision.transform.position).normalized;
            ApplyKickVelocity(kickDirection * kickSpeed);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) return;

        if (walking && knockbackTimer <= 0)
        {
            ChooseDirection();
        }
    }

    public void ApplyKickVelocity(Vector2 kickVelocity)
    {
        ChooseIdle();
        knockbackTimer = kickPauseDuration;
        fleeTimer = fleeDuration;

        if (rb != null)
        {
            rb.linearVelocity = kickVelocity;
        }

        // Particle & Audio FX
        if (kickDustParticles != null)
        {
            kickDustParticles.Play();
        }

        if (audioSource != null && kickSound != null)
        {
            audioSource.PlayOneShot(kickSound);
        }

        // Trigger visual impact squash
        if (squashCoroutine != null) StopCoroutine(squashCoroutine);
        squashCoroutine = StartCoroutine(ImpactSquashAndStretch());
    }

    private IEnumerator ImpactSquashAndStretch()
    {
        Vector3 compressedScale = new Vector3(originalScale.x * 1.25f, originalScale.y * 0.75f, originalScale.z);
        Vector3 stretchedScale = new Vector3(originalScale.x * 0.85f, originalScale.y * 1.2f, originalScale.z);

        float elapsed = 0f;
        float duration = 0.1f;

        // Compress
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, compressedScale, elapsed / duration);
            yield return null;
        }

        // Stretch
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(compressedScale, stretchedScale, elapsed / duration);
            yield return null;
        }

        // Return to original
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(stretchedScale, originalScale, elapsed / duration);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private bool IsHeadingIntoWall(Vector2 dir)
    {
        if (dir == Vector2.zero) return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(obstacleLayers);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        RaycastHit2D[] hits = new RaycastHit2D[4];

        // Three-whisker check (Center, 25 deg left, 25 deg right) to smoothly avoid corners
        Vector2 leftWhisker = Quaternion.Euler(0, 0, 25f) * dir;
        Vector2 rightWhisker = Quaternion.Euler(0, 0, -25f) * dir;

        return CheckWhisker(dir, filter, hits) || CheckWhisker(leftWhisker, filter, hits) || CheckWhisker(rightWhisker, filter, hits);
    }

    private bool CheckWhisker(Vector2 rayDir, ContactFilter2D filter, RaycastHit2D[] hits)
    {
        int count = Physics2D.Raycast(transform.position, rayDir, filter, hits, wallCheckDistance);
        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider != null && hits[i].collider != itemCollider)
            {
                return true;
            }
        }
        return false;
    }

    private void ChooseDirection()
    {
        List<Vector2> shuffledDirs = new List<Vector2>(possibleDirections);
        ShuffleList(shuffledDirs);

        foreach (Vector2 candidateDir in shuffledDirs)
        {
            if (!IsHeadingIntoWall(candidateDir))
            {
                direction = candidateDir;
                walking = true;
                timer = Random.Range(minWalkTime, maxWalkTime);
                return;
            }
        }

        ChooseIdle();
    }

    private void ChooseIdle()
    {
        direction = Vector2.zero;
        walking = false;

        if (rb != null && knockbackTimer <= 0)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Ambient idle turn around
        if (flipSpriteOnX && spriteRenderer != null && Random.Range(0, 100) < idleLookAroundChance)
        {
            spriteRenderer.flipX = !spriteRenderer.flipX;
        }

        // Trigger peck / ground inspect animation
        if (animator != null && !string.IsNullOrEmpty(peckAnimTrigger))
        {
            if (Random.Range(0, 100) < peckChanceWhenIdle)
            {
                animator.SetTrigger(peckAnimTrigger);
            }
        }

        timer = Random.Range(minIdleTime, maxIdleTime);
    }

    private void UpdateAnimatorAndFacing()
    {
        if (direction != Vector2.zero)
        {
            if (flipSpriteOnX && spriteRenderer != null)
            {
                if (direction.x > 0.01f) spriteRenderer.flipX = false;
                else if (direction.x < -0.01f) spriteRenderer.flipX = true;
            }

            if (animator != null)
            {
                animator.SetFloat("MoveX", direction.x);
                animator.SetFloat("MoveY", direction.y);
            }
        }

        if (animator != null)
        {
            animator.SetBool("IsWalking", walking);
            animator.SetFloat("Speed", walking ? (fleeTimer > 0 ? fleeSpeedMultiplier : 1f) : 0f);
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wallCheckDistance);
    }
}