using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [Header("Progress Bar")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI percentText;
    //[SerializeField] private TextMeshProUGUI statusText;

    [Header("Settings")]
    [Tooltip("How fast the bar smooths toward the target value. Higher = snappier.")]
    [SerializeField] private float smoothSpeed = 5f;

    // ─── State ───
    private float targetProgress;
    private float displayedProgress;

    // Weighted progress ranges — auth takes the first 20%, data loading the rest
    private const float AUTH_WEIGHT = 0.20f;
    private const float DATA_WEIGHT = 0.80f;

    // ─── Lifecycle ───

    private void Awake()
    {
        ResetBar();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<AuthProgressEvent>(OnAuthProgress);
        EventBus.Subscribe<DataLoadProgressEvent>(OnDataLoadProgress);
        EventBus.Subscribe<DataLoadCompletedEvent>(OnDataLoadCompleted);
        EventBus.Subscribe<DataLoadFailedEvent>(OnDataLoadFailed);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<AuthProgressEvent>(OnAuthProgress);
        EventBus.Unsubscribe<DataLoadProgressEvent>(OnDataLoadProgress);
        EventBus.Unsubscribe<DataLoadCompletedEvent>(OnDataLoadCompleted);
        EventBus.Unsubscribe<DataLoadFailedEvent>(OnDataLoadFailed);
    }

    private void Update()
    {
        if (Mathf.Approximately(displayedProgress, targetProgress)) return;

        displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, smoothSpeed * Time.deltaTime);
        UpdateVisuals();
    }

    // ─── Event Handlers ───

    private void OnAuthProgress(AuthProgressEvent evt)
    {
        targetProgress = Mathf.Clamp01(evt.progress) * AUTH_WEIGHT;
        //SetStatus(evt.currentStep);
    }

    private void OnDataLoadProgress(DataLoadProgressEvent evt)
    {
        targetProgress = AUTH_WEIGHT + (Mathf.Clamp01(evt.progress) * DATA_WEIGHT);
        //SetStatus(evt.currentStep);
    }

    private void OnDataLoadCompleted(DataLoadCompletedEvent evt)
    {
        targetProgress = 1f;
        //SetStatus("Ready");
    }

    private void OnDataLoadFailed(DataLoadFailedEvent evt)
    {
        //SetStatus($"Error: {evt.errorMessage}");
    }

    // ─── Visuals ───

    private void UpdateVisuals()
    {
        if (fillImage != null)
            fillImage.fillAmount = displayedProgress;

        if (percentText != null)
            percentText.text = $"{Mathf.RoundToInt(displayedProgress * 100f)}%";
    }

    //private void SetStatus(string message)
    //{
    //    if (statusText != null) statusText.text = message;
    //}

    private void ResetBar()
    {
        targetProgress = 0f;
        displayedProgress = 0f;
        UpdateVisuals();
        //SetStatus("Loading...");
    }
}