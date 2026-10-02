using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Quest", menuName = "Quest System/Quest")]
public class Quest : ScriptableObject
{
    [Header("Basic Info")]
    public string questName; // Internal ID (e.g., "PeteAppleQuest")
    public string questDisplayName = "Null Quest!"; // Player-facing title
    [TextArea(3, 5)] public string description;

    [Header("NPC Assignments")]
    [Tooltip("The NPC who offers/gives this quest.")]
    public NPCData questGiverNPC;
    public string questGiverName;

    [Tooltip("The NPC who accepts quest turn-in. If left empty, defaults to the quest giver.")]
    public NPCData turnInNPC;
    public string turnInNPCName;

    [Header("Rewards")]
    public int goldReward;
    public int expReward;
    public List<Item> itemRewards = new List<Item>();

    [Header("Timer Settings")]
    public bool isTimed = false;
    [Tooltip("Time limit in seconds.")]
    public float timeLimit = 60f;
    [HideInInspector] public float timeRemaining;

    [Header("Prerequisites & Scene")]
    [Tooltip("Quest that must be completed before this quest becomes available.")]
    public Quest prerequisiteQuest;
    public GameScene targetScene; 

    [Header("Audio Overrides")]
    public AudioClip customAcceptSFX;
    public AudioClip customCompleteSFX;

    [Header("Runtime State")]
    public QuestState state = QuestState.Unassigned;
    public List<QuestGoal> goals = new List<QuestGoal>();

    public string DisplayTitle => !string.IsNullOrEmpty(questDisplayName) ? questDisplayName : questName;

    public string GetGiverName()
    {
        if (questGiverNPC != null && !string.IsNullOrEmpty(questGiverNPC.npcName))
            return questGiverNPC.npcName;
        return questGiverName ?? string.Empty;
    }

    public string GetTurnInName()
    {
        if (turnInNPC != null && !string.IsNullOrEmpty(turnInNPC.npcName))
            return turnInNPC.npcName;
        if (!string.IsNullOrEmpty(turnInNPCName))
            return turnInNPCName;
        return GetGiverName();
    }

    public void InitializeTimer()
    {
        if (isTimed) timeRemaining = timeLimit;
    }

    public bool IsCompleted()
    {
        if (goals == null || goals.Count == 0) return false;
        return goals.TrueForAll(goal => goal != null && goal.IsReached());
    }

    public void Complete() => state = QuestState.Completed;
    public void Fail() => state = QuestState.Failed;
}