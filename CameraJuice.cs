using System.Collections;
using UnityEngine;

public class CameraJuice : MonoBehaviour
{
    public static CameraJuice Instance { get; private set; }

    [Header("Idle Sway Settings")]
    [SerializeField] private bool enableIdleSway = true;
    [SerializeField] private float swayFrequency = 1.2f;
    [SerializeField] private float swayAmount = 0.08f;

    private Vector3 shakeOffset;
    private float anticipationZoomOffset;

    private Coroutine shakeRoutine;
    private Coroutine hitStopRoutine;
    private Coroutine anticipationRoutine;

    public bool IsSwayEnabled { get => enableIdleSway; set => enableIdleSway = value; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Returns the combined position offset from Shake + Idle Sway.
    /// Used by DialogueCamera in LateUpdate.
    /// </summary>
    public Vector3 GetPositionOffset()
    {
        Vector3 swayOffset = Vector3.zero;

        if (enableIdleSway)
        {
            float swayX = Mathf.Sin(Time.time * swayFrequency) * swayAmount;
            float swayY = Mathf.Cos(Time.time * swayFrequency * 0.8f) * swayAmount;
            swayOffset = new Vector3(swayX, swayY, 0f);
        }

        return shakeOffset + swayOffset;
    }

    /// <summary>
    /// Returns temporary zoom modifications (like anticipation bumps).
    /// </summary>
    public float GetZoomOffset()
    {
        return anticipationZoomOffset;
    }

    // --- JUICE TRIGGERS ---

    public void TriggerHitStop(float durationSeconds)
    {
        if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
        hitStopRoutine = StartCoroutine(DoHitStop(durationSeconds));
    }

    public void TriggerScreenShake(float intensity, float duration)
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(DoShake(intensity, duration));
    }

    public void TriggerImpactJuice(float shakeIntensity, float shakeDuration, float hitStopDuration)
    {
        TriggerScreenShake(shakeIntensity, shakeDuration);
        TriggerHitStop(hitStopDuration);
    }

    public void TriggerAnticipationBump(float bumpAmount, float duration)
    {
        if (anticipationRoutine != null) StopCoroutine(anticipationRoutine);
        anticipationRoutine = StartCoroutine(DoAnticipationBump(bumpAmount, duration));
    }

    // --- COROUTINES ---

    private IEnumerator DoHitStop(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    private IEnumerator DoShake(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            shakeOffset = new Vector3(x, y, 0f);

            // Use unscaledDeltaTime so shake works even during Hit Stop!
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
    }

    private IEnumerator DoAnticipationBump(float bumpAmount, float duration)
    {
        anticipationZoomOffset = bumpAmount;
        yield return new WaitForSeconds(duration);
        anticipationZoomOffset = 0f;
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
}