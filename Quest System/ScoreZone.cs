
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
    [SerializeField] private int pointValue = 1;
    [SerializeField] private SkeeballController controller;
    [SerializeField] private GoalAreaVisuals visualHandler;

    [Header("Item Filtering")]
    [Tooltip("Leave empty to accept any identifiable item. Assign an Item asset to require that specific item.")]
    [SerializeField] private Item requiredItem;

    [Header("Behavior Settings")]
    [SerializeField] private ScoreBehavior scoreBehavior = ScoreBehavior.KeepInPlay;

    [Header("Snappy Rest Settings")]
    [SerializeField] private float slowThreshold = 0.4f;
    [SerializeField] private float requiredSettleTime = 0.3f;

    private readonly HashSet<GameObject> scoredObjects = new HashSet<GameObject>();
    private readonly Dictionary<GameObject, Coroutine> activeChecks =
        new Dictionary<GameObject, Coroutine>();

    private void Awake()
    {
        if (controller == null)
            controller = GetComponentInParent<SkeeballController>();

        if (visualHandler == null)
            visualHandler = GetComponentInChildren<GoalAreaVisuals>();
    }

    public GoalAreaVisuals GetVisualHandler() => visualHandler;

    /// <summary>
    /// Finds the item's data using ItemHolder first, then CollectibleItem.
    /// The collider may be on a child of the actual item object.
    /// </summary>
    private bool TryGetItem(Collider2D other, out GameObject itemObject, out Item itemData)
    {
        itemObject = null;
        itemData = null;

        if (other == null) return false;

        // First, check for ItemHolder on the collider's object or its parents.
        ItemHolder holder = other.GetComponentInParent<ItemHolder>();

        if (holder != null)
        {
            itemObject = holder.gameObject;
            itemData = holder.itemData;
        }
        else
        {
            // Otherwise, check for CollectibleItem.
            CollectibleItem collectible = other.GetComponentInParent<CollectibleItem>();

            if (collectible == null)
                collectible = other.GetComponentInChildren<CollectibleItem>();

            if (collectible != null)
            {
                itemObject = collectible.gameObject;
                itemData = collectible.ItemData;
            }
        }

        // We need a real item object and an assigned Item asset.
        if (itemObject == null || itemData == null)
            return false;

        // If a required item is assigned, reject all other items.
        if (requiredItem != null && itemData != requiredItem)
            return false;

        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (controller != null && controller.IsGameLocked)
            return;

        if (!TryGetItem(other, out GameObject obj, out _))
            return;

        if (scoredObjects.Contains(obj) || activeChecks.ContainsKey(obj))
            return;

        activeChecks[obj] = StartCoroutine(WaitForStoneToSettle(obj));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Identify the object even if it has moved out of the zone.
        // Do not use the required-item filter here: an object's identity
        // may need to be resolved to clean up a previously started check.
        if (!TryGetItemForExit(other, out GameObject obj))
            return;

        if (activeChecks.TryGetValue(obj, out Coroutine check) && check != null)
        {
            StopCoroutine(check);
            activeChecks.Remove(obj);
        }

        if (controller != null && controller.IsGameLocked)
            return;

        if (scoredObjects.Contains(obj))
        {
            scoredObjects.Remove(obj);

            if (scoreBehavior == ScoreBehavior.KeepInPlay)
            {
                if (controller != null)
                    controller.RemovePoints(pointValue, obj);

                if (visualHandler != null)
                    visualHandler.SetState(GoalState.Lost);
            }
        }
    }

    private bool TryGetItemForExit(Collider2D other, out GameObject itemObject)
    {
        itemObject = null;

        if (other == null) return false;

        ItemHolder holder = other.GetComponentInParent<ItemHolder>();

        if (holder != null)
        {
            itemObject = holder.gameObject;
            return true;
        }

        CollectibleItem collectible = other.GetComponentInParent<CollectibleItem>();

        if (collectible == null)
            collectible = other.GetComponentInChildren<CollectibleItem>();

        if (collectible != null)
        {
            itemObject = collectible.gameObject;
            return true;
        }

        return false;
    }

    private IEnumerator WaitForStoneToSettle(GameObject obj)
    {
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();

        if (rb == null)
            rb = obj.GetComponentInChildren<Rigidbody2D>();

        if (rb == null)
        {
            AwardScore(obj);
            activeChecks.Remove(obj);
            yield break;
        }

        float timeLowVelocity = 0f;

        while (obj != null)
        {
            if (!obj.activeInHierarchy ||
                (controller != null && controller.IsGameLocked))
            {
                activeChecks.Remove(obj);
                yield break;
            }

            if (rb == null)
            {
                activeChecks.Remove(obj);
                yield break;
            }

            if (rb.linearVelocity.magnitude < slowThreshold)
            {
                timeLowVelocity += Time.deltaTime;

                if (timeLowVelocity >= requiredSettleTime)
                    break;
            }
            else
            {
                timeLowVelocity = 0f;
            }

            yield return null;
        }

        if (obj != null && (controller == null || !controller.IsGameLocked))
            AwardScore(obj);

        if (obj != null)
            activeChecks.Remove(obj);
    }

    private void AwardScore(GameObject obj)
    {
        if (obj == null || scoredObjects.Contains(obj))
            return;

        scoredObjects.Add(obj);

        if (visualHandler != null)
            visualHandler.SetState(GoalState.Scored);

        StartCoroutine(JuicePop(obj.transform));

        if (controller != null)
            controller.AddPoints(pointValue, obj, scoreBehavior);
    }

    private IEnumerator JuicePop(Transform t)
    {
        if (t == null)
            yield break;

        Vector3 originalScale = t.localScale;
        Vector3 targetScale = originalScale * 1.2f;

        float elapsed = 0f;
        float duration = 0.12f;

        while (elapsed < duration)
        {
            if (t == null)
                yield break;

            t.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < duration)
        {
            if (t == null)
                yield break;

            t.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (t != null)
            t.localScale = originalScale;
    }
}
