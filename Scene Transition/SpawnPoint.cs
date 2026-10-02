using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public LocationID spawnPointID;
    
    [Header("Spawn Settings")]
    public FacingDirection spawnFacingDirection = FacingDirection.Down;

    [Header("Spawn Position Offset")]
    [Tooltip("Offset from the door object where the player actually spawns.")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, -1.5f);

    /// <summary>
    /// Returns the world position where the player should spawn, including offset.
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        return transform.position + (Vector3)spawnOffset;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 finalPos = GetSpawnPosition();

        // Draw a green sphere at the spawn location
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(finalPos, 0.4f);

        // Draw a line connecting the door to the spawn point
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, finalPos);
    }
}