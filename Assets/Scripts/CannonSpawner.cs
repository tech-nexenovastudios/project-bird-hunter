using BirdHunter.Inventory;
using Cysharp.Threading.Tasks;
using Gameplay.Player;
using UnityEngine;

public class CannonSpawner : MonoBehaviour
{
    public CannonDatabase database;
    public static GameObject cannon;

    public async UniTask<GameObject> CannonSpawn()
    {
        string key = await CannonLoader.GetEquippedCannonKeyAsync();
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[CannonSpawner] No equipped cannon in cloud snapshot.");
            return null;
        }

        var entry = database?.GetEntry(key);
        if (entry?.cannonPrefab == null)
        {
            Debug.LogError($"[CannonSpawner] Cannon prefab missing for key '{key}' in database.");
            return null;
        }

        var statSheet = await CannonLoader.BuildStatSheetForEquippedAsync();
        if (statSheet == null)
        {
            Debug.LogError("[CannonSpawner] Failed to build stat sheet.");
            return null;
        }

        cannon = Instantiate(entry.cannonPrefab, transform.position, transform.rotation);

        // Search on root first, then in children (in case BaseCannon sits on a child object)
        var baseCannon = cannon.GetComponent<BaseCannon>()
                         ?? cannon.GetComponentInChildren<BaseCannon>(true);

        if (baseCannon != null)
        {
            baseCannon.Configure(statSheet, entry.bulletPrefab);

            // Wire the cannon into the power-up caster
            if (Gameplay.PowerUps.CannonPowerUpCaster.Instance != null)
            {
                Gameplay.PowerUps.CannonPowerUpCaster.Instance.SetCannon(baseCannon);
            }
            else
            {
                Debug.LogWarning("[CannonSpawner] CannonPowerUpCaster.Instance is null. PowerUps won't work.");
            }

            Debug.Log($"[CannonSpawner] Configured cannon '{key}': {entry.cannonPrefab.name}");
        }
        else
        {
            Debug.LogError($"[CannonSpawner] Prefab '{entry.cannonPrefab.name}' (key: '{key}') is missing a BaseCannon component. " +
                           "Check the prefab and its children.");
        }

        return cannon;
    }

    public void DestroyCannon()
    {
        if (cannon != null) Destroy(cannon);
    }
}