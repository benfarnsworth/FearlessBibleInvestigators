using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    [Header("Launch Physics Settings")]
    [SerializeField] private float launchForceUp = 4.5f;
    [SerializeField] private float launchForceSide = 1.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Spawns an item's worldPrefab into the scene with persistence & physics launch.
    /// </summary>
    public GameObject SpawnItemAt(string itemIdentifier, Vector3 position, Vector2? customLaunchVector = null)
    {
        if (Inventory.Instance == null)
        {
            Debug.LogWarning("[ItemSpawner] Cannot spawn item: Inventory Instance is missing!");
            return null;
        }

        Item itemData = Inventory.Instance.GetItemFromDatabase(itemIdentifier);
        if (itemData == null)
        {
            Debug.LogWarning($"[ItemSpawner] Could not find item '{itemIdentifier}' in Inventory itemDatabase!");
            return null;
        }

        return SpawnItemAt(itemData, position, customLaunchVector);
    }

    /// <summary>
    /// Direct overload for spawning via Item ScriptableObject reference.
    /// </summary>
    public GameObject SpawnItemAt(Item itemData, Vector3 position, Vector2? customLaunchVector = null)
    {
        if (itemData == null || itemData.worldPrefab == null)
        {
            Debug.LogWarning($"[ItemSpawner] Item is null or missing worldPrefab!");
            return null;
        }

        // 1. Instantiate world prefab
        GameObject spawnedItem = Instantiate(itemData.worldPrefab, position, Quaternion.identity);

        // 2. Register with persistence system
        if (ItemPersistenceManager.Instance != null && !string.IsNullOrEmpty(itemData.itemID))
        {
            ItemPersistenceManager.Instance.TrackDroppedItem(itemData.itemID, position);
        }

        if (spawnedItem.TryGetComponent<PersistentItem>(out var persistent))
        {
            persistent.GenerateUniqueID();
        }

        // 3. Apply launch physics
        if (spawnedItem.TryGetComponent<Rigidbody2D>(out var rb))
        {
            Vector2 launchForce = customLaunchVector ?? new Vector2(Random.Range(-launchForceSide, launchForceSide), launchForceUp);
            rb.AddForce(launchForce, ForceMode2D.Impulse);
        }

        return spawnedItem;
    }
}