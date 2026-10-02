using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class KickAimLine : MonoBehaviour
{
    [Header("Length & Width Settings")]
    [SerializeField] private float minLength = 1.5f;
    [SerializeField] private float maxLength = 4.0f;
    [SerializeField] private float lineWidth = 0.08f;

    [Header("Raycast Obstacle Detection")]
    [Tooltip("Layers that block the laser line (e.g. Walls, Obstacles, Furniture).")]
    [SerializeField] private LayerMask obstacleLayers;
    [Tooltip("Layers for kickable objects to trigger target highlight response.")]
    [SerializeField] private LayerMask kickableLayers;

    [Header("Fade & Pulse Settings")]
    [SerializeField] private float fadeInTime = 0.08f;
    [SerializeField] private float fadeOutDuration = 0.15f;

    [Header("Color Gradient")]
    [SerializeField] private Color lowChargeColor = new Color(1f, 0.9f, 0.2f, 0.8f);
    [SerializeField] private Color maxChargeColor = new Color(1f, 0.1f, 0.2f, 1.0f);
    [SerializeField] private Color lockedTargetColor = new Color(0.2f, 1f, 0.4f, 1.0f); // Green highlight when aiming at kickable

    private LineRenderer line;
    private Coroutine fadeOutRoutine;
    private float currentChargeTimer = 0f;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        if (line != null)
        {
            line.enabled = false;
            line.positionCount = 2;
        }
    }

    /// <summary>
    /// Updates or enables the aim line originating from a point in a given direction.
    /// </summary>
    /// <param name="origin">World space start position.</param>
    /// <param name="direction">Aim vector direction.</param>
    /// <param name="chargeRatio">0.0 to 1.0 charge percentage.</param>
    public void SetAim(Vector2 origin, Vector2 direction, float chargeRatio = 1f)
    {
        if (line == null) return;

        if (fadeOutRoutine != null)
        {
            StopCoroutine(fadeOutRoutine);
            fadeOutRoutine = null;
        }

        line.enabled = true;
        currentChargeTimer += Time.deltaTime;

        float targetLength = Mathf.Lerp(minLength, maxLength, Mathf.Clamp01(chargeRatio));
        Vector2 normalizedDir = direction.normalized;

        // 1. Raycast Check to Stop Line at Walls
        float actualLength = targetLength;
        bool isTargetingKickable = false;

        if (obstacleLayers.value != 0)
        {
            RaycastHit2D wallHit = Physics2D.Raycast(origin, normalizedDir, targetLength, obstacleLayers);
            if (wallHit.collider != null)
            {
                actualLength = wallHit.distance;
            }
        }

        // 2. Check if aiming directly at a Kickable object
        if (kickableLayers.value != 0)
        {
            RaycastHit2D targetHit = Physics2D.Raycast(origin, normalizedDir, actualLength, kickableLayers);
            if (targetHit.collider != null && targetHit.collider.gameObject != transform.root.gameObject)
            {
                isTargetingKickable = true;
            }
        }

        Vector3 startPos = new Vector3(origin.x, origin.y, -1f);
        Vector3 endPos = startPos + (Vector3)(normalizedDir * actualLength);

        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);

        // 3. Smooth Fade-in & Target Color Reaction
        float fadeInAlpha = Mathf.Clamp01(currentChargeTimer / fadeInTime);
        Color baseTargetColor = isTargetingKickable ? lockedTargetColor : Color.Lerp(lowChargeColor, maxChargeColor, chargeRatio);
        baseTargetColor.a *= fadeInAlpha;

        Color endColor = new Color(baseTargetColor.r, baseTargetColor.g, baseTargetColor.b, 0f);
        line.startColor = baseTargetColor;
        line.endColor = endColor;

        // 4. Max Charge / Target Locked Pulsing
        float widthModifier = 1f;
        if (chargeRatio >= 1f || isTargetingKickable)
        {
            float pulseFrequency = isTargetingKickable ? 35f : 25f;
            widthModifier = 1f + (Mathf.Sin(Time.time * pulseFrequency) * 0.25f);
        }

        line.startWidth = lineWidth * widthModifier;
        line.endWidth = (lineWidth * 0.5f) * widthModifier;
    }

    /// <summary>
    /// Smoothly dissolves the line and disables it.
    /// </summary>
    public void Hide()
    {
        if (line == null || !line.enabled) return;

        currentChargeTimer = 0f;

        if (gameObject.activeInHierarchy)
        {
            if (fadeOutRoutine != null) StopCoroutine(fadeOutRoutine);
            fadeOutRoutine = StartCoroutine(FadeOutRoutine());
        }
        else
        {
            line.enabled = false;
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        float elapsed = 0f;
        Color initialStartColor = line.startColor;
        Color initialEndColor = line.endColor;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;

            Color fadeStart = initialStartColor;
            fadeStart.a = Mathf.Lerp(initialStartColor.a, 0f, t);

            Color fadeEnd = initialEndColor;
            fadeEnd.a = Mathf.Lerp(initialEndColor.a, 0f, t);

            line.startColor = fadeStart;
            line.endColor = fadeEnd;

            yield return null;
        }

        line.enabled = false;
        fadeOutRoutine = null;
    }
}