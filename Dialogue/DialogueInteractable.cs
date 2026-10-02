using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

[RequireComponent(typeof(Collider2D))]
public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("UI Prompt")]
    [SerializeField] private string promptText = "Talk";

    [Header("Display Name (Optional)")]
    [Tooltip("Override the NPC's prompt name. If blank, uses NPCData or default.")]
    [SerializeField] private string overrideDisplayName;

    [Header("NPC Data (Optional)")]
    [SerializeField] public NPCData npcData;

    [Header("Yarn Dialogue Settings")]
    [SerializeField] private string startNodeName = "StartNode";

    private DialogueRunner dialogueRunner;
    private Animator animator;

    public NPCData NpcData => npcData;

    private DialogueRunner Runner
    {
        get
        {
            if (dialogueRunner == null)
            {
                dialogueRunner = FindFirstObjectByType<DialogueRunner>();
            }
            return dialogueRunner;
        }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(overrideDisplayName)) return overrideDisplayName;
            if (npcData != null && !string.IsNullOrEmpty(npcData.npcName)) return npcData.npcName;
            return "NPC";
        }
    }

    public string GetInteractionPrompt()
    {
        // Check if player has any ready turn-in quests for this NPC directly via QuestManager
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(DisplayName))
        {
            List<Quest> readyQuests = QuestManager.Instance.GetReadyTurnInQuestsForNPC(DisplayName);
            if (readyQuests != null && readyQuests.Count > 0)
            {
                return $"Turn in {readyQuests[0].DisplayTitle}";
            }
        }

        if (!string.IsNullOrEmpty(DisplayName) && DisplayName != "NPC")
        {
            return $"Talk to {DisplayName}";
        }

        return promptText;
    }

    public void Interact()
    {
        TriggerDialogue();
    }

    public void TriggerDialogue()
    {
        if (Runner == null || Runner.IsDialogueRunning) return;

        if (npcData != null)
        {
            StoryStateManager.SetFlag($"met_{npcData.npcName}", true);

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.TrackNPCTalkedTo(npcData);
            }

            if (TypewriterAudio.Instance != null)
            {
                TypewriterAudio.Instance.SetSpeaker(npcData);
            }
        }
        else if (!string.IsNullOrEmpty(overrideDisplayName) && QuestManager.Instance != null)
        {
            QuestManager.Instance.TrackNPCTalkedTo(overrideDisplayName);
        }

        FacePlayer();

        if (!string.IsNullOrEmpty(startNodeName))
        {
            Debug.Log($"[DIALOGUE] Starting Yarn node: '{startNodeName}' on {gameObject.name}");
            Runner.StartDialogue(startNodeName);
        }
    }

    private void FacePlayer()
    {
        if (animator == null) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector2 dir = player.transform.position - transform.position;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            animator.SetFloat("MoveX", dir.x > 0 ? 1 : -1);
            animator.SetFloat("MoveY", 0);
        }
        else
        {
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", dir.y > 0 ? 1 : -1);
        }
    }
}