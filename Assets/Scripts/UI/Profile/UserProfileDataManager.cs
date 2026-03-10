using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using System.Collections.Generic;

/// <summary>
/// Manages user profile: username (editable), player ID (read-only),
/// and avatar selection with cloud save.
/// </summary>
public class UserProfileDataManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TextMeshProUGUI playerIdText;
    [SerializeField] private Button editButton;

    [Header("Avatar")]
    [SerializeField] private Image profileImage;               // External display — shows selected avatar
    [SerializeField] private Image profileButtonImage;
    [SerializeField] private List<Button> avatarButtons;        // 8 avatar buttons in ProfilePanel
    [SerializeField] private List<Sprite> avatarSprites;        // 8 matching sprites (same order as buttons)
    [SerializeField] private Button saveButton;

    [Header("Avatar Visual States")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.5f);

    // Cloud Save keys
    private const string USERNAME_KEY = "username";
    private const string AVATAR_KEY = "avatarIndex";

    private string currentUsername;
    private bool isEditing = false;

    private int savedAvatarIndex = 0;
    private int previewedAvatarIndex = -1;

    private void Awake()
    {
        if (usernameInputField != null)
            usernameInputField.interactable = false;

        if (editButton != null)
            editButton.onClick.AddListener(OnEditPressed);

        if (saveButton != null)
            saveButton.onClick.AddListener(OnSavePressed);

        // Hook avatar buttons
        for (int i = 0; i < avatarButtons.Count; i++)
        {
            int index = i;
            if (avatarButtons[i] != null)
                avatarButtons[i].onClick.AddListener(() => PreviewAvatar(index));
        }
    }

    private void Start()
    {
        LoadProfile().Forget();
    }

    private void OnEnable()
    {
        if (usernameInputField != null)
        {
            usernameInputField.onSubmit.AddListener(OnInputSubmit);
            usernameInputField.onDeselect.AddListener(OnInputSubmit);
        }
    }

    private void OnDisable()
    {
        if (usernameInputField != null)
        {
            usernameInputField.onSubmit.RemoveListener(OnInputSubmit);
            usernameInputField.onDeselect.RemoveListener(OnInputSubmit);
        }
    }

    private void OnDestroy()
    {
        if (editButton != null)
            editButton.onClick.RemoveListener(OnEditPressed);

        if (saveButton != null)
            saveButton.onClick.RemoveListener(OnSavePressed);

        for (int i = 0; i < avatarButtons.Count; i++)
        {
            if (avatarButtons[i] != null)
                avatarButtons[i].onClick.RemoveAllListeners();
        }
    }

    // ==================== Load Profile ====================

    private async UniTaskVoid LoadProfile()
    {
        if (playerIdText != null && AuthenticationService.Instance.IsSignedIn)
            playerIdText.text = AuthenticationService.Instance.PlayerId;

        try
        {
            var keys = new HashSet<string> { USERNAME_KEY, AVATAR_KEY };
            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            // Load username
            string savedUsername = null;
            if (data.ContainsKey(USERNAME_KEY))
                savedUsername = data[USERNAME_KEY].Value.GetAs<string>();

            if (string.IsNullOrEmpty(savedUsername))
            {
                savedUsername = GenerateRandomUsername();
                await CloudSaveManager.Instance.SaveValueAsync(USERNAME_KEY, savedUsername);
                Debug.Log($"[UserProfile] Generated new username: {savedUsername}");
            }

            currentUsername = savedUsername;
            SetInputFieldText(currentUsername);

            // Load avatar
            if (data.ContainsKey(AVATAR_KEY))
                savedAvatarIndex = data[AVATAR_KEY].Value.GetAs<int>();

            savedAvatarIndex = Mathf.Clamp(savedAvatarIndex, 0, avatarSprites.Count - 1);
            previewedAvatarIndex = savedAvatarIndex;
            ApplyAvatar(savedAvatarIndex);
            HighlightAvatar(savedAvatarIndex);

            Debug.Log($"[UserProfile] Loaded — Username: {currentUsername}, Avatar: {savedAvatarIndex}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] Failed to load profile: {ex.Message}");
        }
    }

    // ==================== Avatar ====================

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
        if (profileImage != null && index >= 0 && index < avatarSprites.Count)
        {

            profileImage.sprite = avatarSprites[index];
            profileButtonImage.sprite = avatarSprites[index];
        }
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

    // ==================== Save Button ====================

    private void OnSavePressed()
    {
        SaveProfile().Forget();
    }

    private async UniTaskVoid SaveProfile()
    {
        string newUsername = usernameInputField != null ? usernameInputField.text.Trim() : currentUsername;
        if (string.IsNullOrWhiteSpace(newUsername))
            newUsername = currentUsername;

        bool usernameChanged = newUsername != currentUsername;
        bool avatarChanged = previewedAvatarIndex != savedAvatarIndex;

        if (!usernameChanged && !avatarChanged)
        {
            Debug.Log("[UserProfile] No changes to save.");
            LockInput();
            return;
        }

        try
        {
            if (usernameChanged && avatarChanged)
            {
                await CloudSaveManager.Instance.SaveBatchAsync(
                    (USERNAME_KEY, newUsername),
                    (AVATAR_KEY, previewedAvatarIndex)
                );
                currentUsername = newUsername;
                savedAvatarIndex = previewedAvatarIndex;
            }
            else if (usernameChanged)
            {
                await CloudSaveManager.Instance.SaveValueAsync(USERNAME_KEY, newUsername);
                currentUsername = newUsername;
            }
            else
            {
                await CloudSaveManager.Instance.SaveValueAsync(AVATAR_KEY, previewedAvatarIndex);
                savedAvatarIndex = previewedAvatarIndex;
            }

            Debug.Log($"[UserProfile] Saved — Username: {currentUsername}, Avatar: {savedAvatarIndex}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] Failed to save: {ex.Message}");
        }
        finally
        {
            SetInputFieldText(currentUsername);
            LockInput();
        }
    }

    // ==================== Edit Button ====================

    private void OnEditPressed()
    {
        if (usernameInputField == null) return;

        if (!isEditing)
        {
            isEditing = true;
            usernameInputField.interactable = true;
            usernameInputField.Select();
            usernameInputField.ActivateInputField();
        }
        else
        {
            OnInputSubmit(usernameInputField.text);
        }
    }

    // ==================== Save Username (via Enter/Deselect) ====================

    private void OnInputSubmit(string input)
    {
        if (!isEditing) return;
        SaveUsername(input).Forget();
    }

    private async UniTaskVoid SaveUsername(string newUsername)
    {
        newUsername = newUsername.Trim();

        if (string.IsNullOrWhiteSpace(newUsername))
        {
            SetInputFieldText(currentUsername);
            LockInput();
            return;
        }

        if (newUsername == currentUsername)
        {
            LockInput();
            return;
        }

        usernameInputField.interactable = false;

        try
        {
            await CloudSaveManager.Instance.SaveValueAsync(USERNAME_KEY, newUsername);
            currentUsername = newUsername;
            Debug.Log($"[UserProfile] Username updated: {currentUsername}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] Failed to update username: {ex.Message}");
        }
        finally
        {
            SetInputFieldText(currentUsername);
            LockInput();
        }
    }

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

    // ==================== Random Username Generator ====================

    private string GenerateRandomUsername()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";

        char[] username = new char[8];

        for (int i = 0; i < 5; i++)
            username[i] = chars[Random.Range(0, chars.Length)];

        for (int i = 5; i < 8; i++)
            username[i] = digits[Random.Range(0, digits.Length)];

        for (int i = username.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (username[i], username[j]) = (username[j], username[i]);
        }

        return "Player_" + new string(username);
    }

    // ==================== Public API ====================

    public string GetUsername() => currentUsername;
    public int GetAvatarIndex() => savedAvatarIndex;
    public Sprite GetAvatarSprite() => (savedAvatarIndex >= 0 && savedAvatarIndex < avatarSprites.Count)
        ? avatarSprites[savedAvatarIndex] : null;

    public string GetPlayerId()
    {
        return AuthenticationService.Instance.IsSignedIn
            ? AuthenticationService.Instance.PlayerId
            : "Not signed in";
    }
}