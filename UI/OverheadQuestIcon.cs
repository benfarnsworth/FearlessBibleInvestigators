using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]

public class OverheadQuestIcon : MonoBehaviour
{
    [Header("NPC Reference")]
    [Tooltip("If left empty, this will automatically search parent objects for a DialogueInteractable at runtime.")]
    [SerializeField] private NPCData npcData;
    
    [Tooltip("Fallback name string if no NPCData asset is used.")]
    [SerializeField] private string npcName;

    [Header("Display Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Animation - Continuous Idle Hover")]
    [SerializeField] private bool enableIdleHover = true;
    [SerializeField] private float floatSpeed = 3.0f;
    [SerializeField] private float floatHeight = 0.12f;
    [SerializeField] private float tiltAngle = 3.5f;

    [Header("Animation - State Change Bounce")]
    [SerializeField] private float popDuration = 0.35f;
    [SerializeField] private AnimationCurve popCurve = new AnimationCurve(
        new Keyframe(0f, 0f),      // Start tiny
        new Keyframe(0.4f, 1.35f), // Overshoot scale (pop)
        new Keyframe(0.75f, 0.9f), // Elastic recoil
        new Keyframe(1f, 1f)       // Settle to default scale
    );

    private Vector3 startLocalPos;
    private Vector3 baseScale;
    private QuestIconState currentState = QuestIconState.None;
    private Coroutine activePopCoroutine;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        startLocalPos = transform.localPosition;
        baseScale = transform.localScale;

        AutoDetectNPC();
    }

    private void OnEnable()
    {
        QuestManager.OnQuestsUpdated += RefreshIcon;
        RefreshIcon();
    }

    private void OnDisable()
    {
        QuestManager.OnQuestsUpdated -= RefreshIcon;
    }

    private void Update()
    {
        if (enableIdleHover && spriteRenderer != null && spriteRenderer.enabled)
        {
            AnimateIdleHover();
        }
    }

    /// <summary>
    /// Handles continuous hovering and subtle rotation tilt in world space.
    /// </summary>
    private void AnimateIdleHover()
    {
        // Hover vertically on a smooth sine wave
        float newY = startLocalPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.localPosition = new Vector3(startLocalPos.x, newY, startLocalPos.z);

        // Slight side-to-side rotation tilt
        float tilt = Mathf.Sin(Time.time * (floatSpeed * 0.75f)) * tiltAngle;
        transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
    }

    /// <summary>
    /// Sets the icon state directly and triggers a bounce pop if the state changed.
    /// </summary>
    public void SetState(QuestIconState newState)
    {
        Sprite iconSprite = (QuestManager.Instance != null) 
            ? QuestManager.Instance.GetQuestIcon(newState) 
            : null;

        bool stateChanged = (newState != currentState);
        currentState = newState;

        SetSprite(iconSprite);

        // Trigger pop bounce whenever state updates to an active icon
        if (iconSprite != null && stateChanged)
        {
            TriggerPopAnimation();
        }
    }

    public void RefreshIcon()
    {
        if (QuestManager.Instance == null) return;

        string targetName = npcData != null ? npcData.npcName : npcName;

        if (string.IsNullOrEmpty(targetName))
        {
            SetSprite(null);
            return;
        }

        QuestIconState newState = QuestManager.Instance.GetIconStateForNPC(targetName);
        SetState(newState);
    }

    private void TriggerPopAnimation()
    {
        if (activePopCoroutine != null)
        {
            StopCoroutine(activePopCoroutine);
        }
        activePopCoroutine = StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsed / popDuration);
            
            float scaleMultiplier = popCurve.Evaluate(percent);
            transform.localScale = baseScale * scaleMultiplier;

            yield return null;
        }

        transform.localScale = baseScale;
        activePopCoroutine = null;
    }

    private void AutoDetectNPC()
    {
        if (npcData != null || !string.IsNullOrEmpty(npcName)) return;

        var dialogueComp = GetComponentInParent<DialogueInteractable>();
        if (dialogueComp != null && dialogueComp.npcData != null)
        {
            npcData = dialogueComp.npcData;
        }
    }

    private void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = (sprite != null);

            if (sprite == null)
            {
                transform.localScale = baseScale;
                transform.localPosition = startLocalPos;
                transform.localRotation = Quaternion.identity;
            }
        }
    }
}