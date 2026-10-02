using UnityEngine;

public class PauseMenuToggle : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag the container from your Scene Hierarchy (NOT from the Project Assets folder).")]
    [SerializeField] private GameObject pauseMenuContainer;

    [Header("Input Controls")]
    [SerializeField] private KeyCode menuKey = KeyCode.Tab;
    [SerializeField] private KeyCode alternativeKey = KeyCode.Escape;

    private void Awake()
    {
        EnsureLocalSceneReference();
    }

    private void Update()
    {
        if (Input.GetKeyDown(menuKey) || Input.GetKeyDown(alternativeKey))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        // 1. Safety check: Ensure we are targeting a scene instance, not a project prefab
        if (!EnsureLocalSceneReference())
        {
            Debug.LogError($"[PauseMenuToggle] Cannot toggle menu on {gameObject.name} because no valid Scene hierarchy object was found!", this);
            return;
        }

        // 2. Toggle active state
        bool newState = !pauseMenuContainer.activeSelf;
        pauseMenuContainer.SetActive(newState);

        // 3. Freeze/Unfreeze game time
        Time.timeScale = newState ? 0f : 1f;

        Debug.Log($"[PauseMenuToggle] Toggled '{pauseMenuContainer.name}' in scene '{pauseMenuContainer.scene.name}' to: {newState}", pauseMenuContainer);
    }

    /// <summary>
    /// Validates whether pauseMenuContainer points to an active scene object.
    /// If unassigned or pointing to a Project asset, it automatically finds the child container in this Canvas.
    /// </summary>
    private bool EnsureLocalSceneReference()
    {
        // .scene.IsValid() returns FALSE if the object is a Prefab Asset from the Project folder
        if (pauseMenuContainer != null && pauseMenuContainer.scene.IsValid())
        {
            return true; // Reference is valid and exists in the current scene
        }

        // Auto-assign: Search children of this SceneCanvas for a container object
        Transform childContainer = transform.Find("PauseMenuContainer"); // Replace string if your child object has a different name
        
        if (childContainer == null)
        {
            // Fallback: Find any child named "PauseMenu" or similar
            foreach (Transform child in transform)
            {
                if (child.name.ToLower().Contains("pause"))
                {
                    childContainer = child;
                    break;
                }
            }
        }

        if (childContainer != null)
        {
            pauseMenuContainer = childContainer.gameObject;
            Debug.LogWarning($"[PauseMenuToggle] Re-linked 'pauseMenuContainer' on {gameObject.name} to local scene object: {pauseMenuContainer.name}", pauseMenuContainer);
            return true;
        }

        return false;
    }
}