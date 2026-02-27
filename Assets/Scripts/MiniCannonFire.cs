using System.Collections;
using UnityEngine;

public class MiniCannonFire : MonoBehaviour
{
    [SerializeField] Transform tip;
    public GameObject bulletPrefab;
    public float damage;
    public float shotsPerSecond;

    [HideInInspector] public float bulletSpeed = 5f;
    [HideInInspector] public float Damage = 1;

    [SerializeField] ParticleSystem fireParticle;

    private void Start()
    {
        StartCoroutine(StartFire());
    }

    IEnumerator StartFire()
    {
        GameObject go;
        while (true)
        {
            yield return new WaitForSeconds(shotsPerSecond / 60);
            go = Instantiate(bulletPrefab, tip.position, Quaternion.identity);
            fireParticle.Play();
            var bullet = go.GetComponent<Bullet>();
            if(bullet != null)
            {
                bullet.bulletSpeed = bulletSpeed;
                bullet.Damage = damage;
            }
        }
    }
}
