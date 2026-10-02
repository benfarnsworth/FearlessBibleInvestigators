using System;
using UnityEngine;

[Serializable]
public class QuestGoal
{
    public GoalType goalType;
    public GameScene targetScene;

    [Header("Custom Display")]
    [Tooltip("Overrides the default text. E.g. 'Herd Reindeer' displays as 'Herd Reindeer (2/5)'.")]
    public string customGoalName;

    [Header("Item Settings (Gathering & Delivery)")]
    [Tooltip("Target Item asset. For Delivery goals, leave NULL if ANY item with an ItemHolder counts.")]
    public Item targetItem;

    [Header("Talk Goal Settings")]
    public NPCData targetNPC;
    public string targetNPCName;

    [Header("Area / Delivery Settings")]
    [Tooltip("Must match the Goal Area ID on the GoalArea trigger script in the scene.")]
    public string goalAreaID;

    [Header("Progress")]
    public int requiredAmount = 1;
    public int currentAmount;

    [HideInInspector] 
    public bool isCompleted; // Tracks if completion has already been processed

    public string targetName
    {
        get
        {
            if (!string.IsNullOrEmpty(customGoalName)) return customGoalName;

            switch (goalType)
            {
                case GoalType.Gathering:
                case GoalType.DeliverToArea:
                    if (targetItem != null)
                    {
                        return !string.IsNullOrEmpty(targetItem.itemName) ? targetItem.itemName : targetItem.name;
                    }
                    return goalType == GoalType.DeliverToArea ? "Objects" : "Unknown Item";

                case GoalType.TalkToNPC:
                    if (targetNPC != null) return targetNPC.npcName;
                    if (!string.IsNullOrEmpty(targetNPCName)) return targetNPCName;
                    return "Unknown Character";

                case GoalType.ScorePoints:
                    return "Points";

                default:
                    return "Target";
            }
        }
    }

    public bool IsReached() => currentAmount >= requiredAmount;

    public bool SetProgress(int newAmount)
    {
        int clamped = Mathf.Clamp(newAmount, 0, requiredAmount);
        if (currentAmount == clamped) return false;

        currentAmount = clamped;

        // Automatically flag as completed when target is hit
        if (IsReached())
        {
            isCompleted = true;
        }

        return true;
    }

    public bool AddProgress(int delta)
    {
        return SetProgress(currentAmount + delta);
    }

    public void ItemCollected(Item item)
    {
        if (goalType == GoalType.Gathering && targetItem != null && item != null)
        {
            bool matchesID = !string.IsNullOrEmpty(item.itemID) && targetItem.itemID == item.itemID;
            bool matchesRef = targetItem == item || targetItem.name == item.name;

            if (matchesID || matchesRef)
            {
                AddProgress(1);
            }
        }
    }

    public void ItemRemoved(Item item)
    {
        if (goalType == GoalType.Gathering && targetItem != null && item != null)
        {
            bool matchesID = !string.IsNullOrEmpty(item.itemID) && targetItem.itemID == item.itemID;
            bool matchesRef = targetItem == item || targetItem.name == item.name;

            if (matchesID || matchesRef)
            {
                AddProgress(-1);
            }
        }
    }

    public void NPCTalkedTo(NPCData npc)
    {
        if (goalType != GoalType.TalkToNPC) return;

        if (targetNPC != null && npc != null && targetNPC == npc)
        {
            AddProgress(1);
        }
        else if (!string.IsNullOrEmpty(targetNPCName) && npc != null && targetNPCName.Equals(npc.npcName, StringComparison.OrdinalIgnoreCase))
        {
            AddProgress(1);
        }
    }

    public void NPCTalkedTo(string npcName)
    {
        if (goalType != GoalType.TalkToNPC || string.IsNullOrEmpty(npcName)) return;

        if (targetNPC != null && targetNPC.npcName.Equals(npcName, StringComparison.OrdinalIgnoreCase))
        {
            AddProgress(1);
        }
        else if (!string.IsNullOrEmpty(targetNPCName) && targetNPCName.Equals(npcName, StringComparison.OrdinalIgnoreCase))
        {
            AddProgress(1);
        }
    }

    public string GetProgressText(string incompleteIcon = "<color=#888888>○</color>", string completedIcon = "<color=#55FF55>✓</color>")
    {
        bool isDone = IsReached();
        string icon = isDone ? completedIcon : incompleteIcon;
        string textColor = isDone ? "<color=#AAAAAA><s>" : "<color=#EEEEEE>";
        string textEnd = isDone ? "</s></color>" : "</color>";

        string description;

        if (!string.IsNullOrEmpty(customGoalName))
        {
            description = (goalType == GoalType.TalkToNPC) 
                ? customGoalName 
                : $"{customGoalName} ({currentAmount}/{requiredAmount})";
        }
        else
        {
            description = goalType switch
            {
                GoalType.TalkToNPC => $"Talk to {targetName}",
                GoalType.Gathering => $"Gather {targetName} ({currentAmount}/{requiredAmount})",
                GoalType.DeliverToArea => targetItem != null 
                    ? $"Deliver {targetName} ({currentAmount}/{requiredAmount})"
                    : $"Deliver items ({currentAmount}/{requiredAmount})",
                GoalType.ScorePoints => $"Score Points ({currentAmount}/{requiredAmount})",
                _ => "Unknown Goal"
            };
        }

        return $"  {icon} {textColor}{description}{textEnd}";
    }

    /// <summary>
    /// Creates a deep runtime copy of this goal with initialized progress.
    /// </summary>
    public QuestGoal Clone()
    {
        return new QuestGoal
        {
            goalType = this.goalType,
            targetScene = this.targetScene,
            customGoalName = this.customGoalName,
            targetItem = this.targetItem,
            targetNPC = this.targetNPC,
            targetNPCName = this.targetNPCName,
            goalAreaID = this.goalAreaID,
            requiredAmount = this.requiredAmount,
            currentAmount = 0,
            isCompleted = false
        };
    }
}