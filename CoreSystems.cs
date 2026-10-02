using UnityEngine;

public class CoreSystems : MonoBehaviour
{
    public static CoreSystems Instance { get; private set; }

    private void Awake()
    {
        // 1. Prevent duplicates if a CoreSystems instance already exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 2. Set this instance as the global singleton
        Instance = this;

        // 3. Detach from any parent (DontDestroyOnLoad only works on root GameObjects)
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        // 4. Mark the object to persist across scene changes
        DontDestroyOnLoad(gameObject);
    }
}