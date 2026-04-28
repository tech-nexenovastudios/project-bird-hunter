using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UserDataRepository
{
    private CloudDatabase cloudDatabase;
    private ICloudSaveManager cloudSave;

    // Both properties now return the same underlying data — no more null trap.
    public UserData UserData => cloudDatabase?.UserData;
    public UserData Data => cloudDatabase?.UserData;

    public event Action OnUserDataChanged;

    public void Initialize(CloudDatabase cloudDb, ICloudSaveManager cloudSaveManager)
    {
        cloudDatabase = cloudDb;
        cloudSave = cloudSaveManager;
        OnUserDataChanged?.Invoke();   // Initial load — wakes up reactive UI
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
            OnUserDataChanged?.Invoke();   // Notify UI after every successful save
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UserDataRepository] Save failed: {ex.Message}");
            throw;
        }
    }
}