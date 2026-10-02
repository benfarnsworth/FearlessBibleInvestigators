using System.Collections.Generic;
using UnityEngine;

public class ItemPersistenceManager : MonoBehaviour
{
    public static ItemPersistenceManager Instance { get; private set; }

    private HashSet<string> collectedItems = new HashSet<string>();
    private List<DroppedItemSaveEntry> droppedItems = new List<DroppedItemSaveEntry>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsCollected(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return false;
        return collectedItems.Contains(itemID);
    }

    public void MarkAsCollected(string itemID)
    {
        if (!string.IsNullOrEmpty(itemID))
        {
            collectedItems.Add(itemID);
        }
    }

    public void TrackDroppedItem(string itemID, Vector3 position)
    {
        if (string.IsNullOrEmpty(itemID)) return;

        droppedItems.Add(new DroppedItemSaveEntry { itemID = itemID, position = position });
    }

    public void RemoveDroppedItem(string itemID, Vector3 position, float threshold = 0.5f)
    {
        if (string.IsNullOrEmpty(itemID)) return;

        droppedItems.RemoveAll(entry => entry.itemID == itemID && Vector3.Distance(entry.position, position) < threshold);
    }

    public void SaveState(SaveData data)
    {
        if (data == null) return;

        data.collectedItemIDs = new List<string>(collectedItems);
        data.droppedWorldItems = new List<DroppedItemSaveEntry>(droppedItems);
    }

    public void LoadState(SaveData data)
    {
        // Clear RAM state first so previous run data doesn't leak into this session
        collectedItems.Clear();
        droppedItems.Clear();

        if (data == null) return;

        if (data.collectedItemIDs != null)
        {
            collectedItems = new HashSet<string>(data.collectedItemIDs);
        }

        if (data.droppedWorldItems != null)
        {
            droppedItems = new List<DroppedItemSaveEntry>(data.droppedWorldItems);
        }
    }

    public void RestoreDroppedItems(SaveData data)
    {
        if (data == null || data.droppedWorldItems == null || Inventory.Instance == null) return;

        foreach (var entry in data.droppedWorldItems)
        {
            Item item = Inventory.Instance.GetItemFromDatabase(entry.itemID);
            if (item == null || item.worldPrefab == null) continue;

            if (ItemSpawner.Instance != null)
            {
                // Spawn without adding launch velocity
                ItemSpawner.Instance.SpawnItemAt(item, entry.position, Vector2.zero);
            }
            else
            {
                Instantiate(item.worldPrefab, entry.position, Quaternion.identity);
            }
        }
    }
}