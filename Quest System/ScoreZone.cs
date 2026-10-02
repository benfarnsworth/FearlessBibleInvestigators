using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ScoreZone : MonoBehaviour
{
    public enum ScoreBehavior
    {
        DestroyObject,
        DisableColliderOnly,
        KeepInPlay
    }

    [Header("Score Setup")]
    [SerializeField] private int pointValue = 10;
    [SerializeField] private SkeeballController controller;
    [SerializeField] private GoalAreaVisuals visualHandler;

    [Header("Behavior Settings")]
    [SerializeField] private ScoreBehavior scoreBehavior = ScoreBehavior.KeepInPlay;

    [Header("Snappy Rest Settings")]
    [Tooltip("Velocity threshold below which the stone is considered slowing down.")]
    [SerializeField] private float slowThreshold = 0.4f;
    [Tooltip("How long (in seconds) the stone needs to stay slow before locking in points.")]
    [SerializeField] private float requiredSettleTime = 0.3f;

    private HashSet<GameObject> scoredObjects = new HashSet<GameObject>();
    private Dictionary<GameObject, Coroutine> activeChecks = new Dictionary<GameObject, Coroutine>();

    private void Awake()
    {
        if (controller == null) controller = GetComponentInParent<SkeeballController>();
        if (visualHandler == null) visualHandler = GetComponentInChildren<GoalAreaVisuals>();
    }

    public GoalAreaVisuals GetVisualHandler() => visualHandler;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (controller != null && controller.IsGameLocked) return;

        ItemHolder holder = other.GetComponentInParent<ItemHolder>();
        if (holder == null) return;

        GameObject obj = holder.gameObject;

        if (scoredObjects.Contains(obj) || activeChecks.ContainsKey(obj)) return;

        activeChecks[obj] = StartCoroutine(WaitForStoneToSettle(obj));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        ItemHolder holder = other.GetComponentInParent<ItemHolder>();
        if (holder == null) return;

        GameObject obj = holder.gameObject;

        if (activeChecks.TryGetValue(obj, out Coroutine check) && check != null)
        {
            StopCoroutine(check);
            activeChecks.Remove(obj);
        }

        if (controller != null && controller.IsGameLocked) return;

        if (scoredObjects.Contains(obj))
        {
            scoredObjects.Remove(obj);

            if (scoreBehavior == ScoreBehavior.KeepInPlay)
            {
                if (controller != null)
                {
                    controller.RemovePoints(pointValue, obj);
                }

                if (visualHandler != null) visualHandler.SetState(GoalState.Lost);
            }
        }
    }

    private IEnumerator WaitForStoneToSettle(GameObject obj)
    {
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb == null) rb = obj.GetComponentInChildren<Rigidbody2D>();

        if (rb == null)
        {
            AwardScore(obj);
            activeChecks.Remove(obj);
            yield break;
        }

        float timeLowVelocity = 0f;

        while (obj != null)
        {
            if (!obj.activeInHierarchy || (controller != null && controller.IsGameLocked))
            {
                activeChecks.Remove(obj);
                yield break;
            }

            if (rb.linearVelocity.magnitude < slowThreshold)
            {
                timeLowVelocity += Time.deltaTime;
                if (timeLowVelocity >= requiredSettleTime)
                {
                    break;
                }
            }
            else
            {
                timeLowVelocity = 0f;
            }

            yield return null;
        }

        if (obj != null && (controller == null || !controller.IsGameLocked))
        {
            AwardScore(obj);
        }

        if (obj != null && activeChecks.ContainsKey(obj))
        {
            activeChecks.Remove(obj);
        }
    }

    private void AwardScore(GameObject obj)
    {
        if (scoredObjects.Contains(obj)) return;
        scoredObjects.Add(obj);

        if (visualHandler != null) visualHandler.SetState(GoalState.Scored);

        StartCoroutine(JuicePop(obj.transform));

        if (controller != null)
        {
            controller.AddPoints(pointValue, obj, scoreBehavior);
        }
    }

    private IEnumerator JuicePop(Transform t)
    {
        if (t == null) yield break;
        Vector3 originalScale = t.localScale;
        Vector3 targetScale = originalScale * 1.2f;

        float elapsed = 0f;
        float duration = 0.12f;

        while (elapsed < duration)
        {
            if (t == null) yield break;
            t.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            if (t == null) yield break;
            t.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (t != null) t.localScale = originalScale;
    }
}