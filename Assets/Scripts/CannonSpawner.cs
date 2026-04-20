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

        int id = service.EquippedId;
        if (id < 0)
        {
            Debug.LogError("[CannonSpawner] No cannon equipped.");
            return null;
        }

        if (!database.cannonPrefabs.TryGetValue(id, out var cannonPrefab) || cannonPrefab == null)
        {
            Debug.LogError($"[CannonSpawner] Cannon prefab missing for id {id} in database.");
            return null;
        }

        database.cannonBullets.TryGetValue(id, out var bulletPrefab);

        cannon = Instantiate(cannonPrefab, transform.position, transform.rotation);

        var baseCannon = cannon.GetComponent<BaseCannon>();
        if (baseCannon != null)
        {
            baseCannon.Configure(service.GetStatSheet(id), bulletPrefab);
            Debug.Log($"[CannonSpawner] Configured cannon id {id}: {cannonPrefab.name}");
        }
        else
        {
            Debug.LogError($"[CannonSpawner] {cannonPrefab.name} is missing a BaseCannon component.");
        }

        return cannon;
    }

    public void DestroyCannon()
    {
        if (cannon != null) Destroy(cannon);
    }
}
