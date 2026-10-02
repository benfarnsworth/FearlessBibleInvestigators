using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SortOption
{
    Name,
    Quantity
}

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [System.Serializable]
    public class InventorySlot
    {
        public Item item;
        public int count;

        public InventorySlot(Item item, int count)
        {
            this.item = item;
            this.count = count;
        }
    }

    [Header("Save System Database")]
    [Tooltip("Drag all Item ScriptableObjects into this list so the Save System can restore them by ID!")]
    [SerializeField] private List<Item> itemDatabase = new List<Item>();

    private readonly Dictionary<string, Item> databaseCache = new Dictionary<string, Item>();
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    // Events
    public static event Action OnInventoryChanged;
    public static event Action<Item, int> OnItemAdded;
    public static event Action<Item, int> OnItemRemoved;

    public bool IsDataLoaded { get; private set; } = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeDatabaseCache();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void InitializeDatabaseCache()
    {
        databaseCache.Clear();
        foreach (var item in itemDatabase)
        {
            if (item == null) continue;

            if (!string.IsNullOrEmpty(item.itemID) && !databaseCache.ContainsKey(item.itemID))
                databaseCache.Add(item.itemID, item);

            if (!string.IsNullOrEmpty(item.itemName) && !databaseCache.ContainsKey(item.itemName))
                databaseCache.Add(item.itemName, item);

            if (!databaseCache.ContainsKey(item.name))
                databaseCache.Add(item.name, item);
        }
    }

    public void AddItem(Item item, int amount = 1)
    {
        if (!IsDataLoaded)
        {
            Debug.LogWarning("[INVENTORY] Attempted to add item before save data loaded.");
            return;
        }

        if (item == null || amount <= 0) return;

        InventorySlot slot = FindSlot(item);
        if (slot != null)
        {
            slot.count += amount;
        }
        else
        {
            slots.Add(new InventorySlot(item, amount));
        }

        OnItemAdded?.Invoke(item, amount);
        OnInventoryChanged?.Invoke();
    }

    public bool RemoveItem(Item item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        InventorySlot slot = FindSlot(item);
        if (slot == null || slot.count < amount) return false;

        slot.count -= amount;
        if (slot.count <= 0)
        {
            slots.Remove(slot);
        }

        OnItemRemoved?.Invoke(item, amount);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void DropItem(Item itemToDrop)
    {
        if (itemToDrop == null) return;

        if (itemToDrop.isQuestItem || !itemToDrop.canBeDropped)
        {
            Debug.LogWarning($"[INVENTORY] Cannot drop '{itemToDrop.itemName}': Quest or Key items cannot be dropped!");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || itemToDrop.worldPrefab == null)
        {
            Debug.LogWarning($"[INVENTORY] Cannot drop {itemToDrop.name}: Missing Player tag or World Prefab!");
            return;
        }

        Vector2 dropDirection = Vector2.down;
        if (player.TryGetComponent<PlayerController>(out var controller))
        {
            dropDirection = controller.LastDirection.normalized;
        }

        Vector2 dropPos = (Vector2)player.transform.position + (dropDirection * 1.0f);

        if (ItemSpawner.Instance != null)
        {
            ItemSpawner.Instance.SpawnItemAt(itemToDrop, dropPos, dropDirection * 2.5f);
        }
        else
        {
            Instantiate(itemToDrop.worldPrefab, dropPos, Quaternion.identity);
        }

        RemoveItem(itemToDrop);
    }

    public int GetItemCount(Item item)
    {
        if (item == null) return 0;
        InventorySlot slot = FindSlot(item);
        return slot != null ? slot.count : 0;
    }

    public int GetItemCount(string itemIdentifier)
    {
        if (string.IsNullOrEmpty(itemIdentifier)) return 0;

        int total = 0;
        foreach (var slot in slots)
        {
            if (slot.item != null && (slot.item.itemID == itemIdentifier || slot.item.itemName == itemIdentifier || slot.item.name == itemIdentifier))
            {
                total += slot.count;
            }
        }
        return total;
    }

    public bool HasItem(Item item, int amount = 1) => GetItemCount(item) >= amount;

    public List<Item> GetItems() => slots.Where(s => s != null && s.item != null).Select(s => s.item).ToList();

    public List<InventorySlot> GetSlots() => slots;

    private InventorySlot FindSlot(Item targetItem)
    {
        if (targetItem == null) return null;

        return slots.Find(s =>
        {
            if (s.item == null) return false;
            if (s.item == targetItem) return true;

            if (!string.IsNullOrEmpty(targetItem.itemID) && !string.IsNullOrEmpty(s.item.itemID))
            {
                return s.item.itemID == targetItem.itemID;
            }

            string targetName = !string.IsNullOrEmpty(targetItem.itemName) ? targetItem.itemName : targetItem.name;
            string slotName = !string.IsNullOrEmpty(s.item.itemName) ? s.item.itemName : s.item.name;

            return slotName == targetName;
        });
    }

    public void SortInventory(SortOption sortOption)
    {
        if (slots == null || slots.Count <= 1) return;

        List<InventorySlot> sorted = sortOption switch
        {
            SortOption.Name => slots
                .OrderByDescending(s => s.item != null && s.item.isQuestItem)
                .ThenBy(s => GetItemDisplayName(s.item))
                .ThenByDescending(s => s.count)
                .ToList(),

            SortOption.Quantity => slots
                .OrderByDescending(s => s.item != null && s.item.isQuestItem)
                .ThenByDescending(s => s.count)
                .ThenBy(s => GetItemDisplayName(s.item))
                .ToList(),

            _ => null
        };

        if (sorted != null)
        {
            slots.Clear();
            slots.AddRange(sorted);
            OnInventoryChanged?.Invoke();
        }
    }

    private string GetItemDisplayName(Item item)
    {
        if (item == null) return string.Empty;
        return !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.name;
    }

    public List<InventorySaveEntry> GetSaveData()
    {
        List<InventorySaveEntry> saveData = new List<InventorySaveEntry>();
        foreach (var slot in slots)
        {
            if (slot.item != null)
            {
                string id = !string.IsNullOrEmpty(slot.item.itemID) ? slot.item.itemID : slot.item.name;
                saveData.Add(new InventorySaveEntry { itemID = id, count = slot.count });
            }
        }
        return saveData;
    }

    public void LoadSaveData(List<InventorySaveEntry> saveData)
    {
        slots.Clear();

        if (saveData == null)
        {
            IsDataLoaded = true;
            OnInventoryChanged?.Invoke();
            return;
        }

        foreach (var entry in saveData)
        {
            Item foundItem = GetItemFromDatabase(entry.itemID);
            if (foundItem != null)
            {
                slots.Add(new InventorySlot(foundItem, entry.count));
            }
        }

        IsDataLoaded = true;
        OnInventoryChanged?.Invoke();
    }

    public bool AddItem(string itemIdentifier, int amount = 1)
    {
        Item foundItem = GetItemFromDatabase(itemIdentifier);
        if (foundItem == null) return false;

        AddItem(foundItem, amount);
        return true;
    }

    public bool RemoveItem(string itemIdentifier, int amount = 1)
    {
        Item foundItem = GetItemFromDatabase(itemIdentifier);
        if (foundItem == null) return false;

        return RemoveItem(foundItem, amount);
    }

    public Item GetItemFromDatabase(string itemIdentifier)
    {
        if (string.IsNullOrEmpty(itemIdentifier)) return null;

        if (databaseCache.Count == 0 && itemDatabase.Count > 0)
        {
            InitializeDatabaseCache();
        }

        return databaseCache.TryGetValue(itemIdentifier, out Item item) ? item : null;
    }
}