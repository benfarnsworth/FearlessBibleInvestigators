using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerSurfaceController : MonoBehaviour
{
    [Header("Base Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 7f;
    [SerializeField] private float baseAcceleration = 30f;
    [SerializeField] private float baseDeceleration = 30f;

    [Header("Dash Juice Settings")]
    [SerializeField] private int dashParticleBurstCount = 25;
    [Tooltip("Speed multiplier applied right as dash ends to create recoil impact (e.g., 0.35 = 35% of normal speed)")]
    [SerializeField] private float postDashSlowdownMultiplier = 0.35f;
    [Tooltip("How long the slowdown skid lasts in seconds before normal control returns")]
    [SerializeField] private float postDashSlowdownDuration = 0.12f;

    [Header("Feet Particles")]
    [SerializeField] private ParticleSystem feetParticles;
    [SerializeField] private bool emitParticlesOnNormalGround = false;
    [SerializeField] private Color normalGroundParticleColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);

    [Header("Debug Options")]
    [SerializeField] private bool showOnScreenDebug = true;
    [SerializeField] private bool logSurfaceChanges = true;

    private Rigidbody2D rb;
    private readonly List<SurfaceZone> activeZones = new List<SurfaceZone>();
    private SurfaceZone currentSurfaceZone;
    private Coroutine dashCoroutine;

    /// <summary>
    /// True while the player is dashing or in post-dash recovery.
    /// </summary>
    public bool IsDashing { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        HandleFeetParticles();
    }

    private void OnGUI()
    {
        if (!showOnScreenDebug) return;

        string surfaceName = currentSurfaceZone != null ? currentSurfaceZone.surfaceType.ToString() : "Normal (Default)";
        float speedMult = currentSurfaceZone != null ? currentSurfaceZone.speedMultiplier : 1.0f;
        bool particlesActive = feetParticles != null && feetParticles.isPlaying && feetParticles.emission.enabled;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(10, 10, 10, 10)
        };

        string displayText = $"<b>[SURFACE DEBUG]</b>\n" +
                            $"<b>Surface:</b> <color=yellow>{surfaceName}</color>\n" +
                            $"<b>Speed Mult:</b> {speedMult}x\n" +
                            $"<b>Dash State:</b> {(IsDashing ? "<color=cyan>DASHING</color>" : "Normal")}\n" +
                            $"<b>Particles:</b> {(particlesActive ? "<color=green>EMITTING</color>" : "<color=red>OFF</color>")}";

        GUI.Box(new Rect(10, 10, 220, 110), displayText, boxStyle);
    }

    private void HandleFeetParticles()
    {
        if (feetParticles == null) return;

        var emission = feetParticles.emission;
        var main = feetParticles.main;

        bool isMoving = rb.linearVelocity.sqrMagnitude > 0.1f;
        bool shouldEmit = false;
        Color targetColor = Color.white;

        if (currentSurfaceZone != null)
        {
            shouldEmit = currentSurfaceZone.enableParticles && isMoving;
            targetColor = currentSurfaceZone.particleTint;
        }
        else
        {
            shouldEmit = emitParticlesOnNormalGround && isMoving;
            targetColor = normalGroundParticleColor;
        }

        if (shouldEmit)
        {
            main.startColor = targetColor;
            emission.enabled = true;

            if (!feetParticles.isPlaying)
            {
                feetParticles.Play();
            }
        }
        else
        {
            emission.enabled = false;
        }
    }

    public void ProcessMovement(Vector2 inputDirection)
    {
        // Ignore normal walk movement inputs while dashing or recovering from dash
        if (IsDashing) return;

        float targetSpeed = baseMoveSpeed;
        float accel = baseAcceleration;
        float decel = baseDeceleration;

        if (currentSurfaceZone != null)
        {
            targetSpeed *= currentSurfaceZone.speedMultiplier;
            accel = currentSurfaceZone.acceleration;
            decel = currentSurfaceZone.deceleration;
        }

        Vector2 targetVelocity = inputDirection.normalized * targetSpeed;
        float rate = (inputDirection.sqrMagnitude > 0.01f) ? accel : decel;

        rb.linearVelocity = Vector2.MoveTowards(
            rb.linearVelocity,
            targetVelocity,
            rate * Time.fixedDeltaTime
        );
    }

    /// <summary>
    /// Triggers a juicy dash with particle burst and post-dash skid recovery.
    /// </summary>
    public void PerformDash(Vector2 direction, float dashSpeed, float dashDuration)
    {
        if (direction.sqrMagnitude < 0.01f) return;

        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
        }

        dashCoroutine = StartCoroutine(DashRoutine(direction.normalized, dashSpeed, dashDuration));
    }

    private IEnumerator DashRoutine(Vector2 dashDir, float dashSpeed, float dashDuration)
    {
        IsDashing = true;

        // 1. Emit Particle Burst in the surface color (or normal ground color if zone is null)
        if (feetParticles != null)
        {
            var main = feetParticles.main;
            Color targetColor = (currentSurfaceZone != null) 
                ? currentSurfaceZone.particleTint 
                : normalGroundParticleColor;

            main.startColor = targetColor;
            feetParticles.Emit(dashParticleBurstCount);
        }

        // 2. High-Speed Dash Phase
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            rb.linearVelocity = dashDir * dashSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. Post-Dash Slowdown Skid Phase (Impact Weight)
        elapsed = 0f;
        float currentSurfaceMult = currentSurfaceZone != null ? currentSurfaceZone.speedMultiplier : 1.0f;
        float postDashSpeed = (baseMoveSpeed * currentSurfaceMult) * postDashSlowdownMultiplier;
        Vector2 slowdownStartVelocity = dashDir * postDashSpeed;

        while (elapsed < postDashSlowdownDuration)
        {
            // Smoothly decay velocity during recovery skid
            rb.linearVelocity = Vector2.Lerp(slowdownStartVelocity, Vector2.zero, elapsed / postDashSlowdownDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        IsDashing = false;
        dashCoroutine = null;
    }

    public void ResetVelocity()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void ApplySurfaceZone(SurfaceZone zone)
    {
        if (!activeZones.Contains(zone))
        {
            activeZones.Add(zone);
        }

        UpdateCurrentSurfaceZone();

        if (logSurfaceChanges)
        {
            Debug.Log($"<color=cyan>[Surface System]</color> Entered <b>{zone.surfaceType}</b>", zone);
        }
    }

    public void ResetSurfaceZone(SurfaceZone zone)
    {
        if (activeZones.Contains(zone))
        {
            activeZones.Remove(zone);
        }

        UpdateCurrentSurfaceZone();

        if (logSurfaceChanges)
        {
            string currentName = currentSurfaceZone != null ? currentSurfaceZone.surfaceType.ToString() : "Normal Ground";
            Debug.Log($"<color=orange>[Surface System]</color> Exited <b>{zone.surfaceType}</b> | Active: <b>{currentName}</b>");
        }
    }

    private void UpdateCurrentSurfaceZone()
    {
        if (activeZones.Count > 0)
        {
            currentSurfaceZone = activeZones[activeZones.Count - 1];
        }
        else
        {
            currentSurfaceZone = null;
        }
    }
}