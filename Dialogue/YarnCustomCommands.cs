using UnityEngine;
using Yarn.Unity;

public static class YarnCustomCommands
{
    // ==========================================
    // YARN FUNCTIONS (Used in <<if>> checks)
    // ==========================================

    [YarnFunction("has_met_npc")]
    public static bool HasMetNPC(string npcName)
    {
        // 1. Check if met_NPCName flag is set directly
        if (StoryStateManager.CheckConditions($"met_{npcName}")) 
            return true;

        // 2. Fallback: If StoryStateManager uses an NPCData overload, check that flag key format
        return StoryStateManager.CheckConditions($"met_{npcName.Replace(" ", "")}");
    }

    [YarnFunction("has_item")]
    public static bool HasItem(string itemName, int count = 1)
    {
        if (Inventory.Instance == null) return false;
        return Inventory.Instance.GetItemCount(itemName) >= count;
    }

    [YarnFunction("check_flag")]
    public static bool CheckFlag(string flagName)
    {
        return StoryStateManager.CheckConditions(flagName);
    }


    // ==========================================
    // YARN COMMANDS (Used as <<actions>>)
    // ==========================================

    [YarnCommand("mark_npc_met")]
    public static void MarkNPCMet(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return;
        
        // Set both raw and sanitized flags so conditions always evaluate correctly
        StoryStateManager.SetFlag($"met_{npcName}", true);
        StoryStateManager.SetFlag($"met_{npcName.Replace(" ", "")}", true);

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.TrackNPCTalkedTo(npcName);
        }
    }

    [YarnCommand("set_flag")]
    public static void SetFlag(string flagName)
    {
        StoryStateManager.SetFlag(flagName, true);
    }

    [YarnCommand("give_item")]
    public static void GiveItem(string itemName, int count = 1)
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.AddItem(itemName, count);
        }
    }

    [YarnCommand("take_item")]
    public static void TakeItem(string itemName, int count = 1)
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.RemoveItem(itemName, count);
        }
    }

    [YarnCommand("accept_quest")]
    public static void AcceptQuest(string questName)
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.AcceptQuest(questName);
        }
    }

    [YarnCommand("complete_quest")]
    public static void CompleteQuest(string questName)
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteQuest(questName);
        }
    }

    [YarnFunction("is_quest_ready")]
    public static bool IsQuestReady(string questName)
    {
        if (QuestManager.Instance != null)
        {
            return QuestManager.Instance.IsQuestReady(questName);
        }
        
        // Safety fallback if QuestManager is absent
        return StoryStateManager.CheckConditions($"Quest_{questName}_Ready");
    }

    [YarnFunction("is_quest_active")]
    public static bool IsQuestActive(string questName)
    {
        if (QuestManager.Instance != null)
        {
            return QuestManager.Instance.IsQuestActive(questName);
        }

        return StoryStateManager.CheckConditions($"Quest_{questName}_Active");
    }

    [YarnFunction("is_quest_completed")]
    public static bool IsQuestCompleted(string questName)
    {
        if (QuestManager.Instance != null)
        {
            return QuestManager.Instance.IsQuestCompleted(questName);
        }

        return StoryStateManager.CheckConditions($"Quest_{questName}_Completed");
    }

    [YarnCommand("speaker")]
    public static void SetDialogueSpeaker(string speakerName)
    {
        TypewriterAudio.Instance?.SetSpeakerByName(speakerName);
    }

    [YarnCommand("drop_item")]
    public static void DropItem(string itemName, string npcName = "")
    {
        if (ItemSpawner.Instance == null)
        {
            Debug.LogWarning("[YarnCustomCommands] Cannot drop item: ItemSpawner Instance is missing in the scene!");
            return;
        }

        Vector3 spawnPosition = Vector3.zero;
        bool positionFound = false;

        // 1. Try to find the specified NPC in the scene by GameObject name
        if (!string.IsNullOrEmpty(npcName))
        {
            GameObject npcObject = GameObject.Find(npcName);
            if (npcObject != null)
            {
                spawnPosition = npcObject.transform.position;
                positionFound = true;
            }
        }

        // 2. Fallback: Find player position if NPC wasn't found or specified
        if (!positionFound)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                spawnPosition = player.transform.position;
                positionFound = true;
            }
        }

        if (positionFound)
        {
            ItemSpawner.Instance.SpawnItemAt(itemName, spawnPosition);
        }
    }

    /// <summary>
    /// Forces an NPC into a specific state (e.g., <<npc_state Guard Idle>>) via Yarn Spinner
    /// </summary>
    [YarnCommand("npc_state")]
    public static void SetNPCState(string npcName, string stateName)
    {
        GameObject npcObj = GameObject.Find(npcName);
        if (npcObj != null && npcObj.TryGetComponent<NPCController>(out var npc))
        {
            if (System.Enum.TryParse(stateName, true, out NPCController.NPCState parsedState))
            {
                npc.SetState(parsedState);
            }
            else
            {
                Debug.LogWarning($"[CutsceneManager] Unknown NPC state: {stateName}");
            }
        }
        else
        {
            Debug.LogError($"[CutsceneManager] Could not find NPCController on '{npcName}'");
        }
    }
}