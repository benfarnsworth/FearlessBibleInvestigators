using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerKick : MonoBehaviour
{
    [Header("Kick Inputs & Controls")]
    [SerializeField] private KeyCode kickKey = KeyCode.Space;
    [SerializeField] private float minKickForce = 8f;
    [SerializeField] private float maxKickForce = 20f;
    [SerializeField] private float maxChargeTime = 1.0f;

    [Header("Detection Settings")]
    [SerializeField] private float kickRadius = 1.2f;
    [SerializeField] private float kickOffset = 0.8f;
    [SerializeField] private LayerMask kickableLayers;

    [Header("Aim Assist Settings")]
    [SerializeField] private KickAimLine aimLine;
    [SerializeField] private float baseHeightOffset = 0.4f;
    [SerializeField] private float forwardPush = 0.6f;

    [Header("Screen Shake Settings")]
    [SerializeField] private float minShakeIntensity = 0.03f;
    [SerializeField] private float maxShakeIntensity = 0.2f;
    [Space(5)]
    [SerializeField] private float minShakeDuration = 0.08f;
    [SerializeField] private float maxShakeDuration = 0.22f;

    private PlayerController playerController;
    private PlayerJuice playerJuice;
    private Rigidbody2D playerRb;

    private float chargeTimer = 0f;
    private bool isCharging = false;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerRb = GetComponent<Rigidbody2D>();
        playerJuice = GetComponent<PlayerJuice>();
    }

    private void Update()
    {
        if (playerController != null && !playerController.enabled)
        {
            CancelCharge();
            return;
        }

        // 1. Start Charging
        if (Input.GetKeyDown(kickKey))
        {
            isCharging = true;
            chargeTimer = 0f;
        }

        // 2. Build Charge & Update Aim Line / Juice
        if (isCharging && Input.GetKey(kickKey))
        {
            chargeTimer += Time.deltaTime;
            chargeTimer = Mathf.Clamp(chargeTimer, 0f, maxChargeTime);

            float chargeRatio = chargeTimer / maxChargeTime;

            UpdateAimLine(chargeRatio);

            if (playerJuice != null)
            {
                playerJuice.ApplyChargeWindup(chargeRatio);
            }
        }
        else if (playerController != null && playerController.IsAimLocked)
        {
            // Show base aim guide line while holding Shift, even if not charging yet
            UpdateAimLine(0.1f);
        }
        else if (!isCharging)
        {
            if (aimLine != null) aimLine.Hide();
        }

        // 3. Release Kick
        if (isCharging && Input.GetKeyUp(kickKey))
        {
            PerformKick();
            CancelCharge();
        }
    }

    private void CancelCharge()
    {
        isCharging = false;
        chargeTimer = 0f;
        if (aimLine != null && (playerController == null || !playerController.IsAimLocked))
        {
            aimLine.Hide();
        }
    }

    private void UpdateAimLine(float chargeRatio)
    {
        if (aimLine == null) return;

        Vector2 facingDir = playerController != null ? playerController.LastDirection : Vector2.down;
        Vector2 origin = (Vector2)transform.position 
                       + new Vector2(0f, baseHeightOffset) 
                       + (facingDir.normalized * forwardPush);

        aimLine.SetAim(origin, facingDir, chargeRatio);
    }

    private void PerformKick()
    {
        Vector2 facingDir = playerController != null ? playerController.LastDirection : Vector2.down;
        Vector2 kickCenter = (Vector2)transform.position + (facingDir * kickOffset);

        float chargeRatio = chargeTimer / maxChargeTime;
        float finalKickForce = Mathf.Lerp(minKickForce, maxKickForce, chargeRatio);

        float calculatedShakeIntensity = Mathf.Lerp(minShakeIntensity, maxShakeIntensity, chargeRatio);
        float calculatedShakeDuration = Mathf.Lerp(minShakeDuration, maxShakeDuration, chargeRatio);

        if (playerJuice != null)
        {
            playerJuice.TriggerKickRelease(facingDir, chargeRatio);
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(kickCenter, kickRadius, kickableLayers);

        foreach (Collider2D hit in hitColliders)
        {
            Vector2 hitPos = hit.attachedRigidbody != null ? hit.attachedRigidbody.position : (Vector2)hit.transform.position;
            Vector2 radialDir = (hitPos - (Vector2)transform.position).normalized;
            if (radialDir == Vector2.zero) radialDir = facingDir;

            Vector2 finalDirection = Vector2.Lerp(radialDir, facingDir.normalized, chargeRatio).normalized;

            if (hit.TryGetComponent<NPCZoneDefender>(out var defender))
            {
                defender.TakeStun(finalDirection, finalKickForce);

                if (CameraJuice.Instance != null)
                {
                    CameraJuice.Instance.TriggerScreenShake(calculatedShakeDuration, calculatedShakeIntensity);
                }
                continue;
            }

            if (hit.TryGetComponent<IKickable>(out var kickable))
            {
                kickable.OnKicked(finalDirection, chargeRatio);
            }

            Rigidbody2D itemRb = hit.attachedRigidbody;
            if (itemRb != null && itemRb != playerRb)
            {
                itemRb.linearVelocity = Vector2.zero;
                itemRb.AddForce(finalDirection * finalKickForce, ForceMode2D.Impulse);

                if (itemRb.TryGetComponent<ImpactJuice>(out var impactJuice))
                {
                    impactJuice.TriggerJuice(finalKickForce);
                }

                if (CameraJuice.Instance != null)
                {
                    CameraJuice.Instance.TriggerScreenShake(calculatedShakeDuration, calculatedShakeIntensity);
                }
            }
        }
    }
}