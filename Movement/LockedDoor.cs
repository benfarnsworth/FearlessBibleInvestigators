using UnityEngine;

public class LockedDoor : MonoBehaviour
{
    [SerializeField] private string unlockEventName = "UnlockChest";
    
    [Header("Loot Settings")]
    [SerializeField] private GameObject itemPrefabToSpawn; // The collectible item prefab
    [SerializeField] private Transform spawnPoint;          // Where the item pops out from
    [SerializeField] private float launchForceUp = 5f;     // Upward force
    [SerializeField] private float launchForceSide = 2f;   // Random left/right variance

    private void OnEnable() => DialogueManager.OnDialogueEventTriggered += HandleDialogueEvent;
    private void OnDisable() => DialogueManager.OnDialogueEventTriggered -= HandleDialogueEvent;

    private void HandleDialogueEvent(string eventName)
    {
        if (eventName == unlockEventName) Unlock();
    }

    private void Unlock()
    {
        
        // 1. Play the animation
        GetComponent<Animator>().SetTrigger("Open");
        
        // 2. Spawn the physical item
        SpawnLoot();

        this.enabled = false; 
    }

    private void SpawnLoot()
    {
        if (itemPrefabToSpawn == null) return;

        // Determine spawn position (use chest position if no explicit spawnPoint is assigned)
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;

        // Instantiate the item into the scene
        GameObject spawnedItem = Instantiate(itemPrefabToSpawn, spawnPos, Quaternion.identity);

        // Get the Rigidbody2D to yeet it
        Rigidbody2D rb = spawnedItem.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Generate a random arc (a lot upward, a little bit left or right)
            float randomX = Random.Range(-launchForceSide, launchForceSide);
            Vector2 launchVelocity = new Vector2(randomX, launchForceUp);

            // Apply the physics push!
            rb.AddForce(launchVelocity, ForceMode2D.Impulse);
        }
    }
}