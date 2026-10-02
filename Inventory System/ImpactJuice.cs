using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ImpactJuice : MonoBehaviour
{
    [Header("Kick & Impact SFX")]
    [SerializeField] private AudioClip kickSound;
    [Tooltip("Minimum hit speed required to play sound and trigger juice FX.")]
    [SerializeField] private float minImpactForce = 1.5f; 
    [Tooltip("Minimum time (in seconds) between impact sounds to prevent audio stacking.")]
    [SerializeField] private float soundCooldown = 0.15f;
    [SerializeField] private float minPitch = 0.85f;
    [SerializeField] private float maxPitch = 1.25f;
    [SerializeField] private bool pitchWithHitForce = true;

    [Header("Audio Proximity & Filtering")]
    [Tooltip("If true, sound will ONLY play if the item is visible on screen.")]
    [SerializeField] private bool onlyPlayWhenVisible = true;

    [Tooltip("If true, sound will ONLY play if within maxSoundDistance of Player/Camera.")]
    [SerializeField] private bool useDistanceFilter = true;
    [SerializeField] private float maxSoundDistance = 10f;

    [Tooltip("If true, item-on-item collisions won't play sound (only hits terrain or player).")]
    [SerializeField] private bool ignoreItemToItemCollisions = false;
    [SerializeField] private string itemTag = "Item";

    [Header("Juice & Visual FX")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("How much the item deforms when kicked (e.g. 0.35 = 35% deformation).")]
    [SerializeField] private float squashAmount = 0.35f;
    [Tooltip("Duration of the squash and stretch spring-back effect in seconds.")]
    [SerializeField] private float juiceDuration = 0.15f;
    [Tooltip("Color to flash on impact.")]
    [SerializeField] private Color hitFlashColor = new Color(2f, 2f, 2f, 1f); 
    [Range(0f, 1f)] [SerializeField] private float flashIntensity = 0.35f; 
    [SerializeField] private float flashDuration = 0.06f;

    private Vector3 originalScale;
    private Color originalColor;
    private Coroutine juiceCoroutine;
    private Coroutine flashCoroutine;
    private float lastSoundTime;
    private Transform listenerTarget;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        CacheListenerTarget();
    }

    private void CacheListenerTarget()
    {
        // Try finding player first, fall back to Main Camera
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            listenerTarget = player.transform;
        }
        else if (Camera.main != null)
        {
            listenerTarget = Camera.main.transform;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce < minImpactForce) return;

        // 1. Check Collision Filtering
        if (ignoreItemToItemCollisions && collision.gameObject.CompareTag(itemTag))
        {
            return;
        }

        // 2. Play Audio (with Cooldown & Proximity/Visibility Checks)
        if (kickSound != null && AudioManager.Instance != null)
        {
            if (Time.time >= lastSoundTime + soundCooldown && CanPlayImpactSound())
            {
                float calculatedPitch = Random.Range(minPitch, maxPitch);

                if (pitchWithHitForce)
                {
                    float forceFactor = Mathf.Clamp01(impactForce / 15f); 
                    calculatedPitch = Mathf.Lerp(minPitch, maxPitch, forceFactor);
                }

                AudioManager.Instance.PlaySFX(kickSound, calculatedPitch, calculatedPitch);
                lastSoundTime = Time.time;
            }
        }

        // 3. Trigger Visual Juice (Always happens if force is met, even if muted by distance)
        TriggerJuice(impactForce);
    }

    private bool CanPlayImpactSound()
    {
        // Filter 1: Screen Visibility
        if (onlyPlayWhenVisible && spriteRenderer != null && !spriteRenderer.isVisible)
        {
            return false;
        }

        // Filter 2: Proximity Check
        if (useDistanceFilter)
        {
            if (listenerTarget == null) CacheListenerTarget();

            if (listenerTarget != null)
            {
                float distSqr = (transform.position - listenerTarget.position).sqrMagnitude;
                if (distSqr > maxSoundDistance * maxSoundDistance)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void TriggerJuice(float impactForce)
    {
        if (spriteRenderer == null) return;

        float intensity = Mathf.Clamp01(impactForce / 15f);

        if (juiceCoroutine != null) StopCoroutine(juiceCoroutine);
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);

        juiceCoroutine = StartCoroutine(SquashAndStretchRoutine(intensity));
        flashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator SquashAndStretchRoutine(float intensity)
    {
        float currentSquash = squashAmount * Mathf.Max(0.4f, intensity);

        Vector3 squashedScale = new Vector3(
            originalScale.x * (1f + currentSquash),
            originalScale.y * (1f - currentSquash),
            originalScale.z
        );

        transform.localScale = squashedScale;
        yield return new WaitForSeconds(0.03f);

        Vector3 stretchedScale = new Vector3(
            originalScale.x * (1f - (currentSquash * 0.5f)),
            originalScale.y * (1f + (currentSquash * 0.5f)),
            originalScale.z
        );

        transform.localScale = stretchedScale;

        float elapsed = 0f;
        while (elapsed < juiceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / juiceDuration;
            transform.localScale = Vector3.Lerp(stretchedScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = Color.Lerp(originalColor, hitFlashColor, flashIntensity);
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }
}