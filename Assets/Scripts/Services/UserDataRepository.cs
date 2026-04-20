using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UserDataRepository
{
    private CloudDatabase cloudDatabase;
    private ICloudSaveManager cloudSave;

    public UserData Data => cloudDatabase?.UserData;

    public void Initialize(CloudDatabase cloudDb, ICloudSaveManager cloudSaveManager)
    {
        cloudDatabase = cloudDb;
        cloudSave = cloudSaveManager;
    }

    public async UniTask SaveAsync()
    {
        if (cloudDatabase?.UserData == null || cloudSave == null)
        {
            Debug.LogError("[UserDataRepository] Not initialized or UserData missing.");
            return;
        }

        try
        {
            await cloudSave.SaveValueAsync(CloudKeys.USER_DATA, cloudDatabase.UserData);
            EventBus.Publish(new UserDataSavedEvent());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UserDataRepository] Save failed: {ex.Message}");
            throw;
        }
    }
}