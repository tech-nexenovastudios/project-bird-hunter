//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// Self-contained component attached to every cannon button in the bottom grid.
/////
///// VISUAL STATES
///// ─────────────
/////   Unlocked + Not Selected  → normalBackground active,   normalFrame active,
/////                              selectedBackground inactive, selectedFrame inactive,
/////                              grayscale OFF, lockIcon OFF
/////
/////   Unlocked + Selected      → normalBackground inactive,  normalFrame inactive,
/////                              selectedBackground active,  selectedFrame active,
/////                              grayscale OFF, lockIcon OFF
/////
/////   Locked                   → normalBackground active,   normalFrame active,
/////                              selectedBackground inactive, selectedFrame inactive,
/////                              grayscale ON,  lockIcon ON
/////
///// HIERARCHY EXPECTATION (one possible setup — names don't matter, just wire refs)
///// ────────────────────────────────────────────────────────────────────────────────
/////   CannonButton (this script + Button)
/////     ├─ NormalBackground      (Image)
/////     ├─ SelectedBackground    (Image)
/////     ├─ NormalFrame           (Image)
/////     ├─ SelectedFrame         (Image)
/////     ├─ CannonSprite          (Image)   ← grid thumbnail
/////     ├─ LockIcon              (GameObject)
/////     └─ LevelText             (TextMeshProUGUI)
///// </summary>
//public class CannonItemUI : MonoBehaviour
//{
//    // ── Inspector ─────────────────────────────────────────────────────────────

//    [Header("Button")]
//    [Tooltip("The Button component on this GameObject (or a child).")]
//    public Button button;

//    [Header("Backgrounds (swap on select)")]
//    [Tooltip("Shown when unlocked & NOT selected.")]
//    public GameObject normalBackground;

//    [Tooltip("Shown when unlocked & selected.")]
//    public GameObject selectedBackground;

//    [Header("Frames (swap on select)")]
//    [Tooltip("Shown when unlocked & NOT selected.")]
//    public GameObject normalFrame;

//    [Tooltip("Shown when unlocked & selected.")]
//    public GameObject selectedFrame;

//    [Header("Cannon Visual")]
//    [Tooltip("Image displaying the cannon thumbnail — sprite is pre-assigned in Inspector, not set via code.")]
//    public Image cannonImage;

//    [Tooltip("Grayscale material applied to cannonImage when locked.")]
//    public Material grayscaleMaterial;

//    [Header("Lock")]
//    [Tooltip("Root GameObject containing the lock icon — toggled on/off.")]
//    public GameObject lockIcon;

//    //[Header("Level Text")]
//    //public TextMeshProUGUI levelText;

//    // ── Runtime ───────────────────────────────────────────────────────────────

//    /// <summary>Cannon index this item represents — set once by CannonSelectionManager.</summary>
//    [HideInInspector] public int cannonIndex = -1;

//    // ═════════════════════════════════════════════════════════════════════════
//    // Public State API
//    // ═════════════════════════════════════════════════════════════════════════

//    public enum State
//    {
//        UnlockedNotSelected,
//        UnlockedSelected,
//        Locked
//    }

//    /// <summary>
//    /// Applies the correct visuals for the given state.
//    /// Call this any time lock status or selection changes.
//    /// </summary>
//    public void SetState(State state)
//    {
//        switch (state)
//        {
//            case State.UnlockedNotSelected:
//                SetBackgrounds(normalActive: true);
//                SetFrames(normalActive: true);
//                SetGrayscale(false);
//                SetLockIcon(false);
//                if (button != null) button.interactable = true;
//                break;

//            case State.UnlockedSelected:
//                SetBackgrounds(normalActive: false);
//                SetFrames(normalActive: false);
//                SetGrayscale(false);
//                SetLockIcon(false);
//                if (button != null) button.interactable = true;
//                break;

//            case State.Locked:
//                SetBackgrounds(normalActive: true);
//                SetFrames(normalActive: true);
//                SetGrayscale(true);
//                SetLockIcon(true);
//                if (button != null) button.interactable = true; // still tappable to show info
//                break;
//        }
//    }

//    // ── Convenience Wrapper ───────────────────────────────────────────────────

//    /// <summary>Updates the level badge text.</summary>
//    //public void SetLevel(int level)
//    //{
//    //    if (levelText != null)
//    //        levelText.text = level.ToString();
//    //}

//    // ═════════════════════════════════════════════════════════════════════════
//    // Private Helpers
//    // ═════════════════════════════════════════════════════════════════════════

//    private void SetBackgrounds(bool normalActive)
//    {
//        if (normalBackground != null) normalBackground.SetActive(normalActive);
//        if (selectedBackground != null) selectedBackground.SetActive(!normalActive);
//    }

//    private void SetFrames(bool normalActive)
//    {
//        if (normalFrame != null) normalFrame.SetActive(normalActive);
//        if (selectedFrame != null) selectedFrame.SetActive(!normalActive);
//    }

//    private void SetGrayscale(bool grey)
//    {
//        if (cannonImage == null) return;
//        cannonImage.material = grey ? grayscaleMaterial : null;
//    }

//    private void SetLockIcon(bool visible)
//    {
//        if (lockIcon != null) lockIcon.SetActive(visible);
//    }
//}
