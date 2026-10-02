using UnityEngine;

public class DialogueCamera : MonoBehaviour
{
    [Header("Target Tracking")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float defaultZoomSize = 5f;
    [SerializeField] private float dialogueZoomSize = 3.5f;
    [SerializeField] private float smoothTime = 0.25f;

    [Header("World Bounds")]
    [Tooltip("Drag a BoxCollider2D that covers your entire map background area.")]
    [SerializeField] private BoxCollider2D mapBounds;

    [Header("Dialogue Framing")]
    [Tooltip("Shifts the focus up slightly so speech bubbles fit above character heads.")]
    [SerializeField] private Vector3 dialogueOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Anticipation Juice Config")]
    [SerializeField] private float anticipationZoomBump = 0.8f;
    [SerializeField] private float anticipationDuration = 0.12f;

    private Camera cam;
    private float originalZoomSize;
    private Vector3 originalOffset;

    private Transform currentNPCTransform;
    private bool isDialogueActive = false;

    // SmoothDamp velocity trackers
    private Vector3 positionVelocity;
    private float zoomVelocity;

    private void Start()
    {
        cam = GetComponent<Camera>();
        
        originalZoomSize = defaultZoomSize; 
        cam.orthographicSize = defaultZoomSize;

        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        originalOffset = new Vector3(0f, 0f, transform.position.z);

        DialogueManager.OnDialogueOpened += ZoomInOnDialogue;
        DialogueManager.OnDialogueClosed += ZoomOutToNormal;
    }

    private void OnDestroy()
    {
        DialogueManager.OnDialogueOpened -= ZoomInOnDialogue;
        DialogueManager.OnDialogueClosed -= ZoomOutToNormal;
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        Vector3 targetPosition;
        float baseTargetZoom;

        // 1. Calculate Base Target Position & Base Zoom
        if (isDialogueActive && currentNPCTransform != null)
        {
            Vector3 midpoint = (playerTransform.position + currentNPCTransform.position) / 2f;
            targetPosition = new Vector3(midpoint.x, midpoint.y, transform.position.z) + dialogueOffset;
            baseTargetZoom = dialogueZoomSize;
        }
        else
        {
            targetPosition = playerTransform.position + originalOffset;
            baseTargetZoom = originalZoomSize;
        }

        // 2. Add Juice Zoom Offset (e.g., Anticipation Bump)
        float juiceZoomOffset = CameraJuice.Instance != null ? CameraJuice.Instance.GetZoomOffset() : 0f;
        float finalTargetZoom = baseTargetZoom + juiceZoomOffset;

        // Smoothly update camera zoom
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, finalTargetZoom, ref zoomVelocity, smoothTime);

        // 3. Clamp Base Position to Map Bounds
        if (mapBounds != null)
        {
            targetPosition = ClampTargetToMapBounds(targetPosition);
        }

        // 4. SmoothDamp position towards base target
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref positionVelocity, smoothTime);

        // 5. Apply Juice Position Offsets (Shake + Sway) on top of smoothed position
        Vector3 juicePosOffset = CameraJuice.Instance != null ? CameraJuice.Instance.GetPositionOffset() : Vector3.zero;
        transform.position = smoothedPosition + juicePosOffset;
    }

    private Vector3 ClampTargetToMapBounds(Vector3 rawTarget)
    {
        Bounds bounds = mapBounds.bounds;

        float camVertExtent = cam.orthographicSize;
        float camHorizExtent = cam.orthographicSize * cam.aspect;

        float minX = bounds.min.x + camHorizExtent;
        float maxX = bounds.max.x - camHorizExtent;
        float minY = bounds.min.y + camVertExtent;
        float maxY = bounds.max.y - camVertExtent;

        float clampedX = (minX > maxX) ? bounds.center.x : Mathf.Clamp(rawTarget.x, minX, maxX);
        float clampedY = (minY > maxY) ? bounds.center.y : Mathf.Clamp(rawTarget.y, minY, maxY);

        return new Vector3(clampedX, clampedY, rawTarget.z);
    }

    public void SnapToPlayer()
    {
        if (cam == null) cam = GetComponent<Camera>();

        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        if (playerTransform == null) return;

        originalOffset = new Vector3(0f, 0f, transform.position.z);
        positionVelocity = Vector3.zero;
        zoomVelocity = 0f;

        originalZoomSize = defaultZoomSize;
        cam.orthographicSize = defaultZoomSize;

        Vector3 targetPosition = playerTransform.position + originalOffset;

        if (mapBounds != null)
        {
            targetPosition = ClampTargetToMapBounds(targetPosition);
        }

        transform.position = targetPosition;
    }

    private void ZoomInOnDialogue()
    {
        isDialogueActive = true;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(playerTransform.position, 3f);
        foreach (var col in hitColliders)
        {
            if (col.CompareTag("NPC") && col.transform != playerTransform)
            {
                currentNPCTransform = col.transform;
                break;
            }
        }

        // Trigger anticipation kick through CameraJuice Manager
        if (CameraJuice.Instance != null)
        {
            CameraJuice.Instance.TriggerAnticipationBump(anticipationZoomBump, anticipationDuration);
        }
    }

    private void ZoomOutToNormal()
    {
        isDialogueActive = false;
        currentNPCTransform = null;
    }
}