using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New NPC Data", menuName = "Dialogue/NPC Data")]
public class NPCData : ScriptableObject
{
    public string npcName = "NPC";
    public Sprite portrait;
    [TextArea(2, 5)] public string description;

    [Header("Quests")]
    public List<Quest> assignedQuests = new List<Quest>();

    [Header("Voice & Typing Audio")]
    public AudioClip typingSound;
    [Range(0.5f, 1.5f)] public float minPitch = 0.85f;
    [Range(0.5f, 1.5f)] public float maxPitch = 1.15f;
}