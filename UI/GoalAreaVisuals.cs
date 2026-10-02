using System.Collections;
using UnityEngine;

public enum GoalState
{
    Inactive,
    Active,         // Pulsing base color (Cyan)
    Scored,         // Quick flash (Green)
    Lost,           // Quick flash (Red)
    MissionComplete // Gold splash that smoothly fades out
}

public class GoalAreaVisuals : MonoBehaviour
{
    [Header("State Colors")]
    public Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0f);
    public Color activeColor = new Color(0f, 0.8f, 1f, 0.8f);       // Cyan
    public Color scoredColor = new Color(0.2f, 1f, 0.2f, 1f);      // Green
    public Color lostColor = new Color(1f, 0.1f, 0.1f, 1f);        // Red
    public Color completeColor = new Color(1f, 0.8f, 0f, 1f);      // Gold

    [Header("Pulse Settings")]
    public float pulseSpeed = 4.0f;
    public float minAlphaMultiplier = 0.4f;
    public float maxAlphaMultiplier = 1.0f;

    [Header("Mission Complete Fade Settings")]
    [Tooltip("How long (in seconds) the gold color stays solid before starting to fade.")]
    [SerializeField] public float completeHoldDuration = 0.6f;
    [Tooltip("How long (in seconds) it takes to fade from solid gold to completely invisible.")]
    [SerializeField] public float completeFadeDuration = 1.5f;

    private SpriteRenderer spriteRenderer;
    private Renderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private GoalJuice goalJuice;

    private GoalState currentState = GoalState.Inactive;
    private Coroutine activeFeedbackCoroutine;
    private bool isFlashLocked = false;

    private static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorProperty = Shader.PropertyToID("_Color");

    public GoalState CurrentState => currentState;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        meshRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        goalJuice = GetComponent<GoalJuice>();

        AutoFitToParentCollider();
    }

    private void Update()
    {
        // Only run pulse if active AND not currently performing a priority flash
        if (currentState == GoalState.Active && !isFlashLocked)
        {
            float lerpVal = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            float currentAlpha = Mathf.Lerp(minAlphaMultiplier, maxAlphaMultiplier, lerpVal);

            Color pulsedColor = activeColor;
            pulsedColor.a *= currentAlpha;

            ApplyColor(pulsedColor);
        }
    }

    public void SetState(GoalState newState)
    {
        // IGNORE duplicate MissionComplete triggers if we've already done or are doing the fade
        if (newState == GoalState.MissionComplete && currentState == GoalState.MissionComplete)
        {
            return;
        }

        // 1. HARD GUARD: If a green/red flash is running, ignore requests to set state to Active or Inactive
        if (isFlashLocked)
        {
            if (newState == GoalState.Active || newState == GoalState.Inactive)
            {
                return;
            }
        }

        // 2. Mission Complete overrides everything immediately
        if (newState == GoalState.MissionComplete)
        {
            if (activeFeedbackCoroutine != null) StopCoroutine(activeFeedbackCoroutine);
            isFlashLocked = false;
            currentState = GoalState.MissionComplete;
            activeFeedbackCoroutine = StartCoroutine(MissionCompleteFadeEffect());
            return;
        }

        // 3. Stop running coroutines for standard transitions
        if (activeFeedbackCoroutine != null)
        {
            StopCoroutine(activeFeedbackCoroutine);
            activeFeedbackCoroutine = null;
        }

        currentState = newState;

        if (!gameObject.activeInHierarchy || !enabled)
        {
            ApplyStateDirect(newState);
            return;
        }

        // Trigger Juice effects
        if (goalJuice != null)
        {
            if (currentState == GoalState.Scored)
            {
                goalJuice.TriggerGoalEffects(transform.position, isPositive: true);
            }
            else if (currentState == GoalState.Lost)
            {
                goalJuice.TriggerGoalEffects(transform.position, isPositive: false);
            }
        }

        switch (currentState)
        {
            case GoalState.Inactive:
                ApplyColor(inactiveColor);
                break;

            case GoalState.Active:
                // Handled in Update()
                break;

            case GoalState.Scored:
                activeFeedbackCoroutine = StartCoroutine(FlashEffect(scoredColor, 0.6f, GoalState.Active));
                break;

            case GoalState.Lost:
                activeFeedbackCoroutine = StartCoroutine(FlashEffect(lostColor, 0.6f, GoalState.Active));
                break;
        }
    }

    private void ApplyStateDirect(GoalState state)
    {
        switch (state)
        {
            case GoalState.Inactive:
                ApplyColor(inactiveColor);
                break;
            case GoalState.Active:
                ApplyColor(activeColor);
                break;
            case GoalState.Scored:
                ApplyColor(scoredColor);
                break;
            case GoalState.Lost:
                ApplyColor(lostColor);
                break;
            case GoalState.MissionComplete:
                ApplyColor(completeColor);
                break;
        }
    }

    private IEnumerator FlashEffect(Color targetFlashColor, float duration, GoalState returnState)
    {
        isFlashLocked = true;
        float elapsed = 0f;
        float flashSpeed = 25f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float blink = (Mathf.Sin(elapsed * flashSpeed) + 1f) / 2f;
            ApplyColor(Color.Lerp(activeColor, targetFlashColor, blink));
            yield return null;
        }

        isFlashLocked = false;
        SetState(returnState);
    }

    private IEnumerator MissionCompleteFadeEffect()
    {
        isFlashLocked = true;
        ApplyColor(completeColor);
        
        if (completeHoldDuration > 0f)
        {
            yield return new WaitForSeconds(completeHoldDuration);
        }

        float elapsed = 0f;
        Color startColor = completeColor;
        Color fadeTargetColor = new Color(completeColor.r, completeColor.g, completeColor.b, 0f);

        while (elapsed < completeFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / completeFadeDuration;
            ApplyColor(Color.Lerp(startColor, fadeTargetColor, t));
            yield return null;
        }

        isFlashLocked = false;
        SetState(GoalState.Inactive);
    }

    private void ApplyColor(Color finalColor)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = finalColor;
        }
        else if (meshRenderer != null)
        {
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(ColorProperty, finalColor);
            propBlock.SetColor(LegacyColorProperty, finalColor);
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }

    public void AutoFitToParentCollider()
    {
        Collider2D col = GetComponentInParent<Collider2D>();
        if (col == null || spriteRenderer == null || spriteRenderer.sprite == null) return;

        transform.position = col.bounds.center;
        Vector2 colliderSize = col.bounds.size;
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;

        transform.localScale = new Vector3(
            (colliderSize.x / spriteSize.x) / (parentScale.x != 0 ? parentScale.x : 1f),
            (colliderSize.y / spriteSize.y) / (parentScale.y != 0 ? parentScale.y : 1f),
            1f
        );
    }
}