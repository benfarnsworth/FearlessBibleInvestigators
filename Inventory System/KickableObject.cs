using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class KickableObject : MonoBehaviour, IKickable
{
    [Header("Kick Destruction")]
    [Tooltip("If true, a fully/partially charged kick shatters this on direct impact.")]
    [SerializeField] private bool breakOnDirectKick = true;
    [Range(0f, 1f)]
    [SerializeField] private float minChargeToBreak = 0.5f;

    [Header("Collision Destruction")]
    [Tooltip("If true, crashing into walls or objects at high velocity shatters this.")]
    [SerializeField] private bool breakOnCollision = true;
    [SerializeField] private float minBreakVelocity = 9.0f;

    [Header("Loot Drop Setup")]
    [Tooltip("The CollectibleItem prefab to spawn when this breaks (leave empty for non-loot objects).")]
    [SerializeField] private GameObject itemToDropPrefab;
    [SerializeField] private int minDropCount = 1;
    [SerializeField] private int maxDropCount = 3;
    [SerializeField] private float dropScatterForce = 5f;

    [Header("FX & Juice")]
    [SerializeField] private GameObject shatterFxPrefab;
    [SerializeField] private AudioClip shatterSFX;

    private Rigidbody2D rb;
    private bool isBroken = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // --- 1. IKickable Reaction (Direct Kick) ---
    public void OnKicked(Vector2 kickDirection, float chargeRatio)
    {
        if (isBroken) return;

        if (breakOnDirectKick && chargeRatio >= minChargeToBreak)
        {
            Shatter(kickDirection);
        }
    }

    // --- 2. High-Speed Impact & Chain Reactions ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBroken || !breakOnCollision) return;

        // Calculate impact speed relative to collision target
        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed >= minBreakVelocity)
        {
            Vector2 impactDirection = -collision.relativeVelocity.normalized;

            // Chain Reaction: If we hit another KickableObject, break it too!
            if (collision.gameObject.TryGetComponent<KickableObject>(out var otherKickable))
            {
                otherKickable.Shatter(collision.relativeVelocity.normalized);
            }

            Shatter(impactDirection);
        }
    }

    // --- 3. Shatter Logic ---
    public void Shatter(Vector2 forceDirection)
    {
        if (isBroken) return;
        isBroken = true;

        // Trigger Camera Juice / Hit Stop on break
        if (CameraJuice.Instance != null)
        {
            CameraJuice.Instance.TriggerImpactJuice(0.2f, 0.15f, 0.05f);
        }

        // Audio
        if (shatterSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(shatterSFX, 0.85f, 1.15f);
        }

        // Particle FX
        if (shatterFxPrefab != null)
        {
            GameObject fx = Instantiate(shatterFxPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2.0f);
        }

        // Spawn Collectibles
        SpawnLoot(forceDirection);

        // Destroy self
        Destroy(gameObject);
    }

    private void SpawnLoot(Vector2 forceDirection)
    {
        if (itemToDropPrefab == null) return;

        int dropCount = Random.Range(minDropCount, maxDropCount + 1);

        for (int i = 0; i < dropCount; i++)
        {
            GameObject loot = Instantiate(itemToDropPrefab, transform.position, Quaternion.identity);

            if (loot.TryGetComponent<Rigidbody2D>(out var lootRb))
            {
                // Scatter loot outwards in a 70-degree cone in the kick direction
                Vector2 spreadDir = Quaternion.Euler(0, 0, Random.Range(-35f, 35f)) * forceDirection;
                float force = Random.Range(dropScatterForce * 0.6f, dropScatterForce);
                
                lootRb.AddForce(spreadDir * force, ForceMode2D.Impulse);
            }
        }
    }
}