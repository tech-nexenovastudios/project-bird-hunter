using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Manages user profile UI: username edit with validation, avatar selection.
/// Reads from UserData (already loaded by CloudDatabase at boot).
/// Saves through UserDataRepository (one save = full UserData persisted).
///
/// Avatar behaviour:
///   - Selecting an avatar previews it on profileImage only.
///   - profileButtonImage only updates after the player clicks Save.
///   - If the panel is closed without saving, profileImage reverts to the
///     last saved avatar (matching profileButtonImage).
/// Avatar panel behaviour:
///   - avatarSelectionPanel is hidden by default when the profile panel opens.
///   - It opens when the player clicks the Edit button.
///   - It closes when the panel is closed (OnPanelClosed) or Save is clicked.
/// </summary>
public class UserProfileDataManager : MonoBehaviour
{
    // ───────────────────────── Inspector References ─────────────────────────

    [Header("Username UI")]
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TextMeshProUGUI playerIdText;
    [SerializeField] private Button editButton;
    [SerializeField] private TextMeshProUGUI validationText;

    [Header("Validation Animation")]
    [SerializeField] private float validationHoldDuration = 1.5f;
    [SerializeField] private float validationFadeDuration = 0.6f;
    [SerializeField] private float validationFloatDistance = 40f;

    [Header("Avatar UI")]
    [SerializeField] private Image profileImage;           // Preview target — changes on selection
    [SerializeField] private Image profileButtonImage;     // Persistent icon — only changes on Save
    [SerializeField] private List<Button> avatarButtons;
    [SerializeField] private List<Sprite> avatarSprites;
    [SerializeField] private Button saveButton;
    [SerializeField] private GameObject avatarSelectionPanel; // Panel that shows avatar grid
    [SerializeField] private GameObject statsPanel;           // Default panel shown when profile opens

    [Header("Avatar Visual States")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.5f);

    // ───────────────────────── Constants ─────────────────────────

    private const int MIN_USERNAME_LENGTH = 7;
    private const int MAX_USERNAME_LENGTH = 15;

    // ───────────────────────── Services ─────────────────────────

    private UserDataRepository userDataRepo;
    private AuthService authService;

    // ───────────────────────── Runtime State ─────────────────────────

    private int previewedAvatarIndex = -1;   // What is currently shown on profileImage
    private bool isEditing = false;
    private bool isSaving = false;

    private CancellationTokenSource cts;

    // Validation animation state
    private CancellationTokenSource validationAnimCts;
    private CanvasGroup validationCanvasGroup;
    private RectTransform validationRect;
    private Vector2 validationOriginalAnchoredPos;

    // ═══════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        cts = new CancellationTokenSource();

        if (usernameInputField != null)
            usernameInputField.interactable = false;

        InitValidationAnimation();
        ClearValidation();

        // Avatar selection panel is hidden until Edit is pressed
        SetAvatarPanelVisible(false);

        if (editButton != null)
            editButton.onClick.AddListener(OnEditPressed);

        if (saveButton != null)
            saveButton.onClick.AddListener(OnSavePressed);

