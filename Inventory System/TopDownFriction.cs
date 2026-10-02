using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownFriction : MonoBehaviour
{
    [Header("Snap Brake Settings")]
    [SerializeField] private float snapThreshold = 0.8f;
    [SerializeField] private float brakeForce = 12f;

    private Rigidbody2D rb;
    private float currentFrictionMultiplier = 1.0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetFrictionMultiplier(float multiplier) => currentFrictionMultiplier = multiplier;
    public void ResetFrictionMultiplier() => currentFrictionMultiplier = 1.0f;

    private void FixedUpdate()
    {
        float currentSpeed = rb.linearVelocity.magnitude;

        if (currentSpeed > 0f && currentSpeed < snapThreshold)
        {
            // Ice reduces brakeForce (slides further), Mud increases brakeForce (stops instantly)
            float effectiveBrake = brakeForce * currentFrictionMultiplier;

            rb.linearVelocity = Vector2.MoveTowards(
                rb.linearVelocity, 
                Vector2.zero, 
                effectiveBrake * Time.fixedDeltaTime
            );
        }
    }
}