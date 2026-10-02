using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaypointIndicator : MonoBehaviour
{
    public static WaypointIndicator Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private RectTransform containerRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;
    [SerializeField] private RectTransform arrowPointer;
    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Screen Edge Clamping")]
    [SerializeField] private float edgePadding = 50f;
    [SerializeField] private bool showDistance = true;

    [Header("Target Tracking")]
    [SerializeField] private Transform playerTransform;

    private WaypointTarget currentTarget;
    private Camera mainCam;

    private static readonly List<WaypointTarget> activeTargets = new List<WaypointTarget>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        mainCam = Camera.main;

        if (containerRect == null || containerRect.gameObject == gameObject)
        {
            Transform childContainer = transform.Find("Container");
            if (childContainer != null) containerRect = childContainer.GetComponent<RectTransform>();
            
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        QuestManager.OnQuestsUpdated += EvaluateActiveTarget;
        QuestManager.OnTrackedQuestChanged += HandleTrackedQuestChanged;
        EvaluateActiveTarget();
    }

    private void OnDisable()
    {
        QuestManager.OnQuestsUpdated -= EvaluateActiveTarget;
        QuestManager.OnTrackedQuestChanged -= HandleTrackedQuestChanged;
    }

    private void HandleTrackedQuestChanged(Quest trackedQuest)
    {
        EvaluateActiveTarget();
    }

    private void Update()
    {
        if (mainCam == null || !mainCam.gameObject.activeInHierarchy)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        Quest tracked = QuestManager.Instance != null ? QuestManager.Instance.TrackedQuest : null;

        if (currentTarget == null || !currentTarget.enabled || tracked == null || !currentTarget.IsValidForTrackedQuest(tracked))
        {
            SetIndicatorVisible(false);
            return;
        }

        SetIndicatorVisible(true);
        UpdatePositionAndPointer();
    }

    public static void RegisterTarget(WaypointTarget target)
    {
        if (target == null) return;
        if (!activeTargets.Contains(target)) activeTargets.Add(target);

        if (Instance != null) Instance.EvaluateActiveTarget();
    }

    public static void UnregisterTarget(WaypointTarget target)
    {
        activeTargets.Remove(target);
        if (Instance != null) Instance.EvaluateActiveTarget();
    }

    public void EvaluateActiveTarget()
    {
        activeTargets.RemoveAll(t => t == null);

        if (QuestManager.Instance == null || QuestManager.Instance.TrackedQuest == null)
        {
            SetTarget(null);
            return;
        }

        Quest currentTrackedQuest = QuestManager.Instance.TrackedQuest;

        // 1. Prefer specific objective / NPC target first
        WaypointTarget bestTarget = activeTargets.Find(t => 
            t != null && t.enabled && t.IsValidForTrackedQuest(currentTrackedQuest) && t.waypointType != WaypointType.SceneDoor
        );

        // 2. Fall back to door/transition target if the objective isn't in this scene
        if (bestTarget == null)
        {
            bestTarget = activeTargets.Find(t => 
                t != null && t.enabled && t.IsValidForTrackedQuest(currentTrackedQuest) && t.waypointType == WaypointType.SceneDoor
            );
        }

        SetTarget(bestTarget);
    }

    public void SetTarget(WaypointTarget target)
    {
        currentTarget = target;

        if (currentTarget == null)
        {
            SetIndicatorVisible(false);
            return;
        }

        if (iconImage != null && currentTarget.customIcon != null)
        {
            iconImage.sprite = currentTarget.customIcon;
        }
    }

    private void UpdatePositionAndPointer()
    {
        if (currentTarget == null || mainCam == null) return;

        Vector3 targetWorldPos = currentTarget.GetWorldPosition();
        Vector3 screenPos = mainCam.WorldToScreenPoint(targetWorldPos);

        bool isBehindCamera = screenPos.z < 0;
        if (isBehindCamera)
        {
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        bool isOffScreen = isBehindCamera ||
                           screenPos.x < edgePadding ||
                           screenPos.x > Screen.width - edgePadding ||
                           screenPos.y < edgePadding ||
                           screenPos.y > Screen.height - edgePadding;

        Vector3 clampedScreenPos = screenPos;
        clampedScreenPos.x = Mathf.Clamp(clampedScreenPos.x, edgePadding, Screen.width - edgePadding);
        clampedScreenPos.y = Mathf.Clamp(clampedScreenPos.y, edgePadding, Screen.height - edgePadding);
        clampedScreenPos.z = 0;

        if (containerRect != null) containerRect.position = clampedScreenPos;

        if (arrowPointer != null)
        {
            if (isOffScreen)
            {
                arrowPointer.gameObject.SetActive(true);
                Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
                Vector3 direction = (clampedScreenPos - screenCenter).normalized;

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                arrowPointer.rotation = Quaternion.Euler(0, 0, angle + 90f);
            }
            else
            {
                arrowPointer.gameObject.SetActive(false);
            }
        }

        if (showDistance && distanceText != null && playerTransform != null)
        {
            float dist = Vector2.Distance(playerTransform.position, targetWorldPos);
            distanceText.text = $"{Mathf.RoundToInt(dist)}m";
        }
    }

    private void SetIndicatorVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}