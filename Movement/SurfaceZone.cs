using UnityEngine;

public enum SurfaceType { Normal, Grass, Sand, Snow, Ice, Mud }

[RequireComponent(typeof(Collider2D))]
public class SurfaceZone : MonoBehaviour
{
    public SurfaceType surfaceType = SurfaceType.Normal;

    [Header("Movement Modifiers")]
    [Range(0.1f, 2f)] public float speedMultiplier = 1.0f;
    public float acceleration = 30f;
    public float deceleration = 30f;

    [Header("Particle Effects")]
    public bool enableParticles = true;
    public Color particleTint = Color.white;

    [SerializeField, HideInInspector] private SurfaceType previousType;

    private void OnValidate()
    {
        if (surfaceType != previousType)
        {
            previousType = surfaceType;
            ApplyPresetDefaults();
        }
    }

    private void ApplyPresetDefaults()
    {
        switch (surfaceType)
        {
            case SurfaceType.Normal:
                speedMultiplier = 1.0f;
                acceleration = 30f;
                deceleration = 30f;
                enableParticles = false;
                particleTint = new Color(1f, 1f, 1f, 0f);
                break;

            case SurfaceType.Grass:
                speedMultiplier = 0.95f;
                acceleration = 25f;
                deceleration = 25f;
                enableParticles = true;
                particleTint = new Color(0.35f, 0.65f, 0.2f, 0.8f);
                break;

            case SurfaceType.Sand:
                speedMultiplier = 0.65f;
                acceleration = 12f;
                deceleration = 35f;
                enableParticles = true;
                particleTint = new Color(0.85f, 0.72f, 0.45f, 0.9f);
                break;

            case SurfaceType.Mud:
                speedMultiplier = 0.4f;
                acceleration = 40f;
                deceleration = 40f;
                enableParticles = true;
                particleTint = new Color(0.35f, 0.2f, 0.1f, 1f);
                break;

            case SurfaceType.Snow:
                speedMultiplier = 0.8f;
                acceleration = 15f;
                deceleration = 20f;
                enableParticles = true;
                particleTint = new Color(0.95f, 0.98f, 1f, 0.9f);
                break;

            case SurfaceType.Ice:
                speedMultiplier = 1.2f;
                acceleration = 4f;
                deceleration = 2f;
                enableParticles = true;
                particleTint = new Color(0.8f, 0.95f, 1f, 0.6f);
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;

        if (other.TryGetComponent<TopDownFriction>(out var itemFriction))
        {
            float frictionMultiplier = deceleration / 30f;
            itemFriction.SetFrictionMultiplier(frictionMultiplier);
        }

        if (other.TryGetComponent<PlayerSurfaceController>(out var playerSurface))
        {
            playerSurface.ApplySurfaceZone(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.isTrigger) return;

        if (other.TryGetComponent<TopDownFriction>(out var itemFriction))
        {
            itemFriction.ResetFrictionMultiplier();
        }

        if (other.TryGetComponent<PlayerSurfaceController>(out var playerSurface))
        {
            playerSurface.ResetSurfaceZone(this);
        }
    }
}