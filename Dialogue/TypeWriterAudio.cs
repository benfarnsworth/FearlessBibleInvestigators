using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

public class TypewriterAudio : ActionMarkupHandler
{
    public static TypewriterAudio Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("UI References")]
    [Tooltip("Drag your UI's Character Name Text box here.")]
    [SerializeField] private TMP_Text characterNameText;

    [Header("Default Audio Settings (Fallback)")]
    [SerializeField] private AudioClip defaultTextBlipClip;
    [Range(0.5f, 1.5f)] [SerializeField] private float defaultMinPitch = 0.85f;
    [Range(0.5f, 1.5f)] [SerializeField] private float defaultMaxPitch = 1.15f;

    [Header("Frequency & Filtering")]
    [Tooltip("Play sound every N characters (2 is recommended for smooth audio).")]
    [SerializeField] private int frequency = 2;
    [Tooltip("Do not play blip sounds on spaces or punctuation marks.")]
    [SerializeField] private bool ignorePunctuationAndSpaces = true;

    [Header("NPC Voice Database")]
    [Tooltip("Add all your NPCData ScriptableObjects here. Unmatched names use default fallback.")]
    [SerializeField] private List<NPCData> npcDatabase = new List<NPCData>();

    private readonly Dictionary<string, NPCData> npcLookup = new Dictionary<string, NPCData>(System.StringComparer.OrdinalIgnoreCase);
    private NPCData currentSpeaker;
    
    // Frame tracking to stop skip cacophony
    private int lastBlippedFrame = -1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }

        foreach (var npc in npcDatabase)
        {
            if (npc != null && !string.IsNullOrEmpty(npc.npcName))
            {
                npcLookup[npc.npcName] = npc;
            }
        }
    }

    public void SetSpeakerByName(string speakerName)
    {
        if (!string.IsNullOrEmpty(speakerName) && npcLookup.TryGetValue(speakerName, out var data))
        {
            currentSpeaker = data;
        }
        else
        {
            currentSpeaker = null;
        }
    }

    public void SetSpeaker(NPCData speakerData)
    {
        currentSpeaker = speakerData;
    }

    // --- Yarn Markup Handler Callbacks ---

    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text textComponent)
    {
        // Stop any leftover blip audio from previous line
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // 1. Check if the Character Name UI text was populated by Yarn
        if (characterNameText != null && !string.IsNullOrEmpty(characterNameText.text))
        {
            SetSpeakerByName(characterNameText.text);
            return;
        }

        // 2. Fallback: Parse "Name: Dialogue" directly from line text
        string fullText = line.Text;
        int colonIndex = fullText.IndexOf(':');
        if (colonIndex > 0 && colonIndex < 25)
        {
            string extractedName = fullText.Substring(0, colonIndex).Trim();
            SetSpeakerByName(extractedName);
            return;
        }

        // 3. Fallback to default settings
        currentSpeaker = null;
    }

    public override YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken)
    {
        // Prevent skip cacophony: Allow MAX ONE blip per frame!
        if (Time.frameCount == lastBlippedFrame)
        {
            return YarnTask.CompletedTask;
        }

        // Ignore spaces and punctuation marks
        if (ignorePunctuationAndSpaces && currentCharacterIndex < line.Text.Length)
        {
            char c = line.Text[currentCharacterIndex];
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c))
            {
                return YarnTask.CompletedTask;
            }
        }

        // Play audio on frequency intervals
        if (currentCharacterIndex % frequency == 0)
        {
            AudioClip clipToPlay = (currentSpeaker != null && currentSpeaker.typingSound != null)
                ? currentSpeaker.typingSound
                : defaultTextBlipClip;

            float lowPitch = (currentSpeaker != null) ? currentSpeaker.minPitch : defaultMinPitch;
            float highPitch = (currentSpeaker != null) ? currentSpeaker.maxPitch : defaultMaxPitch;

            if (audioSource != null && clipToPlay != null)
            {
                lastBlippedFrame = Time.frameCount; // Lock out other blips this frame
                audioSource.pitch = Random.Range(lowPitch, highPitch);
                audioSource.PlayOneShot(clipToPlay);
            }
        }

        return YarnTask.CompletedTask;
    }

    // --- Lifecycle Cleanup ---

    public override void OnLineDisplayComplete()
    {
        // Kill audio instantly when text finishes typing or gets skipped
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public override void OnLineWillDismiss()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text textComponent) { }
}