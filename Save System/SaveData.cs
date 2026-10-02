using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventorySaveEntry
{
    public string itemID;
    public int count;
}

[System.Serializable]
public class DroppedItemSaveEntry
{
    public string itemID;
    public Vector3 position;
}

[System.Serializable]
public class SaveData
{
    public string sceneName;
    public Vector3 playerPosition;

    public List<InventorySaveEntry> inventoryData = new List<InventorySaveEntry>();
    public List<string> activeStoryFlags = new List<string>();
    public List<string> collectedItemIDs = new List<string>(); 
    public List<DroppedItemSaveEntry> droppedWorldItems = new List<DroppedItemSaveEntry>();
    
    // Explicitly initialized to prevent null references during empty state checks
    public List<QuestManager.QuestSaveEntry> questData = new List<QuestManager.QuestSaveEntry>();
}