        for (int i = 0; i < avatarButtons.Count; i++)
        {
            int index = i;
            if (avatarButtons[i] != null)
                avatarButtons[i].onClick.AddListener(() => PreviewAvatar(index));
        }
    }

    private void Start()
    {
        userDataRepo = ServiceLocator.Get<UserDataRepository>();
        authService = ServiceLocator.Get<AuthService>();

        if (userDataRepo == null || userDataRepo.Data == null)
        {
            Debug.LogError("[UserProfile] UserDataRepository unavailable. Profile UI will not work.");
            return;
        }

        InitializeFromUserDataAsync(cts.Token).Forget();
    }

    private void OnEnable()
    {
        // Every time the panel is shown, reset the avatar selection state
        // so unsaved previews from a previous session are discarded.
        ResetAvatarPreviewToSaved();
        SetAvatarPanelVisible(false);

        if (usernameInputField != null)
        {
            usernameInputField.onSubmit.AddListener(OnInputFinished);
            usernameInputField.onDeselect.AddListener(OnInputFinished);
        }
    }

    private void OnDisable()
    {
        // Panel is closing — revert any unsaved avatar preview
        RevertProfileImageToSaved();
        SetAvatarPanelVisible(false);

        if (usernameInputField != null)
        {
            usernameInputField.onSubmit.RemoveListener(OnInputFinished);
            usernameInputField.onDeselect.RemoveListener(OnInputFinished);
        }
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
        validationAnimCts?.Cancel();
        validationAnimCts?.Dispose();

        if (editButton != null) editButton.onClick.RemoveListener(OnEditPressed);
        if (saveButton != null) saveButton.onClick.RemoveListener(OnSavePressed);

        for (int i = 0; i < avatarButtons.Count; i++)
        {
            if (avatarButtons[i] != null)
                avatarButtons[i].onClick.RemoveAllListeners();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  INITIALIZE FROM CACHED USER DATA
    // ═══════════════════════════════════════════════════════════════

    private async UniTaskVoid InitializeFromUserDataAsync(CancellationToken token)
    {
        try
        {
            var data = userDataRepo.Data;

            // Player ID
            if (playerIdText != null && authService != null)
                playerIdText.text = authService.PlayerId ?? "Not signed in";

            // Username — generate if missing (first-time user)
            if (string.IsNullOrEmpty(data.username))
            {
                string hint = authService?.AuthDisplayHint;
                data.username = GenerateUsernameFromHint(hint);
                await userDataRepo.SaveAsync();
                Debug.Log($"[UserProfile] Generated initial username: {data.username}");
            }

            token.ThrowIfCancellationRequested();

            SetInputFieldText(data.username);

            // Avatar — clamp index if out of range
            int clampedIndex = Mathf.Clamp(data.avatarIndex, 0, avatarSprites.Count - 1);
            if (clampedIndex != data.avatarIndex)
            {
                data.avatarIndex = clampedIndex;
                await userDataRepo.SaveAsync();
            }

            // Sync both images to the saved avatar
            previewedAvatarIndex = data.avatarIndex;
            ApplyAvatarToProfileImage(data.avatarIndex);
            ApplyAvatarToButtonImage(data.avatarIndex);
            HighlightAvatar(data.avatarIndex);

            Debug.Log($"[UserProfile] Loaded — Username: {data.username}, Avatar: {data.avatarIndex}");
        }
        catch (System.OperationCanceledException) { }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] Initialization failed: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  USERNAME EDITING
    // ═══════════════════════════════════════════════════════════════

    private void OnEditPressed()
    {
        // Open the avatar selection panel
        //SetAvatarPanelVisible(true);

        // Also unlock the username field for editing
        if (usernameInputField == null) return;

        isEditing = true;
        ClearValidation();

        usernameInputField.characterLimit = MAX_USERNAME_LENGTH;
        usernameInputField.interactable = true;
        usernameInputField.Select();
        usernameInputField.ActivateInputField();
        usernameInputField.caretPosition = usernameInputField.text.Length;
    }

    private void OnInputFinished(string input)
    {
        if (!isEditing || isSaving) return;
        isSaving = true;
        ValidateAndSaveUsername(input, cts.Token).Forget();
    }

    private async UniTaskVoid ValidateAndSaveUsername(string rawInput, CancellationToken token)
    {
        try
        {
            string newUsername = rawInput?.Trim() ?? string.Empty;
            string currentUsername = userDataRepo.Data.username;

            if (!IsValidUsername(newUsername))
            {
                ShowValidation($"Min {MIN_USERNAME_LENGTH} alphanumeric characters, no spaces or symbols");
                SetInputFieldText(currentUsername);
                LockInput();
                return;
            }

            if (newUsername == currentUsername)
            {
                ClearValidation();
                LockInput();
                return;
            }

            usernameInputField.interactable = false;

            userDataRepo.Data.username = newUsername;
            await userDataRepo.SaveAsync();

            token.ThrowIfCancellationRequested();

            EventBus.Publish(new UsernameChangedEvent { newUsername = newUsername });
            ClearValidation();
            Debug.Log($"[UserProfile] Username saved: {newUsername}");
        }
        catch (System.OperationCanceledException) { }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] Failed to save username: {ex.Message}");
            ShowValidation("Save failed — please try again");
        }
        finally
        {
            SetInputFieldText(userDataRepo.Data.username);
            LockInput();
            isSaving = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  AVATAR
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when the player taps an avatar button.
    /// Updates profileImage immediately as a preview.
    /// profileButtonImage is NOT changed until the player saves.
    /// </summary>
    private void PreviewAvatar(int index)
    {
        if (index < 0 || index >= avatarSprites.Count) return;
        if (previewedAvatarIndex == index) return;

        previewedAvatarIndex = index;

        // Show the preview only on profileImage
        ApplyAvatarToProfileImage(index);
        HighlightAvatar(index);
    }

    /// <summary>Updates only the main profile image (preview target).</summary>
    private void ApplyAvatarToProfileImage(int index)
    {
        if (index < 0 || index >= avatarSprites.Count) return;
        if (profileImage != null)
            profileImage.sprite = avatarSprites[index];
    }

    /// <summary>Updates only the profile button image (confirmed/saved target).</summary>
    private void ApplyAvatarToButtonImage(int index)
    {
        if (index < 0 || index >= avatarSprites.Count) return;
        if (profileButtonImage != null)
            profileButtonImage.sprite = avatarSprites[index];
    }

    private void HighlightAvatar(int selectedIndex)
    {
        for (int i = 0; i < avatarButtons.Count; i++)
        {
            if (avatarButtons[i] == null) continue;
            var img = avatarButtons[i].GetComponent<Image>();
            if (img != null)
                img.color = (i == selectedIndex) ? selectedColor : normalColor;
        }
    }

    private void OnSavePressed()
    {
        SaveAvatarSelection(cts.Token).Forget();
    }

    private async UniTaskVoid SaveAvatarSelection(CancellationToken token)
    {
        var data = userDataRepo.Data;

        if (previewedAvatarIndex == data.avatarIndex)
        {
            Debug.Log("[UserProfile] No avatar change to save.");
            // Still close the avatar panel on save
            SetAvatarPanelVisible(false);
            return;
        }

        try
        {
            data.avatarIndex = previewedAvatarIndex;
            await userDataRepo.SaveAsync();

            token.ThrowIfCancellationRequested();

            // Now that it's confirmed, update the button image too
            ApplyAvatarToButtonImage(data.avatarIndex);

            // Close the avatar selection panel after saving
            SetAvatarPanelVisible(false);

            EventBus.Publish(new AvatarChangedEvent { newAvatarIndex = data.avatarIndex });
            Debug.Log($"[UserProfile] Avatar saved: {data.avatarIndex}");
        }
        catch (System.OperationCanceledException) { }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] Failed to save avatar: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  AVATAR PANEL & REVERT HELPERS
    // ═══════════════════════════════════════════════════════════════

    private void SetAvatarPanelVisible(bool visible)
    {
        if (avatarSelectionPanel != null)
            avatarSelectionPanel.SetActive(visible);

        // Stats panel is the inverse — visible when avatar panel is not
        if (statsPanel != null)
            statsPanel.SetActive(!visible);
    }

    /// <summary>
    /// Reverts profileImage to the last saved avatar.
    /// Call this when the profile panel closes without the player having saved.
    /// </summary>
    private void RevertProfileImageToSaved()
    {
        if (userDataRepo?.Data == null) return;

        int savedIndex = userDataRepo.Data.avatarIndex;
        previewedAvatarIndex = savedIndex;
        ApplyAvatarToProfileImage(savedIndex);
        HighlightAvatar(savedIndex);
    }

    /// <summary>
    /// Called in OnEnable to guarantee a clean state every time the panel opens.
    /// </summary>
    private void ResetAvatarPreviewToSaved()
    {
        if (userDataRepo?.Data == null) return;
        RevertProfileImageToSaved();
    }

    // ═══════════════════════════════════════════════════════════════
    //  VALIDATION (animation unchanged)
    // ═══════════════════════════════════════════════════════════════

    private bool IsValidUsername(string username)
    {
        if (string.IsNullOrEmpty(username) ||
            username.Length < MIN_USERNAME_LENGTH ||
            username.Length > MAX_USERNAME_LENGTH)
            return false;

        for (int i = 0; i < username.Length; i++)
        {
            if (!char.IsLetterOrDigit(username[i]))
                return false;
        }
        return true;
    }

    private void InitValidationAnimation()
    {
        if (validationText == null) return;
        validationRect = validationText.GetComponent<RectTransform>();
        validationOriginalAnchoredPos = validationRect.anchoredPosition;
        validationCanvasGroup = validationText.GetComponent<CanvasGroup>();
        if (validationCanvasGroup == null)
            validationCanvasGroup = validationText.gameObject.AddComponent<CanvasGroup>();
    }

    private void ShowValidation(string message)
    {
        if (validationText == null) return;

        validationAnimCts?.Cancel();
        validationAnimCts?.Dispose();
        validationAnimCts = new CancellationTokenSource();

        validationRect.anchoredPosition = validationOriginalAnchoredPos;
        validationCanvasGroup.alpha = 1f;
        validationText.text = message;
        validationText.gameObject.SetActive(true);

        AnimateValidationOut(validationAnimCts.Token).Forget();
    }

    private void ClearValidation()
    {
        validationAnimCts?.Cancel();
        validationAnimCts?.Dispose();
        validationAnimCts = null;

        if (validationText != null)
        {
            validationText.text = string.Empty;
            validationText.gameObject.SetActive(false);
            if (validationRect != null)
                validationRect.anchoredPosition = validationOriginalAnchoredPos;
            if (validationCanvasGroup != null)
                validationCanvasGroup.alpha = 1f;
        }
    }

    private async UniTaskVoid AnimateValidationOut(CancellationToken token)
    {
        try
        {
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(validationHoldDuration),
                cancellationToken: token
            );

            Vector2 startPos = validationOriginalAnchoredPos;
            Vector2 endPos = startPos + new Vector2(0f, validationFloatDistance);
            float elapsed = 0f;

            while (elapsed < validationFadeDuration)
            {
                token.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / validationFadeDuration);
                float eased = 1f - (1f - t) * (1f - t);

                validationRect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
                validationCanvasGroup.alpha = 1f - eased;

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            validationText.gameObject.SetActive(false);
            validationRect.anchoredPosition = validationOriginalAnchoredPos;
            validationCanvasGroup.alpha = 1f;
        }
        catch (System.OperationCanceledException) { }
    }

    // ═══════════════════════════════════════════════════════════════
    //  USERNAME GENERATION
    // ═══════════════════════════════════════════════════════════════

    private string GenerateUsernameFromHint(string hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return GenerateFallbackUsername();

        string firstName = hint;

        int atIndex = firstName.IndexOf('@');
        if (atIndex > 0) firstName = firstName.Substring(0, atIndex);

        int spaceIndex = firstName.IndexOf(' ');
        if (spaceIndex > 0) firstName = firstName.Substring(0, spaceIndex);

        var cleaned = new System.Text.StringBuilder(firstName.Length);
        for (int i = 0; i < firstName.Length; i++)
        {
            if (char.IsLetterOrDigit(firstName[i]))
                cleaned.Append(firstName[i]);
        }

        if (cleaned.Length == 0) return GenerateFallbackUsername();

        string baseName = char.ToUpper(cleaned[0]) + cleaned.ToString(1, cleaned.Length - 1).ToLower();
        string digits = Random.Range(100, 999).ToString();
        string result = baseName + digits;

        if (result.Length < MIN_USERNAME_LENGTH)
        {
            const string letters = "abcdefghijklmnopqrstuvwxyz";
            var padded = new System.Text.StringBuilder(result);
            while (padded.Length < MIN_USERNAME_LENGTH)
                padded.Insert(padded.Length - 3, letters[Random.Range(0, letters.Length)]);
            result = padded.ToString();
        }

        return result;
    }

    private string GenerateFallbackUsername()
    {
        const string letters = "abcdefghijklmnopqrstuvwxyz";
        var sb = new System.Text.StringBuilder("Player");
        sb.Append(letters[Random.Range(0, letters.Length)]);
        sb.Append(Random.Range(100, 999));
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  HELPERS & PUBLIC API
    // ═══════════════════════════════════════════════════════════════

    private void LockInput()
    {
        isEditing = false;
        if (usernameInputField != null)
            usernameInputField.interactable = false;
    }

    private void SetInputFieldText(string text)
    {
        if (usernameInputField != null)
        {
            usernameInputField.text = text;
            usernameInputField.ForceLabelUpdate();
        }
    }

    public string GetUsername() => userDataRepo?.Data?.username ?? "";
    public int GetAvatarIndex() => userDataRepo?.Data?.avatarIndex ?? 0;

    public Sprite GetAvatarSprite()
    {
        int idx = GetAvatarIndex();
        return (idx >= 0 && idx < avatarSprites.Count) ? avatarSprites[idx] : null;
    }

    public string GetPlayerId() => authService?.PlayerId ?? "Not signed in";

    /// <summary>
    /// Call this from your close-panel button instead of just disabling the GameObject,
    /// if you need explicit control (e.g. animated panels). OnDisable handles the same
    /// logic automatically when SetActive(false) is used.
    /// </summary>
    public void OnPanelClosed()
    {
        RevertProfileImageToSaved();
        SetAvatarPanelVisible(false);
    }
}
