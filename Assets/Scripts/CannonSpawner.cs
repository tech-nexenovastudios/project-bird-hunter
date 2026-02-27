using Cysharp.Threading.Tasks;
using UnityEngine;

public class CannonSpawner : MonoBehaviour
{
    public CannonHolder_SO cannons;
    public LevelReferences references;
    public static GameObject cannon;

    private const string SELECTEDCANNONKEY = "SelectedCannonIndex";

    public async UniTask<GameObject> CannonSpawn()
    {
        await UniTask.Yield();

        //int index = await CloudSaveManager.Instance.LoadValueAsync(SELECTEDCANNONKEY, 0);
        var index =  PlayerPrefs.GetInt(SELECTEDCANNONKEY, 0);
        index = Mathf.Clamp(index, 0, cannons.cannonsData.Length - 1);

        var data = cannons.cannonsData[index];
        cannon   = Instantiate(data.cannonPrefab, transform.position, transform.rotation);

        // Wire stats
        var statsSetter = cannon.GetComponent<CannonStatsSetter>();
        if (statsSetter != null)
            statsSetter.cannonStats = data.cannonStats;

        var move =  cannon.GetComponent<CannonMove>();
        if (move != null)
        {
            var (left, right) = references.GetWall();
            move.SetWalls(left,right);
        }
        
        var health =  cannon.GetComponent<CannonHealth>();
        if (health != null)
        {
            var (slider, text) = references.GetCannonHealth();
            health.SetHealthUI(slider, text);
        }
        
        // Wire ability
        var ability = cannon.GetComponent<CannonAbility>();
        if (ability != null && AbilityManager.instance?.activeAbility != null)
            ability.activeAbility = AbilityManager.instance.activeAbility;

        // ✅ Wire bullet prefab from CannonData → CannonFire
        var fire = cannon.GetComponent<CannonFire>();
        if (fire != null)
        {
            fire.bulletPrefab = data.bulletPrefab;  // ← single prefab
            fire.AssignValue();
            fire.StartFiring();
        }

        return cannon;
    }

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