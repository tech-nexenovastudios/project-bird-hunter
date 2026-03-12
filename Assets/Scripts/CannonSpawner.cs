using System;
using Cysharp.Threading.Tasks;
using Gameplay.Managers;
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
            data.cannonStats.ApplyProgression(GameProgressManager.Instance.GlobalLevel);
            
            baseCannon.Configure(data.cannonStats, references, data.bulletPrefab);
            Debug.Log($"[CannonSpawner] Configured new BaseCannon: {data.cannonPrefab.name}");
        }
        else Debug.LogError($"[CannonSpawner] {data.cannonPrefab.name} does not have a BaseCannon component!");

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