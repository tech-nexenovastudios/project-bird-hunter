using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Central manager for cannon lock/unlock state.
/// Unlock = remove grayscale material from "Background" and "Cannon" children,
///          disable "LockBg", enable button.
/// </summary>
public class CannonLockManager : MonoBehaviour
{
    public static CannonLockManager Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private CannonHolder_SO cannonHolderSO;

    [Header("Grayscale")]
    [SerializeField] private Material grayscaleMaterial;

    private const string UNLOCK_KEY_PREFIX = "CannonUnlocked_";

    private bool[] unlockedState;

    /// <summary>Fires when a cannon is unlocked. int = cannon index.</summary>
    public static event Action<int> OnCannonUnlocked;

    /// <summary>Fires after all unlock states are loaded from cloud.</summary>
    public static event Action OnAllUnlockStatesLoaded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (cannonHolderSO != null)
        {
            unlockedState = new bool[cannonHolderSO.cannonsData.Length];
            unlockedState[0] = true; // First cannon always unlocked
        }
    }

    private void Start()
    {
        LoadUnlockState().Forget();
    }

    // ==================== Cloud Save ====================

    private string GetUnlockKey(int index)
    {
        return UNLOCK_KEY_PREFIX + cannonHolderSO.cannonsData[index].cannonName.Replace(" ", "");
    }

    private async UniTaskVoid LoadUnlockState()
    {
        if (cannonHolderSO == null) return;

        try
        {
            var keys = new HashSet<string>();
            for (int i = 0; i < cannonHolderSO.cannonsData.Length; i++)
                keys.Add(GetUnlockKey(i));

            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            for (int i = 0; i < cannonHolderSO.cannonsData.Length; i++)
            {
                string key = GetUnlockKey(i);
                if (data.ContainsKey(key))
                    unlockedState[i] = data[key].Value.GetAs<bool>();
            }

            unlockedState[0] = true;
            Debug.Log("[CannonLock] Loaded unlock states from cloud.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CannonLock] Failed to load: {ex.Message}");
        }

        // Notify all listeners
        OnAllUnlockStatesLoaded?.Invoke();

        for (int i = 0; i < unlockedState.Length; i++)
        {
            if (unlockedState[i])
                OnCannonUnlocked?.Invoke(i);
        }
    }

    // ==================== Visual Helpers ====================

    /// <summary>Apply locked visuals: grayscale on Background & Cannon, show LockIcon,
    /// hide LevelDesc & LevelCount, show LockedText.</summary>
    public void ApplyLockedVisual(Transform buttonTransform)
    {
        SetGrayscale(buttonTransform, "Background", true);
        SetGrayscale(buttonTransform, "Cannon", true);

        var lockBg = buttonTransform.Find("LockIcon");
        if (lockBg != null) lockBg.gameObject.SetActive(true);

        var background = buttonTransform.Find("Background");
        if (background != null)
        {
            SetChildActive(background, "LevelDesc", false);
            SetChildActive(background, "LockedText", true);
            SetChildActive(background, "LevelCount", false);
        }
    }

    /// <summary>Remove locked visuals: clear material on Background & Cannon, hide LockIcon,
    /// show LevelDesc & LevelCount, hide LockedText.</summary>
    public void ApplyUnlockedVisual(Transform buttonTransform)
    {
        SetGrayscale(buttonTransform, "Background", false);
        SetGrayscale(buttonTransform, "Cannon", false);

        var lockBg = buttonTransform.Find("LockIcon");
        if (lockBg != null) lockBg.gameObject.SetActive(false);

        var background = buttonTransform.Find("Background");
        if (background != null)
        {
            SetChildActive(background, "LevelDesc", true);
            SetChildActive(background, "LockedText", false);
            SetChildActive(background, "LevelCount", true);
        }
    }

    private void SetGrayscale(Transform parent, string childName, bool grey)
    {
        var child = parent.Find(childName);
        if (child == null) return;

        var img = child.GetComponent<Image>();
        if (img != null)
            img.material = grey ? grayscaleMaterial : null;
    }

    private void SetChildActive(Transform parent, string childName, bool active)
    {
        var child = parent.Find(childName);
        if (child != null) child.gameObject.SetActive(active);
    }

    // ==================== Public API ====================

    public bool IsUnlocked(int index)
    {
        if (index < 0 || index >= unlockedState.Length) return false;
        return unlockedState[index];
    }

    public Material GetGrayscaleMaterial() => grayscaleMaterial;

    /// <summary>Call this to unlock a cannon. Saves to cloud and notifies all panels.</summary>
    public void UnlockCannon(int index)
    {
        if (index < 0 || index >= unlockedState.Length) return;
        if (unlockedState[index]) return;

        unlockedState[index] = true;
        SaveUnlock(index).Forget();

        OnCannonUnlocked?.Invoke(index);
        Debug.Log($"[CannonLock] Cannon {index} unlocked.");
    }

    /// <summary>TODO: Fill with actual XP/coin requirements later.</summary>
    public bool CanUnlock(int index)
    {
        return true;
    }

    public void TryUnlockCannon(int index)
    {
        if (!CanUnlock(index)) return;
        UnlockCannon(index);
    }

    private async UniTaskVoid SaveUnlock(int index)
    {
        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(GetUnlockKey(index), true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CannonLock] Failed to save unlock: {ex.Message}");
        }
    }

    public int GetTotalCannons()
    {
        return cannonHolderSO != null ? cannonHolderSO.cannonsData.Length : 0;
    }
}