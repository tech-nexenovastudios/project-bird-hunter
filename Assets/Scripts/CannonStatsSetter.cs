using Gameplay.Managers;
using UnityEngine;

public class CannonStatsSetter : MonoBehaviour
{
    public CannonStats cannonStats;
    public CannonFire   cannonFire;
    public CannonMove   cannonMove;
    public CannonHealth cannonHealth;

    private void Start()
    {
        if (cannonStats == null) return;

        int globalLevel = GameProgressManager.Instance.GlobalLevel;
        cannonStats.ApplyProgression(globalLevel);

        if (cannonFire   != null) cannonFire.cannonStats   = cannonStats;
        if (cannonMove   != null) cannonMove.cannonStats   = cannonStats;
        if (cannonHealth != null) cannonHealth.cannonStats = cannonStats;
    }

    private void OnValidate()
    {
        if (cannonFire   != null) cannonFire.cannonStats   = cannonStats;
        if (cannonMove   != null) cannonMove.cannonStats   = cannonStats;
        if (cannonHealth != null) cannonHealth.cannonStats = cannonStats;
    }
}