#if UNITY_EDITOR
using System.Collections;
using Gameplay.Events;
using Gameplay.Managers;
using UnityEngine;

// Editor-only test harness. Drop this on any GameObject in the gameplay scene
// (e.g. an empty "[Debug]" GO). Whenever a level finishes loading, it waits
// `delaySeconds` and then completes the level via the public manager API —
// no edits to gameplay code required. The whole class is wrapped in
// UNITY_EDITOR so it strips out of player builds.
[DisallowMultipleComponent]
public class LevelAutoCompleter : MonoBehaviour
{
    [Header("Auto-complete each level after a delay (Editor only)")]
    [SerializeField] private bool enableAutoComplete = true;
    [SerializeField, Min(0f)] private float delaySeconds = 2f;
    [SerializeField] private int fakeFinalScore = 1;

    private Coroutine pending;

    private void OnEnable()
    {
        // OnGameLevelUpdated fires from GameManager.StartGameplay on every
        // level start (including after CompleteCurrentLevel and after the
        // chapter spin). OnLevelLoaded would only fire on the very first
        // level — see GameProgressManager.LoadLevelProfile.
        GameEvents.OnGameLevelUpdated += HandleLevelStart;
    }

    private void OnDisable()
    {
        GameEvents.OnGameLevelUpdated -= HandleLevelStart;
        CancelPending();
    }

    private void HandleLevelStart(int levelIndex)
    {
        if (!enableAutoComplete) return;
        CancelPending();
        pending = StartCoroutine(CompleteAfterDelay());
    }

    private IEnumerator CompleteAfterDelay()
    {
        yield return new WaitForSeconds(delaySeconds);

        if (!enableAutoComplete) yield break;
        if (GameManager.Instance == null) yield break;
        if (GameManager.Instance.state != GameState.Gameplay) yield break;

        int ch = GameProgressManager.Instance != null ? GameProgressManager.Instance.CurrentChapter : 0;
        int lv = GameProgressManager.Instance != null ? GameProgressManager.Instance.CurrentLevel : 0;
        Debug.Log($"[LevelAutoCompleter] Auto-completing Ch{ch} L{lv} after {delaySeconds}s (score={fakeFinalScore})");
        GameManager.Instance.CompleteCurrentLevel(fakeFinalScore);
    }

    private void CancelPending()
    {
        if (pending != null)
        {
            StopCoroutine(pending);
            pending = null;
        }
    }
}
#endif
