using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Manages user profile: username (editable with validation), player ID (read-only),
/// and avatar selection — all persisted via Cloud Save.
///
/// USERNAME RULES:
///   • First-time: derived from auth display name/email + 3 random digits
///   • Validation : minimum 7 characters, alphanumeric only (a-z A-Z 0-9), no spaces/specials
///
/// EDIT FLOW (mobile-friendly):
///   1. User taps Edit button  → input field unlocks, keyboard opens
///   2. User types new name    → live validation feedback (optional)
///   3. User presses Enter OR taps elsewhere → validate → save if valid, revert if not
///
/// AVATAR FLOW:
///   1. Tap an avatar button to preview it
///   2. Tap Save to persist the selection to Cloud Save
/// </summary>
public class UserProfileDataManager : MonoBehaviour
{
    // ───────────────────────── Inspector References ─────────────────────────

    [Header("Username UI")]
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TextMeshProUGUI playerIdText;
    [SerializeField] private Button editButton;
    [SerializeField] private TextMeshProUGUI validationText;      // Shows error like "Min 7 alphanumeric chars"

    [Header("Validation Animation")]
    [Tooltip("How long the message stays fully visible before animating out.")]
    [SerializeField] private float validationHoldDuration = 1.5f;
    [Tooltip("Duration of the fade-up exit animation.")]
    [SerializeField] private float validationFadeDuration = 0.6f;
    [Tooltip("How far (in pixels) the text floats upward while fading.")]
    [SerializeField] private float validationFloatDistance = 40f;

    [Header("Avatar UI")]
    [SerializeField] private Image profileImage;                   // Main profile display
    [SerializeField] private Image profileButtonImage;             // Button icon that also shows avatar
    [SerializeField] private List<Button> avatarButtons;            // Avatar selection buttons in ProfilePanel
    [SerializeField] private List<Sprite> avatarSprites;           // Matching sprites (same order as buttons)
    [SerializeField] private Button saveButton;                    // Saves avatar (and any unsaved username)

    [Header("Avatar Visual States")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.5f);

    // ───────────────────────── Constants ─────────────────────────

    private const string USERNAME_KEY = "username";
    private const string AVATAR_KEY = "avatarIndex";
    private const int MIN_USERNAME_LENGTH = 7;

    // ───────────────────────── Runtime State ─────────────────────────

    private string currentUsername;
    private int savedAvatarIndex = 0;
    private int previewedAvatarIndex = -1;

    private bool isEditing = false;
    private bool isSaving = false;   // Prevents double-fire from onSubmit + onDeselect racing

    private CancellationTokenSource cts;

    // Validation animation state
    private CancellationTokenSource validationAnimCts;  // Separate token so new messages cancel old animation
    private CanvasGroup validationCanvasGroup;           // Added at runtime for alpha control
    private RectTransform validationRect;                // Cached for position animation
    private Vector2 validationOriginalAnchoredPos;       // Home position to reset to each time

    // ═══════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        cts = new CancellationTokenSource();

        // Start locked
        if (usernameInputField != null)
            usernameInputField.interactable = false;

        // Prepare validation animation components
        InitValidationAnimation();
        ClearValidation();

        // Wire buttons once — cleaned up in OnDestroy
        if (editButton != null)
            editButton.onClick.AddListener(OnEditPressed);

        if (saveButton != null)
            saveButton.onClick.AddListener(OnSavePressed);

