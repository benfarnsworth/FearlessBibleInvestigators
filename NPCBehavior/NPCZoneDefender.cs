using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCZoneDefender : MonoBehaviour
{
    private enum State { Searching, MovingToLineUp, LiningUp, Kicking, ReturningHome, Stunned }

    [Header("NPC Movement & Kick")]
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float kickForce = 18f;
    [SerializeField] private float lineUpOffset = 0.8f; 
    [SerializeField] private float kickRange = 0.4f;
    [SerializeField] private float recheckInterval = 0.3f;

    [Header("Stuck Detection & Smart Pathing")]
    [Tooltip("If Bob moves less than this distance in a check frame, he might be stuck.")]
    [SerializeField] private float stuckDistanceThreshold = 0.05f;
    [Tooltip("How many seconds Bob can push against a wall before giving up on the target.")]
    [SerializeField] private float maxStuckDuration = 0.8f;
    [Tooltip("How long (in seconds) Bob ignores an item that caused him to get stuck.")]
    [SerializeField] private float itemBlacklistTime = 4.0f;

    [Header("Breathing Room & Stun")]
    [Tooltip("Pause duration after kicking before searching for a new target.")]
    [SerializeField] private float postKickCooldown = 1.0f;

    [Tooltip("How long the NPC stays stunned when hit by a player kick.")]
    [SerializeField] private float stunDuration = 1.5f;

    [Header("Defense Strategy")]
    [Tooltip("If true, automatically finds all ScoreZones in scene on Start.")]
    [SerializeField] private bool autoFindZones = true;
    [SerializeField] private List<ScoreZone> targetZones = new List<ScoreZone>();
    [SerializeField] private Transform homePosition;

    [Header("Juice & Visual Polish")]
    [SerializeField] private ParticleSystem kickParticles;
    [SerializeField] private KickAimLine aimLine;
    [Tooltip("Reference to Bob's state indicator component (auto-retrieved if empty).")]
    [SerializeField] private NPCStateIndicator stateIndicator;
    [SerializeField] private float windUpDuration = 0.35f;
    
    [Header("Camera Shake Tweaks")]
    [SerializeField] private bool enableScreenShake = true;
    [SerializeField] private float shakeMagnitude = 0.12f;
    [SerializeField] private float shakeDuration = 0.15f;
    [Tooltip("Maximum distance from player for Bob's kick to shake the camera.")]
    [SerializeField] private float maxShakeDistance = 12f;

    private Rigidbody2D rb;
    private State currentState = State.Searching;
    private Transform currentTargetItem;
    private ScoreZone currentTargetZone;
    private Vector2 homePos;
    private Coroutine logicRoutine;
    private Transform playerTransform;

    // Stuck Tracking
    private Vector2 lastPosition;
    private float stuckTimer;
    private Dictionary<Transform, float> blacklistedItems = new Dictionary<Transform, float>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Auto-fetch state indicator from child objects if not assigned in Inspector
        if (stateIndicator == null)
        {
            stateIndicator = GetComponentInChildren<NPCStateIndicator>();
        }
    }

    private void Start()
    {
        homePos = homePosition != null ? (Vector2)homePosition.position : (Vector2)transform.position;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        if (autoFindZones)
        {
            targetZones.AddRange(FindObjectsByType<ScoreZone>(FindObjectsSortMode.None));
        }

        logicRoutine = StartCoroutine(DefenseLogicRoutine());
    }

    private IEnumerator DefenseLogicRoutine()
    {
        while (true)
        {
            switch (currentState)
            {
                case State.Searching:
                    HideAimLine();
                    rb.linearVelocity = Vector2.zero;
                    ResetStuckTimer();
                    FindItemToClear();
                    yield return new WaitForSeconds(recheckInterval);
                    break;

                case State.MovingToLineUp:
                    if (IsTargetInvalid())
                    {
                        ResetToSearching();
                        yield return null;
                        break;
                    }

                    Vector2 kickDir = GetKickOutwardDirection(currentTargetItem.position, currentTargetZone.transform.position);
                    Vector2 lineUpPos = (Vector2)currentTargetItem.position - (kickDir * lineUpOffset);

                    MoveTowards(lineUpPos);

                    if (CheckIfStuck())
                    {
                        // Pop alert icon briefly when stuck before giving up on item
                        if (stateIndicator != null)
                        {
                            stateIndicator.ShowAlert(0.8f);
                        }

                        BlacklistCurrentTarget();
                        ResetToSearching();
                        yield return null;
                        break;
                    }

                    if (Vector2.Distance(transform.position, lineUpPos) <= kickRange)
                    {
                        currentState = State.LiningUp;
                        ResetStuckTimer();
                    }
                    yield return new WaitForFixedUpdate();
                    break;

                case State.LiningUp:
                    rb.linearVelocity = Vector2.zero;

                    // Smoothly charge aim line over windUpDuration and track target motion live
                    float windUpTimer = 0f;
                    while (windUpTimer < windUpDuration)
                    {
                        if (IsTargetInvalid())
                        {
                            ResetToSearching();
                            break;
                        }

                        windUpTimer += Time.deltaTime;
                        float chargeRatio = Mathf.Clamp01(windUpTimer / windUpDuration);

                        Vector2 liveAimDir = GetKickOutwardDirection(currentTargetItem.position, currentTargetZone.transform.position);
                        if (aimLine != null)
                        {
                            aimLine.SetAim(currentTargetItem.position, liveAimDir, chargeRatio);
                        }

                        yield return null;
                    }

                    if (currentState == State.LiningUp)
                    {
                        HideAimLine();
                        currentState = State.Kicking;
                    }
                    break;

                case State.Kicking:
                    if (!IsTargetInvalid())
                    {
                        ExecuteKick();
                    }

                    rb.linearVelocity = Vector2.zero;
                    currentTargetItem = null;
                    currentTargetZone = null;

                    yield return new WaitForSeconds(postKickCooldown);

                    currentState = State.Searching;
                    break;

                case State.ReturningHome:
                    HideAimLine();
                    MoveTowards(homePos);

                    if (CheckIfStuck())
                    {
                        rb.linearVelocity = Vector2.zero;
                        ResetToSearching();
                        yield return null;
                        break;
                    }

                    if (Vector2.Distance(transform.position, homePos) < 0.3f)
                    {
                        rb.linearVelocity = Vector2.zero;
                        currentState = State.Searching;
                    }
                    yield return new WaitForFixedUpdate();
                    break;

                case State.Stunned:
                    yield return null;
                    break;
            }
        }
    }

    public void TakeStun(Vector2 knockbackDir, float force)
    {
        HideAimLine();

        if (logicRoutine != null)
        {
            StopCoroutine(logicRoutine);
        }

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDir * force, ForceMode2D.Impulse);

        currentState = State.Stunned;

        // Display stun icon above Bob's head for the duration of the stun
        if (stateIndicator != null)
        {
            stateIndicator.ShowStun(stunDuration);
        }

        StartCoroutine(StunRoutine());
    }

    private IEnumerator StunRoutine()
    {
        yield return new WaitForSeconds(stunDuration);

        currentState = State.Searching;
        logicRoutine = StartCoroutine(DefenseLogicRoutine());
    }

    // --- STUCK & BLACKLIST LOGIC ---

    private bool CheckIfStuck()
    {
        float distanceMoved = Vector2.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        if (distanceMoved < stuckDistanceThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= maxStuckDuration)
            {
                return true;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        return false;
    }

    private void ResetStuckTimer()
    {
        stuckTimer = 0f;
        lastPosition = transform.position;
    }

    private void BlacklistCurrentTarget()
    {
        if (currentTargetItem != null)
        {
            blacklistedItems[currentTargetItem] = Time.time + itemBlacklistTime;
        }
    }

    private void CleanExpiredBlacklist()
    {
        if (blacklistedItems.Count == 0) return;

        List<Transform> expired = new List<Transform>();
        foreach (var kvp in blacklistedItems)
        {
            if (Time.time >= kvp.Value)
            {
                expired.Add(kvp.Key);
            }
        }

        foreach (var key in expired)
        {
            blacklistedItems.Remove(key);
        }
    }

    // --- UTILITIES & ACTION EXECUTION ---

    private void HideAimLine()
    {
        if (aimLine != null)
        {
            aimLine.Hide();
        }
    }

    private void ResetToSearching()
    {
        rb.linearVelocity = Vector2.zero;
        HideAimLine();
        currentTargetItem = null;
        currentTargetZone = null;
        ResetStuckTimer();
        currentState = State.Searching;
    }

    private void FindItemToClear()
    {
        CleanExpiredBlacklist();

        float closestDistance = float.MaxValue;
        Transform bestItem = null;
        ScoreZone bestZone = null;

        ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
        List<Collider2D> results = new List<Collider2D>();

        foreach (var zone in targetZones)
        {
            if (zone == null) continue;

            Collider2D zoneCol = zone.GetComponent<Collider2D>();
            if (zoneCol == null) continue;

            results.Clear();
            zoneCol.Overlap(filter, results);

            foreach (var col in results)
            {
                if (col == null) continue;

                ItemHolder holder = col.GetComponentInParent<ItemHolder>();
                if (holder == null) continue;

                if (blacklistedItems.ContainsKey(holder.transform)) continue;

                float dist = Vector2.Distance(transform.position, holder.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    bestItem = holder.transform;
                    bestZone = zone;
                }
            }
        }

        if (bestItem != null)
        {
            currentTargetItem = bestItem;
            currentTargetZone = bestZone;
            currentState = State.MovingToLineUp;
            ResetStuckTimer();

            // Pop alert icon above Bob's head when he spots an item in the zone
            if (stateIndicator != null)
            {
                stateIndicator.ShowAlert(1.2f);
            }
        }
        else if (Vector2.Distance(transform.position, homePos) > 1f)
        {
            currentState = State.ReturningHome;
            ResetStuckTimer();
        }
    }

    private Vector2 GetKickOutwardDirection(Vector2 itemPos, Vector2 zonePos)
    {
        Vector2 dir = (itemPos - zonePos).normalized;
        return dir == Vector2.zero ? Vector2.up : dir;
    }

    private void MoveTowards(Vector2 targetPos)
    {
        Vector2 offset = targetPos - (Vector2)transform.position;
        float dist = offset.magnitude;

        if (dist < 0.05f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float currentSpeed = dist < 0.5f ? Mathf.Lerp(moveSpeed * 0.2f, moveSpeed, dist / 0.5f) : moveSpeed;
        rb.linearVelocity = offset.normalized * currentSpeed;
    }

    private bool IsTargetInvalid()
    {
        if (currentTargetItem == null || currentTargetZone == null) return true;

        Collider2D zoneCol = currentTargetZone.GetComponent<Collider2D>();
        Collider2D itemCol = currentTargetItem.GetComponentInChildren<Collider2D>();

        if (zoneCol == null || itemCol == null) return true;

        return !zoneCol.IsTouching(itemCol);
    }

    private void ExecuteKick()
    {
        if (currentTargetItem == null) return;

        Rigidbody2D itemRb = currentTargetItem.GetComponent<Rigidbody2D>();
        if (itemRb == null) itemRb = currentTargetItem.GetComponentInChildren<Rigidbody2D>();

        if (itemRb != null)
        {
            Vector2 pushDir = GetKickOutwardDirection(currentTargetItem.position, currentTargetZone.transform.position);

            rb.AddForce(pushDir * (kickForce * 0.2f), ForceMode2D.Impulse);
            
            itemRb.linearVelocity = Vector2.zero;
            itemRb.AddForce(pushDir * kickForce, ForceMode2D.Impulse);

            if (currentTargetItem.TryGetComponent<ImpactJuice>(out var impactJuice))
            {
                impactJuice.TriggerJuice(0.5f);
            }

            if (kickParticles != null)
            {
                kickParticles.transform.position = currentTargetItem.position;
                kickParticles.Play();
            }

            if (enableScreenShake && CameraJuice.Instance != null)
            {
                if (playerTransform == null)
                {
                    GameObject p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null) playerTransform = p.transform;
                }

                if (playerTransform != null)
                {
                    float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
                    if (distToPlayer <= maxShakeDistance)
                    {
                        float falloff = 1f - Mathf.Clamp01(distToPlayer / maxShakeDistance);
                        CameraJuice.Instance.TriggerImpactJuice(shakeDuration, shakeMagnitude * falloff, 0.02f);
                    }
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (currentTargetItem != null && currentTargetZone != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTargetItem.position);

            Vector2 kickDir = GetKickOutwardDirection(currentTargetItem.position, currentTargetZone.transform.position);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(currentTargetItem.position, kickDir * 2f);
        }
    }
}