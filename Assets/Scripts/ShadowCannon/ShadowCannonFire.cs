using System.Collections;
using UnityEngine;

public class ShadowCannonFire : MonoBehaviour
{
    public CannonFire cannonFire;
    private Coroutine fireCoroutine;
    public float damageShadowCannon;
    public Transform bulletTip;


    private void Start()
    {
       StartFiring();
    }
    private void OnEnable()
    {
        CannonFire.startFireCallBack += StartFiring;
        CannonFire.stopFireCallBack += StopFiring;
    }

    private void OnDisable()
    {
        CannonFire.startFireCallBack -= StartFiring;
        CannonFire.stopFireCallBack -= StopFiring;
    }


    public void StartFiring()
    {
        if (fireCoroutine == null)
        {
            fireCoroutine = StartCoroutine(FireLoop());
        }
    }

    // ✅ Call this to stop firing
    public void StopFiring()
    {
        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
        }
    }

    IEnumerator FireLoop()
    {
        while (true)
        {
            if (GamePoolManager.bulletQueue.Count > 0)
            {
                var bulletObj = GamePoolManager.bulletQueue[cannonFire.bulletPrefab.GetComponent<Bullet>().type].Dequeue();
                var bullet = bulletObj.GetComponent<Bullet>();
                BulletValueAssign(bullet);
            }
            else
            {
                var go = Instantiate(cannonFire.bulletPrefab);
                BulletValueAssign(go.GetComponent<Bullet>());   
               
            }
           
            yield return new WaitForSeconds(cannonFire.fireDelayBetweenBullet);
        }
    }

    public void BulletValueAssign(Bullet bullet)
    {
        bullet.gameObject.SetActive(true);
        bullet.bulletSpeed = cannonFire.bulletSpeed;
        bullet.Damage = (cannonFire.Damage * damageShadowCannon) / 100;
        bullet.bulletBounce = 0;
        bullet.transform.localScale = transform.lossyScale;
        bullet.transform.position = bulletTip.position;
    }
}
