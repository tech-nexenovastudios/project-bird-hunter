using BirdHunter.Inventory;
using BirdHunter.Inventory.Services;
using Cysharp.Threading.Tasks;
using Gameplay.Player;
using UnityEngine;

public class CannonSpawner : MonoBehaviour
{
    public CannonDatabase database;
    public static GameObject cannon;

    public async UniTask<GameObject> CannonSpawn()
    {
        var service = CannonInventoryService.Instance;
        if (service == null)
        {
            Debug.LogError("[CannonSpawner] CannonInventoryService is not in the scene.");
            return null;
        }

        while (!service.IsReady) await UniTask.Yield();

        string key = service.EquippedKey;
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[CannonSpawner] No cannon equipped.");
            return null;
        }

        var entry = database?.GetEntry(key);
        if (entry?.cannonPrefab == null)
        {
            Debug.LogError($"[CannonSpawner] Cannon prefab missing for key '{key}' in database.");
            return null;
        }

        cannon = Instantiate(entry.cannonPrefab, transform.position, transform.rotation);

        var baseCannon = cannon.GetComponent<BaseCannon>();
        if (baseCannon != null)
        {
            baseCannon.Configure(service.GetStatSheet(key), entry.bulletPrefab);
            Debug.Log($"[CannonSpawner] Configured cannon '{key}': {entry.cannonPrefab.name}");
        }
        else
        {
            Debug.LogError($"[CannonSpawner] {entry.cannonPrefab.name} is missing a BaseCannon component.");
        }

        return cannon;
    }

    public void DestroyCannon()
    {
        if (cannon != null) Destroy(cannon);
    }
}
