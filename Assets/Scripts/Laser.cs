using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
    public Transform laser;
    [SerializeField] List<GameObject> list = new List<GameObject>();
    public float scale;

    public float damage;
    public float laserDelay;
    public float duration;
    public Transform parent;
    bool follow;
    public CannonFire cannonFire;
    private bool canTakeDamage;


    private void OnEnable()
    {
        CannonPower.sizeDecreaseCallBack += LaserSizeUpdate;
    }
    private void OnDisable()
    {
        CannonPower.sizeDecreaseCallBack -= LaserSizeUpdate;

    }

    public void LaserSizeUpdate(float newSize)
    {
        if (newSize != 1)
        {
            {

            }
            float scalePercentage = (1 - (newSize / 1));
            scale = 200 * (1 / scalePercentage);
        }
        else
        {
            scale = 200;
        }
    }
    private void Start()
    {

        StartCoroutine(LaserCoroutine());
    }

    public void Update()
    {
        
    }
    IEnumerator LaserCoroutine()
    {
        yield return new WaitForSeconds(laserDelay);
        canTakeDamage = true;
        laser.transform.DOScaleY(scale, 0.3f);
        yield return new WaitForSeconds(duration);
        laser.transform.DOScaleY(0, 0.3f).OnComplete(() =>
        {
            canTakeDamage = false;
        });
        StartCoroutine(LaserCoroutine());
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (canTakeDamage)
        {
            foreach (GameObject go in list)
            {
                var obj = go.GetComponent<IHealthManager>();
                if (obj != null)
                {
                    obj.TakeDamage((damage * (cannonFire.Damage * cannonFire.shotsPerSecond)) * Time.deltaTime);
                }
                //Debug.Log(go.name);
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        list.Add(collision.gameObject);
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        list.Remove(collision.gameObject);

    }
}
