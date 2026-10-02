using UnityEngine;
using Yarn.Unity;

public class SceneStartTrigger : MonoBehaviour
{
    [Header("Yarn Setup")]
    [Tooltip("The exact title of the Yarn node you want to start.")]
    [SerializeField] private string startNodeName = "Intro_Cutscene";

    [Header("Options")]
    [SerializeField] private bool triggerOnlyOnce = true;
    private const string PrefKey = "SceneTrigger_AlreadyFired_";

    private void Start()
    {
        if (triggerOnlyOnce && PlayerPrefs.GetInt(PrefKey + startNodeName, 0) == 1)
        {
            return; // Already played this intro before
        }

        // Find the active DialogueRunner in the scene and start the node
        DialogueRunner runner = FindFirstObjectByType<DialogueRunner>();
        if (runner != null)
        {
            if (runner.IsDialogueRunning)
            {
                Debug.LogWarning("[SceneStartTrigger] Dialogue is already running, delaying intro.");
                return;
            }

            runner.StartDialogue(startNodeName);

            if (triggerOnlyOnce)
            {
                PlayerPrefs.SetInt(PrefKey + startNodeName, 1);
                PlayerPrefs.Save();
            }
        }
        else
        {
            Debug.LogError("[SceneStartTrigger] No DialogueRunner found in the scene!");
        }
    }
}