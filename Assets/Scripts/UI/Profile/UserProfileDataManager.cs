using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;

/// <summary>
/// Manages the user profile UI, including loading and saving the player's username
/// via Unity Cloud Save. The player ID is read-only and sourced from Unity Authentication.
/// </summary>
public class UserProfileDataManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInputField; // Input field for the editable username
    [SerializeField] private TextMeshProUGUI playerIdText;      // Displays the read-only Unity player ID
    [SerializeField] private Button editButton;                 // Toggles edit mode for the username

    // Cloud Save key used to persist the username across sessions
    private const string USERNAME_KEY = "username";

    private string currentUsername;      // The last saved/loaded username
    private bool isEditing = false;      // Tracks whether the username field is currently in edit mode

    private void Awake()
    {
        // Lock input field by default so users can't type until they press Edit
        if (usernameInputField != null)
            usernameInputField.interactable = false;

        if (editButton != null)
            editButton.onClick.AddListener(OnEditPressed);
    }

    private void Start()
    {
        // Kick off async profile load without blocking the main thread
        LoadProfile().Forget();
    }

    private void OnEnable()
    {
        // Subscribe to submit/deselect events so the username saves when the user
        // presses Enter or clicks away from the input field
        if (usernameInputField != null)
        {
            usernameInputField.onSubmit.AddListener(OnInputSubmit);
            usernameInputField.onDeselect.AddListener(OnInputSubmit);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks or stale callbacks when the object is disabled
        if (usernameInputField != null)
        {
            usernameInputField.onSubmit.RemoveListener(OnInputSubmit);
            usernameInputField.onDeselect.RemoveListener(OnInputSubmit);
        }
    }

    private void OnDestroy()
    {
        // Clean up the edit button listener when the object is destroyed
        if (editButton != null)
            editButton.onClick.RemoveListener(OnEditPressed);
    }

    // ==================== Load Profile ====================

    private async UniTaskVoid LoadProfile()
    {
        // Display Player ID (fixed, never changes)
        if (playerIdText != null && AuthenticationService.Instance.IsSignedIn)
            playerIdText.text = AuthenticationService.Instance.PlayerId;

        try
        {
            string savedUsername = await CloudSaveManager.Instance.LoadValueAsync<string>(USERNAME_KEY, null);

            if (string.IsNullOrEmpty(savedUsername))
            {
                savedUsername = GenerateRandomUsername();
                await CloudSaveManager.Instance.SaveValueAsync(USERNAME_KEY, savedUsername);
                Debug.Log($"[UserProfile] Generated new username: {savedUsername}");
            }
            else
            {
                Debug.Log($"[UserProfile] Loaded username: {savedUsername}");
            }

            currentUsername = savedUsername;
            SetInputFieldText(currentUsername);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] Failed to load profile: {ex.Message}");
        }
    }

    // ==================== Edit Button ====================

    private void OnEditPressed()
    {
        if (usernameInputField == null) return;

        if (!isEditing)
        {
            // Enable editing
            isEditing = true;
            usernameInputField.interactable = true;
            usernameInputField.Select();
            usernameInputField.ActivateInputField();
        }
        else
        {
            // Confirm edit
            OnInputSubmit(usernameInputField.text);
        }
    }

    // ==================== Save Username ====================

    private void OnInputSubmit(string input)
    {
        if (!isEditing) return;
        SaveUsername(input).Forget();
    }

    private async UniTaskVoid SaveUsername(string newUsername)
    {
        newUsername = newUsername.Trim();

        // Revert if empty
        if (string.IsNullOrWhiteSpace(newUsername))
        {
            Debug.LogWarning("[UserProfile] Cannot save empty username.");
            SetInputFieldText(currentUsername);
            LockInput();
            return;
        }

        // No change
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
            // Force refresh so text is visible even when non-interactable
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

    public string GetPlayerId()
    {
        return AuthenticationService.Instance.IsSignedIn
            ? AuthenticationService.Instance.PlayerId
            : "Not signed in";
    }
}