using System.Collections;
using UnityEngine;

public class LaserBarrageBehaviour : BaseAttackBehaviour
{
    private LaserBarrageConfig config;

    // Cache the Transform refs, NOT the world positions
    private Transform leftSpawnTransform;
    private Transform rightSpawnTransform;

    private Quaternion leftRot;
    private Quaternion rightRot;
    private Vector2 leftDir;
    private Vector2 rightDir;

    public void SetConfig(LaserBarrageConfig cfg) => config = cfg;

    protected override void OnExecute()
    {
        NotifyAttackStarted();
        CacheSpawnData();
        StartCoroutine(BarrageSequence());
    }

    private void CacheSpawnData()
    {
        // Find() runs once � we hold the Transform, not a snapshot of its position
        leftSpawnTransform = boss.transform.Find(config.leftLaserDropPosition);
        rightSpawnTransform = boss.transform.Find(config.rightLaserDropPosition);

        // Angles and directions are fixed � still safe to cache
        leftRot = Quaternion.Euler(0, 0, -config.spreadAngle);
        rightRot = Quaternion.Euler(0, 0, config.spreadAngle);
        leftDir = leftRot * Vector2.down;
        rightDir = rightRot * Vector2.down;
    }

    private IEnumerator BarrageSequence()
    {
        float elapsed = 0f;
        float shotInterval = 1f / config.fireRate;
        float shotTimer = 0f;

        while (elapsed < config.duration)
        {
            shotTimer += Time.deltaTime;

            if (shotTimer >= shotInterval)
            {
                shotTimer -= shotInterval;
                FireShots();
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isRunning = false;
        NotifyAttackComplete();
    }

    private void FireShots()
    {
        // .position is read fresh each shot � always tracks the moving boss
        var leftShot = PoolManager.Get(config.laserShotPrefab, leftSpawnTransform.position, leftRot);
        var rightShot = PoolManager.Get(config.laserShotPrefab, rightSpawnTransform.position, rightRot);

        leftShot.GetComponent<Rigidbody2D>().linearVelocity = leftDir * config.shotSpeed;
        rightShot.GetComponent<Rigidbody2D>().linearVelocity = rightDir * config.shotSpeed;

        PoolManager.ReturnDelayed(leftShot, 5f);
        PoolManager.ReturnDelayed(rightShot, 5f);
    }

    public override void OnStop() { StopAllCoroutines(); isRunning = false; }
    public override void OnCleanup() => OnStop();
}