using UnityEngine;
using Yarn.Unity;

[RequireComponent(typeof(BoxCollider))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Yarn Setup")]
    [SerializeField] private string nodeName;

    [Header("Behavior")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered = false;

    private void Awake()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnlyOnce) return;

        if (other.CompareTag("Player"))
        {
            DialogueRunner runner = FindFirstObjectByType<DialogueRunner>();
            if (runner != null && !runner.IsDialogueRunning)
            {
                runner.StartDialogue(nodeName);
                hasTriggered = true;

                if (triggerOnlyOnce)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}