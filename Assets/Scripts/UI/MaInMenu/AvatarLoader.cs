using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class AvatarLoader : MonoBehaviour
{
    [SerializeField] private Image profileImage;
    private const string AVATAR_KEY = "avatarIndex";
    private int savedAvatarIndex = 0;
    [SerializeField] private List<Sprite> avatarSprites;        // 8 matching sprites (same order as buttons)
    void Start()
    {
        LoadAvatar().Forget();
    }
    //laod method
    private async UniTaskVoid LoadAvatar()
    {
        try
        {
            var keys = new HashSet<string> { AVATAR_KEY };
            var data = await CloudSaveManager.Instance.LoadAsync(keys);

            // Load avatar
            if (data.ContainsKey(AVATAR_KEY))
                savedAvatarIndex = data[AVATAR_KEY].Value.GetAs<int>();

            savedAvatarIndex = Mathf.Clamp(savedAvatarIndex, 0, avatarSprites.Count - 1);

            profileImage.sprite = avatarSprites[savedAvatarIndex];
            Debug.Log($"[UserProfile] Loaded —  Avatar: {savedAvatarIndex}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UserProfile] Failed to load profile: {ex.Message}");
        }
    }

}
