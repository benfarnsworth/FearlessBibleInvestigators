using System;
using System.Collections.Generic;
using UnityEngine;

public static class StoryStateManager
{
    private static readonly Dictionary<string, bool> storyFlags = new Dictionary<string, bool>();

    public static void SetFlag(string flagName, bool value)
    {
        if (string.IsNullOrEmpty(flagName)) return;
        storyFlags[flagName] = value;
        Debug.Log($"[STORY STATE] Flag '{flagName}' set to {value}");
    }

    public static bool GetFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName)) return false;
        return storyFlags.TryGetValue(flagName, out bool state) && state;
    }

    public static void ClearAllFlags()
    {
        storyFlags.Clear();
    }

    // --- NPC Meeting Helpers ---
    public static void MarkNPCAsMet(NPCData npc)
    {
        if (npc != null) SetFlag($"met_{npc.npcName}", true);
    }

    public static bool HasMetNPC(NPCData npc)
    {
        return npc != null && GetFlag($"met_{npc.npcName}");
    }

    // --- Condition Evaluator (Handles '&&', '||', '!') ---
    public static bool CheckConditions(string rawCondition)
    {
        if (string.IsNullOrEmpty(rawCondition)) return true;

        string[] orGroups = rawCondition.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string orGroup in orGroups)
        {
            if (EvaluateAndGroup(orGroup))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EvaluateAndGroup(string andGroup)
    {
        string[] conditions = andGroup.Split(new string[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string rawCond in conditions)
        {
            string cond = rawCond.Trim();
            if (string.IsNullOrEmpty(cond)) continue;

            bool expectedValue = true;

            if (cond.StartsWith("!"))
            {
                expectedValue = false;
                cond = cond.Substring(1).Trim();
            }

            if (GetFlag(cond) != expectedValue)
            {
                return false;
            }
        }

        return true;
    }

    // --- SAVE / LOAD HELPERS ---

    public static List<string> GetActiveFlags()
    {
        List<string> activeFlags = new List<string>();
        foreach (var kvp in storyFlags)
        {
            if (kvp.Value) activeFlags.Add(kvp.Key);
        }
        return activeFlags;
    }

    public static void LoadFlags(List<string> loadedFlags)
    {
        storyFlags.Clear();
        if (loadedFlags == null) return;

        foreach (string flag in loadedFlags)
        {
            storyFlags[flag] = true;
        }
        Debug.Log($"[STORY STATE] Restored {loadedFlags.Count} story flags.");
    }

    // --- Cutscene Tracking Helpers ---
    public static void MarkCutsceneAsPlayed(string cutsceneName)
    {
        if (!string.IsNullOrEmpty(cutsceneName)) 
            SetFlag($"cutscene_played_{cutsceneName}", true);
    }

    public static bool HasCutsceneBeenPlayed(string cutsceneName)
    {
        return !string.IsNullOrEmpty(cutsceneName) && GetFlag($"cutscene_played_{cutsceneName}");
    }
}