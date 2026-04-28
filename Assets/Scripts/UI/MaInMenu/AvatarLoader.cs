using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarLoader : MonoBehaviour
{
    [SerializeField] private Image profileImage;
    [SerializeField] private List<Sprite> avatarSprites;

    private UserDataRepository _repo;

    private void OnEnable()
    {
        _repo = ServiceLocator.Get<UserDataRepository>();
        if (_repo != null) _repo.OnUserDataChanged += LoadAvatar;
        LoadAvatar();
    }

    private void OnDisable()
    {
        if (_repo != null) _repo.OnUserDataChanged -= LoadAvatar;
    }

    private void LoadAvatar()
    {
        if (profileImage == null || avatarSprites == null || avatarSprites.Count == 0) return;
        if (_repo == null) _repo = ServiceLocator.Get<UserDataRepository>();
        if (_repo?.UserData == null) return;

        int index = Mathf.Clamp(_repo.UserData.avatarIndex, 0, avatarSprites.Count - 1);
        profileImage.sprite = avatarSprites[index];
    }

    public void Refresh() => LoadAvatar();
}