        for (int i = 0; i < avatarButtons.Count; i++)
        {
            int index = i;   // Closure capture
            if (avatarButtons[i] != null)
                avatarButtons[i].onClick.AddListener(() => PreviewAvatar(index));
        }
    }

    private void Start()
    {
        LoadProfile(cts.Token).Forget();
    }

    /// <summary>
    /// Subscribe to input events every time this object is enabled.
    /// Using OnEnable/OnDisable keeps listeners scoped to active lifetime
    /// and avoids stacking if the GameObject is toggled.
    /// </summary>
    private void OnEnable()
    {
        if (usernameInputField != null)
        {
            usernameInputField.onSubmit.AddListener(OnInputFinished);
            usernameInputField.onDeselect.AddListener(OnInputFinished);
        }
    }

    private void OnDisable()
    {
        if (usernameInputField != null)
        {
            usernameInputField.onSubmit.RemoveListener(OnInputFinished);
            usernameInputField.onDeselect.RemoveListener(OnInputFinished);
        }
    }

    private void OnDestroy()
    {
        // Cancel any in-flight async work
        cts?.Cancel();
        cts?.Dispose();

        validationAnimCts?.Cancel();
        validationAnimCts?.Dispose();

        if (editButton != null)
            editButton.onClick.RemoveListener(OnEditPressed);

        if (saveButton != null)
            saveButton.onClick.RemoveListener(OnSavePressed);

        // Clean up avatar button listeners
        for (int i = 0; i < avatarButtons.Count; i++)
        {
            if (avatarButtons[i] != null)
                avatarButtons[i].onClick.RemoveAllListeners();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  LOAD PROFILE
    // ═══════════════════════════════════════════════════════════════

    private async UniTaskVoid LoadProfile(CancellationToken token)
    {
        // Display Player ID
        if (playerIdText != null && AuthenticationService.Instance.IsSignedIn)
            playerIdText.text = AuthenticationService.Instance.PlayerId;

        try
        {
            var keys = new HashSet<string> { USERNAME_KEY, AVATAR_KEY };
            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            // ── Username ──
            string savedUsername = null;
            if (data.ContainsKey(USERNAME_KEY))
                savedUsername = data[USERNAME_KEY].Value.GetAs<string>();

            if (string.IsNullOrEmpty(savedUsername))
            {
                // First-time user → generate from auth display hint or fallback
                string hint = GetAuthDisplayHint();
                savedUsername = GenerateUsernameFromHint(hint);

                await CloudSaveManager.Instance.SaveValueAsync(USERNAME_KEY, savedUsername);
                Debug.Log($"[UserProfile] Generated initial username: {savedUsername}");
            }

            token.ThrowIfCancellationRequested();

            currentUsername = savedUsername;
            SetInputFieldText(currentUsername);

            // ── Avatar ──
            if (data.ContainsKey(AVATAR_KEY))
                savedAvatarIndex = data[AVATAR_KEY].Value.GetAs<int>();

            savedAvatarIndex = Mathf.Clamp(savedAvatarIndex, 0, avatarSprites.Count - 1);
            previewedAvatarIndex = savedAvatarIndex;
            ApplyAvatar(savedAvatarIndex);
            HighlightAvatar(savedAvatarIndex);

            Debug.Log($"[UserProfile] Loaded — Username: {currentUsername}, Avatar: {savedAvatarIndex}");
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("[UserProfile] LoadProfile cancelled.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] Failed to load profile: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  USERNAME EDITING  (Edit Button → Type → Enter/Deselect → Save)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Always starts edit mode — no toggle.  This is intentional:
    /// toggling caused the bug where a second tap would try to save
    /// before the user finished typing.
    /// </summary>
    private void OnEditPressed()
    {
        if (usernameInputField == null) return;

        isEditing = true;
        ClearValidation();

        usernameInputField.interactable = true;
        usernameInputField.Select();
        usernameInputField.ActivateInputField();

        // Move caret to end so user can see what they're editing
        usernameInputField.caretPosition = usernameInputField.text.Length;
    }

    /// <summary>
    /// Fires on Enter key (onSubmit) and on tap-elsewhere (onDeselect).
    /// Both events can fire nearly simultaneously on mobile, so the
    /// <see cref="isSaving"/> guard prevents double execution.
    /// </summary>
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

            // ── Validate ──
            if (!IsValidUsername(newUsername))
            {
                ShowValidation($"Min {MIN_USERNAME_LENGTH} alphanumeric characters, no spaces or symbols");
                SetInputFieldText(currentUsername);   // Revert to last good value
                LockInput();
                return;
            }

            // ── No change? Just lock ──
            if (newUsername == currentUsername)
            {
                ClearValidation();
                LockInput();
                return;
            }

            // ── Disable field while saving (prevents further edits mid-save) ──
            usernameInputField.interactable = false;

            await CloudSaveManager.Instance.SaveValueAsync(USERNAME_KEY, newUsername);

            token.ThrowIfCancellationRequested();

            currentUsername = newUsername;
            ClearValidation();
            Debug.Log($"[UserProfile] Username saved: {currentUsername}");
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("[UserProfile] Username save cancelled.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] Failed to save username: {ex.Message}");
            ShowValidation("Save failed — please try again");
        }
        finally
        {
            SetInputFieldText(currentUsername);
            LockInput();
            isSaving = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  AVATAR
    // ═══════════════════════════════════════════════════════════════

    private void PreviewAvatar(int index)
    {
        if (index < 0 || index >= avatarSprites.Count) return;
        if (previewedAvatarIndex == index) return;

        previewedAvatarIndex = index;
        ApplyAvatar(index);
        HighlightAvatar(index);
    }

    private void ApplyAvatar(int index)
    {
        if (index < 0 || index >= avatarSprites.Count) return;

        if (profileImage != null)
            profileImage.sprite = avatarSprites[index];

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

    // ═══════════════════════════════════════════════════════════════
    //  SAVE BUTTON  (Avatar + any unsaved username change)
    // ═══════════════════════════════════════════════════════════════

    private void OnSavePressed()
    {
        SaveAvatarSelection(cts.Token).Forget();
    }

    private async UniTaskVoid SaveAvatarSelection(CancellationToken token)
    {
        bool avatarChanged = previewedAvatarIndex != savedAvatarIndex;

        if (!avatarChanged)
        {
            Debug.Log("[UserProfile] No avatar change to save.");
            return;
        }

        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(AVATAR_KEY, previewedAvatarIndex);

            token.ThrowIfCancellationRequested();

            savedAvatarIndex = previewedAvatarIndex;
            Debug.Log($"[UserProfile] Avatar saved: {savedAvatarIndex}");
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("[UserProfile] Avatar save cancelled.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] Failed to save avatar: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  VALIDATION  (with float-up + fade-out animation)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Username must be at least <see cref="MIN_USERNAME_LENGTH"/> characters
    /// and contain only letters (a-z, A-Z) and digits (0-9).
    /// No spaces, underscores, hyphens, or special characters.
    /// </summary>
    private bool IsValidUsername(string username)
    {
        if (string.IsNullOrEmpty(username) || username.Length < MIN_USERNAME_LENGTH)
            return false;

        for (int i = 0; i < username.Length; i++)
        {
            if (!char.IsLetterOrDigit(username[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// One-time setup: caches RectTransform + ensures a CanvasGroup exists
    /// on the validation text GameObject for alpha animation.
    /// CanvasGroup is added at runtime so you don't need to add it manually.
    /// </summary>
    private void InitValidationAnimation()
    {
        if (validationText == null) return;

        validationRect = validationText.GetComponent<RectTransform>();
        validationOriginalAnchoredPos = validationRect.anchoredPosition;

        // GetComponent first to avoid duplicates if one already exists
        validationCanvasGroup = validationText.GetComponent<CanvasGroup>();
        if (validationCanvasGroup == null)
            validationCanvasGroup = validationText.gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Shows validation message, holds it briefly, then animates it
    /// upward while fading out. Calling this again while an animation
    /// is in progress cancels the old one and restarts cleanly.
    /// </summary>
    private void ShowValidation(string message)
    {
        if (validationText == null) return;

        // Cancel any in-progress animation
        validationAnimCts?.Cancel();
        validationAnimCts?.Dispose();
        validationAnimCts = new CancellationTokenSource();

        // Reset to home position and fully visible
        validationRect.anchoredPosition = validationOriginalAnchoredPos;
        validationCanvasGroup.alpha = 1f;

        validationText.text = message;
        validationText.gameObject.SetActive(true);

        // Fire the hold → animate → hide sequence
        AnimateValidationOut(validationAnimCts.Token).Forget();
    }

    /// <summary>
    /// Immediately hides the validation text and cancels any running animation.
    /// </summary>
    private void ClearValidation()
    {
        validationAnimCts?.Cancel();
        validationAnimCts?.Dispose();
        validationAnimCts = null;

        if (validationText != null)
        {
            validationText.text = string.Empty;
            validationText.gameObject.SetActive(false);

            // Reset position and alpha so next show starts clean
            if (validationRect != null)
                validationRect.anchoredPosition = validationOriginalAnchoredPos;
            if (validationCanvasGroup != null)
                validationCanvasGroup.alpha = 1f;
        }
    }

    /// <summary>
    /// Async animation sequence:
    ///   1. HOLD  — message stays fully visible for <see cref="validationHoldDuration"/> seconds
    ///   2. FLOAT — moves upward by <see cref="validationFloatDistance"/> pixels
    ///              while alpha fades from 1 → 0 over <see cref="validationFadeDuration"/> seconds
    ///   3. HIDE  — deactivates the GameObject to keep the hierarchy clean
    ///
    /// Uses smooth ease-out (quadratic) for a natural motion feel.
    /// </summary>
    private async UniTaskVoid AnimateValidationOut(CancellationToken token)
    {
        try
        {
            // ── Phase 1: Hold ──
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(validationHoldDuration),
                cancellationToken: token
            );

            // ── Phase 2: Float up + Fade out ──
            Vector2 startPos = validationOriginalAnchoredPos;
            Vector2 endPos = startPos + new Vector2(0f, validationFloatDistance);

            float elapsed = 0f;

            while (elapsed < validationFadeDuration)
            {
                token.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / validationFadeDuration);

                // Ease-out quadratic: fast start, gentle stop
                float eased = 1f - (1f - t) * (1f - t);

                validationRect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
                validationCanvasGroup.alpha = 1f - eased;

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            // ── Phase 3: Clean up ──
            validationText.gameObject.SetActive(false);
            validationRect.anchoredPosition = validationOriginalAnchoredPos;
            validationCanvasGroup.alpha = 1f;
        }
        catch (System.OperationCanceledException)
        {
            // Animation was cancelled (new message shown or ClearValidation called) — no action needed
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  USERNAME GENERATION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Pulls the display name hint from <see cref="AuthBootstrapper"/>.
    /// Works across scenes because AuthBootstrapper is DontDestroyOnLoad.
    /// </summary>
    private string GetAuthDisplayHint()
    {
        if (AuthBootstrapper.Instance != null)
            return AuthBootstrapper.Instance.AuthDisplayHint;

        return null;
    }

    /// <summary>
    /// Generates a username from the auth provider's display name or email.
    ///
    /// LOGIC:
    ///   1. If hint contains '@', take everything before the '@' (email → first part)
    ///   2. If hint contains a space, take everything before the first space (display name → first name)
    ///   3. Strip all non-alphanumeric characters
    ///   4. Capitalize first letter, lowercase the rest
    ///   5. Append 3 random digits
    ///   6. If result is still too short (fewer than 7 chars), pad with random letters
    ///   7. Fallback: fully random username if no usable hint exists
    /// </summary>
    private string GenerateUsernameFromHint(string hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return GenerateFallbackUsername();

        string firstName = hint;

        // Extract part before '@' if email
        int atIndex = firstName.IndexOf('@');
        if (atIndex > 0)
            firstName = firstName.Substring(0, atIndex);

        // Extract first name if display name has spaces
        int spaceIndex = firstName.IndexOf(' ');
        if (spaceIndex > 0)
            firstName = firstName.Substring(0, spaceIndex);

        // Strip non-alphanumeric
        var cleaned = new System.Text.StringBuilder(firstName.Length);
        for (int i = 0; i < firstName.Length; i++)
        {
            if (char.IsLetterOrDigit(firstName[i]))
                cleaned.Append(firstName[i]);
        }

        if (cleaned.Length == 0)
            return GenerateFallbackUsername();

        // Capitalize: "john" → "John"
        string baseName = char.ToUpper(cleaned[0]) + cleaned.ToString(1, cleaned.Length - 1).ToLower();

        // Append 3 random digits
        string digits = Random.Range(100, 999).ToString();
        string result = baseName + digits;

        // Ensure minimum length by padding with random lowercase letters
        if (result.Length < MIN_USERNAME_LENGTH)
        {
            const string letters = "abcdefghijklmnopqrstuvwxyz";
            var padded = new System.Text.StringBuilder(result);
            while (padded.Length < MIN_USERNAME_LENGTH)
                padded.Insert(padded.Length - 3, letters[Random.Range(0, letters.Length)]);  // Insert before digits
            result = padded.ToString();
        }

        return result;
    }

    /// <summary>
    /// Fallback when no auth hint is available (e.g. anonymous login).
    /// Produces something like "Player8472" — always valid.
    /// </summary>
    private string GenerateFallbackUsername()
    {
        const string letters = "abcdefghijklmnopqrstuvwxyz";

        // "Player" (6 chars) + 4 random chars = 10 chars minimum → well above MIN_USERNAME_LENGTH
        var sb = new System.Text.StringBuilder("Player");

        // Add 1 random letter so it's not just "Player" + digits
        sb.Append(letters[Random.Range(0, letters.Length)]);

        // Add 3 random digits
        sb.Append(Random.Range(100, 999));

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    //  HELPERS
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

    // ═══════════════════════════════════════════════════════════════
    //  PUBLIC API  (for other systems to read profile data)
    // ═══════════════════════════════════════════════════════════════

    public string GetUsername() => currentUsername;
    public int GetAvatarIndex() => savedAvatarIndex;

    public Sprite GetAvatarSprite()
    {
        return (savedAvatarIndex >= 0 && savedAvatarIndex < avatarSprites.Count)
            ? avatarSprites[savedAvatarIndex]
            : null;
    }

    public string GetPlayerId()
    {
        return AuthenticationService.Instance.IsSignedIn
            ? AuthenticationService.Instance.PlayerId
            : "Not signed in";
    }
}