using UnityEngine;

using UnityEngine.UI;

public class ChapterEnvironmentApplier : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Scene References")]
    [Tooltip("The background GameObject's SpriteRenderer in the gameplay scene.")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("The platform GameObject's SpriteRenderer in the gameplay scene.")]
    [SerializeField] private SpriteRenderer platformRenderer;

    // ── Static Chapter Reference ─────────────────────────────────────────────
    // Set BEFORE loading the gameplay scene. Static fields live in code memory,
    // so they survive scene transitions without needing DontDestroyOnLoad.

    private static ChapterData _pendingChapterData;

    /// <summary>
    /// Call this before loading the gameplay scene.
    /// Pass the ChapterData asset for the chapter the player is about to play.
    /// 
    /// Example usage in your existing code:
    ///   var chapterData = chapterDataArray[currentChapterIndex];
    ///   ChapterEnvironmentApplier.SetChapterData(chapterData);
    ///   SceneManager.LoadScene("Gameplay");
    /// </summary>
    public static void SetChapterData(ChapterData data)
    {
        _pendingChapterData = data;
    }

    /// <summary>
    /// Returns the ChapterData that was set. Useful for other gameplay
    /// systems that also need to read from the current chapter's data.
    /// </summary>
    public static ChapterData GetCurrentChapterData() => _pendingChapterData;

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Start()
    {
        ApplyVisuals();
    }

    // ── Core Logic ───────────────────────────────────────────────────────────

    private void ApplyVisuals()
    {
        if (_pendingChapterData == null)
        {
            Debug.LogError("[ChapterEnv] No ChapterData was set before loading " +
                           "the gameplay scene! Call ChapterEnvironmentApplier" +
                           ".SetChapterData() before SceneManager.LoadScene().");
            return;
        }

        // ── Apply background ─────────────────────────────────────────────
        if (backgroundImage != null)
        {
            Sprite bg = _pendingChapterData.ChapterBackground;

            if (bg != null)
                backgroundImage.sprite = bg;
            else
                Debug.LogWarning($"[ChapterEnv] ChapterData '{_pendingChapterData.worldName}' " +
                                 "has no background sprite assigned.");
        }
        else
        {
            Debug.LogWarning("[ChapterEnv] backgroundRenderer is not assigned in Inspector.");
        }

        // ── Apply platform ───────────────────────────────────────────────
        if (platformRenderer != null)
        {
            Sprite plat = _pendingChapterData.Platform;

            if (plat != null)
                platformRenderer.sprite = plat;
            else
                Debug.LogWarning($"[ChapterEnv] ChapterData '{_pendingChapterData.worldName}' " +
                                 "has no platform sprite assigned.");
        }
        else
        {
            Debug.LogWarning("[ChapterEnv] platformRenderer is not assigned in Inspector.");
        }

        Debug.Log($"[ChapterEnv] Applied visuals for '{_pendingChapterData.worldName}'");
    }

    // ── Public API (for runtime chapter switching if ever needed) ─────────

    /// <summary>
    /// Switches visuals to a different chapter without reloading the scene.
    /// Useful if you ever implement chapter transitions within a single scene.
    /// </summary>
    public void SwitchToChapter(ChapterData newChapterData)
    {
        _pendingChapterData = newChapterData;
        ApplyVisuals();
    }
}