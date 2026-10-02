using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    [SerializeField] private List<NPCData> npcDatabase = new List<NPCData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public NPCData GetNPCData(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return npcDatabase.Find(npc => npc != null && (npc.npcName == name || npc.name == name));
    }
}