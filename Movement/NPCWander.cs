using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NPCFacing))]
public class NPCWander : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float minWalkTime = 1.5f;
    [SerializeField] private float maxWalkTime = 4.0f;
    [SerializeField] private float minIdleTime = 1.0f;
    [SerializeField] private float maxIdleTime = 3.0f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private float wallCheckDistance = 0.7f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Player Yield Reaction")]
    [Tooltip("If true, the NPC will immediately yield and pick a new path when bumped into by Starr while walking.")]
    [SerializeField] private bool yieldToPlayerOnCollision = true;

    private Rigidbody2D rb;
    private NPCFacing npcFacing;
    private Collider2D npcCollider;

    private Vector2 direction;
    private float timer;
    private bool walking;
    private float collisionCooldownTimer;

    private readonly Vector2[] possibleDirections = new Vector2[]
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(1f, 1f).normalized,
        new Vector2(-1f, 1f).normalized,
        new Vector2(1f, -1f).normalized,
        new Vector2(-1f, -1f).normalized
    };

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        npcFacing = GetComponent<NPCFacing>();
        npcCollider = GetComponent<Collider2D>();

        DialogueManager.OnDialogueOpened += StopWandering;
        DialogueManager.OnDialogueClosed += ResumeWandering;

        ChooseIdle();
    }

    private void OnDestroy()
    {
        DialogueManager.OnDialogueOpened -= StopWandering;
        DialogueManager.OnDialogueClosed -= ResumeWandering;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (collisionCooldownTimer > 0) collisionCooldownTimer -= Time.deltaTime;

        if (timer <= 0)
        {
            if (walking) ChooseIdle();
            else ChooseDirection();
        }

        if (npcFacing != null)
        {
            npcFacing.SetMovement(direction);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (walking)
        {
            if (IsHeadingIntoWall(direction))
            {
                ChooseDirection();
                return;
            }

            rb.linearVelocity = direction * moveSpeed;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collisionCooldownTimer > 0) return;

        // TWEAK: Yield to Starr immediately if bumped while walking
        if (walking && yieldToPlayerOnCollision && collision.gameObject.CompareTag("Player"))
        {
            collisionCooldownTimer = 0.5f;
            ChooseDirection();
            return;
        }

        // Standard obstacle wall collision bounce
        if (walking)
        {
            collisionCooldownTimer = 0.2f;
            ChooseDirection();
        }
    }

    private bool IsHeadingIntoWall(Vector2 dir)
    {
        if (dir == Vector2.zero) return false;

        // Perform raycast explicitly filtering using obstacleLayers
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(obstacleLayers);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        RaycastHit2D[] hits = new RaycastHit2D[4];
        int count = Physics2D.Raycast(transform.position, dir, filter, hits, wallCheckDistance);

        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider != null && hits[i].collider != npcCollider)
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

        // If no directions are clear, revert to idle
        ChooseIdle();
    }

    private void ChooseIdle()
    {
        direction = Vector2.zero;
        walking = false;
        
        // TWEAK: Kill velocity once when going idle so standard Rigidbody2D drag takes over
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        timer = Random.Range(minIdleTime, maxIdleTime);
    }

    private void StopWandering()
    {
        ChooseIdle();
        if (npcFacing != null) npcFacing.SetMovement(Vector2.zero);
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        this.enabled = false;
    }

    private void ResumeWandering()
    {
        this.enabled = true;
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        ChooseIdle();
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
}