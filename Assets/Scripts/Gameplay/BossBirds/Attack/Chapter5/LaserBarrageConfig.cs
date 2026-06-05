using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch05/Laser Barrage")]
public class LaserBarrageConfig : BaseAttackConfig
{
    [Header("Barrage")]
    public float fireRate = 6f;

    [Header("Laser Shot")]
    public GameObject laserShotPrefab;
    public float shotSpeed = 12f;
    public int shotDamage = 15;
    public string leftLaserDropPosition = "LaserDropPos1";
    public string rightLaserDropPosition = "LaserDropPos2";
    public float spreadAngle = 12f;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<LaserBarrageBehaviour>();
        b.SetConfig(this);
        return b;
    }

    public override void CollectPrewarmPrefabs(System.Collections.Generic.List<GameObject> into)
    {
        base.CollectPrewarmPrefabs(into);
        if (laserShotPrefab != null) into.Add(laserShotPrefab);
    }
}