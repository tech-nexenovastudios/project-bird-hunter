using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Common reset operations every page can use in OnPageEnter.
/// Reduces boilerplate across all page implementations.
/// </summary>
public static class PageResetHelper
{
    /// <summary>Stops all DOTween animations on the transform and its children.</summary>
    public static void KillTweens(Transform root, bool complete = false)
    {
        if (root == null) return;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            DOTween.Kill(t, complete);
    }

    /// <summary>Resets all ScrollRects in this hierarchy to top position.</summary>
    public static void ResetScrolls(Transform root)
    {
        if (root == null) return;
        foreach (var sr in root.GetComponentsInChildren<ScrollRect>(true))
        {
            sr.verticalNormalizedPosition = 1f;
            //sr.horizontalNormalizedPosition = 0f;
        }
    }

    /// <summary>Hides all GameObjects whose name contains the pattern (case-sensitive).</summary>
    public static void HideByNameContains(Transform root, string namePattern)
    {
        if (root == null) return;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Contains(namePattern))
                t.gameObject.SetActive(false);
        }
    }

    /// <summary>Resets all TMP_InputFields in hierarchy to empty + non-interactable.</summary>
    public static void ResetInputFields(Transform root)
    {
        if (root == null) return;
        foreach (var inp in root.GetComponentsInChildren<TMP_InputField>(true))
        {
            inp.text = string.Empty;
            inp.interactable = false;
        }
    }

    /// <summary>Resets all CanvasGroup alphas to fully visible and interactable.</summary>
    public static void ResetCanvasGroups(Transform root)
    {
        if (root == null) return;
        foreach (var cg in root.GetComponentsInChildren<CanvasGroup>(true))
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }
}