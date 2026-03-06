using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Gameplay.Player;

public class CannonSpawner : MonoBehaviour
{
    public CannonHolder_SO cannons;
    public LevelReferences references;
    public static GameObject cannon;

    private const string SELECTEDCANNONKEY = "SelectedCannonIndex";

    public async UniTask<GameObject> CannonSpawn()
    {
        await UniTask.Yield();

        var index = PlayerPrefs.GetInt(SELECTEDCANNONKEY, 0);
        index = Mathf.Clamp(index, 0, cannons.cannonsData.Length - 1);

        var data = cannons.cannonsData[index];
        cannon = Instantiate(data.cannonPrefab, transform.position, transform.rotation);

        // --- NEW CANNON SYSTEM ---
        var baseCannon = cannon.GetComponent<BaseCannon>();
        if (baseCannon != null)
        {
            baseCannon.Configure(data.cannonStats, references, data.bulletPrefab);
            Debug.Log($"[CannonSpawner] Configured new BaseCannon: {data.cannonPrefab.name}");
        }
        else
        {
            // --- LEGACY FALLBACK ---
            Debug.LogWarning($"[CannonSpawner] {data.cannonPrefab.name} does not have BaseCannon component. Using legacy setup.");
            
            var statsSetter = cannon.GetComponent<CannonStatsSetter>();
            if (statsSetter != null)
                statsSetter.cannonStats = data.cannonStats;

            var move = cannon.GetComponent<CannonMove>();
            if (move != null)
            {
                var (left, right) = references.GetWall();
                move.SetWalls(left, right);
            }

            var (slider, text) = references.GetCannonHealth();
            var legacyHealth = cannon.GetComponent<CannonHealth>();
            if (legacyHealth != null)
                legacyHealth.SetHealthUI(slider, text);

            var fire = cannon.GetComponent<CannonFire>();
            if (fire != null)
            {
                fire.bulletPrefab = data.bulletPrefab;
                fire.AssignValue();
                fire.StartFiring();
            }
        }

        return cannon;
    }

    [Obsolete]
    public GameObject SpawnCannon()
    {
        int index = PlayerPrefs.GetInt("CannonIndex", 0);
        index  = Mathf.Clamp(index, 0, cannons.cannonsData.Length - 1);
        cannon = Instantiate(cannons.cannonsData[index].cannonPrefab);
        return cannon;
    }

    public void DestroyCannon()
    {
        if (cannon != null) Destroy(cannon);
    }
}