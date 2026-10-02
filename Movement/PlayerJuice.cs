using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerJuice : MonoBehaviour
{
    [Header("Visual Transform")]
    [Tooltip("Assign sprite child object if animated separately, or leave blank to affect player root.")]
    [SerializeField] private Transform spriteTransform;

    [Header("Squash & Stretch Settings")]
    [SerializeField] private float stretchAmount = 0.25f;
    [SerializeField] private float stretchDuration = 0.15f;

    [Header("Kick Recoil Physics")]
    [SerializeField] private float maxRecoilForce = 4f;

    [Header("Particle & SFX Feedback")]
    [Tooltip("Assign a child ParticleSystem for kick dust bursts.")]
    [SerializeField] private ParticleSystem kickDustParticles;
    [SerializeField] private AudioClip kickSwooshSFX;
    [SerializeField] private AudioClip dashSFX;

    private Rigidbody2D rb;
    private Vector3 originalScale;
    private Coroutine currentSquashRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteTransform == null)
        {
            spriteTransform = transform;
        }

        originalScale = spriteTransform.localScale;
    }

    private void OnDisable()
    {
        ResetJuice();
    }

    /// <summary>
    /// Call every frame while charging a kick.
    /// </summary>
    public void ApplyChargeWindup(float chargeRatio)
    {
        if (spriteTransform == null) return;

        // 1. Wind-up Squish: Player compresses downward as they charge
        float squishY = Mathf.Lerp(1f, 0.82f, chargeRatio);
        float bulgeX = Mathf.Lerp(1f, 1.15f, chargeRatio);

        Vector3 targetScale = new Vector3(originalScale.x * bulgeX, originalScale.y * squishY, originalScale.z);

        // 2. Max Charge Tremble/Vibration
        if (chargeRatio >= 1f)
        {
            float jitterX = Random.Range(-0.03f, 0.03f);
            float jitterY = Random.Range(-0.03f, 0.03f);
            spriteTransform.localScale = targetScale + new Vector3(jitterX, jitterY, 0f);
        }
        else
        {
            spriteTransform.localScale = targetScale;
        }
    }

    /// <summary>
    /// Call when kick key is released.
    /// </summary>
    public void TriggerKickRelease(Vector2 facingDir, float chargeRatio)
    {
        // 1. Audio
        if (kickSwooshSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(kickSwooshSFX, 0.9f + (chargeRatio * 0.3f), 1.0f + (chargeRatio * 0.2f));
        }

        // 2. Recoil Physics
        if (rb != null && chargeRatio > 0.3f)
        {
            Vector2 recoilDir = -facingDir.normalized;
            rb.AddForce(recoilDir * (maxRecoilForce * chargeRatio), ForceMode2D.Impulse);
        }

        // 3. Pooled Particle Burst
        if (kickDustParticles != null && chargeRatio > 0.3f)
        {
            int particleCount = Mathf.RoundToInt(Mathf.Lerp(3, 10, chargeRatio));
            kickDustParticles.Emit(particleCount);
        }

        // 4. Squash & Stretch Snap
        StartSquashRoutine(DoSquashAndStretch(facingDir, chargeRatio));
    }

    /// <summary>
    /// Call when starting a dash/dodge.
    /// </summary>
    public void TriggerDashJuice(Vector2 dashDir)
    {
        if (dashSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(dashSFX, 1.0f, 1.2f);
        }

        if (kickDustParticles != null)
        {
            kickDustParticles.Emit(6);
        }

        // Instant snappy stretch along dash direction
        StartSquashRoutine(DoSquashAndStretch(dashDir, 0.8f));
    }

    /// <summary>
    /// Resets sprite scale if charge is canceled or player freezes.
    /// </summary>
    public void ResetJuice()
    {
        if (currentSquashRoutine != null)
        {
            StopCoroutine(currentSquashRoutine);
        }

        if (spriteTransform != null)
        {
            spriteTransform.localScale = originalScale;
        }
    }

    private void StartSquashRoutine(IEnumerator routine)
    {
        if (currentSquashRoutine != null)
        {
            StopCoroutine(currentSquashRoutine);
        }
        currentSquashRoutine = StartCoroutine(routine);
    }

    private IEnumerator DoSquashAndStretch(Vector2 direction, float intensityRatio)
    {
        float elapsed = 0f;
        float intensity = Mathf.Lerp(0.5f, 1f, intensityRatio);

        bool isHorizontal = Mathf.Abs(direction.x) > Mathf.Abs(direction.y);

        Vector3 stretchedScale = isHorizontal
            ? new Vector3(originalScale.x * (1f + stretchAmount * intensity), originalScale.y * (1f - stretchAmount * 0.5f * intensity), originalScale.z)
            : new Vector3(originalScale.x * (1f - stretchAmount * 0.5f * intensity), originalScale.y * (1f + stretchAmount * intensity), originalScale.z);

        // Stretch Phase
        float stretchTime = stretchDuration * 0.4f;
        while (elapsed < stretchTime)
        {
            spriteTransform.localScale = Vector3.Lerp(spriteTransform.localScale, stretchedScale, elapsed / stretchTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Spring back phase
        elapsed = 0f;
        float returnTime = stretchDuration * 0.6f;
        while (elapsed < returnTime)
        {
            spriteTransform.localScale = Vector3.Lerp(stretchedScale, originalScale, elapsed / returnTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        spriteTransform.localScale = originalScale;
    }
}