using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class LaserRotate : MonoBehaviour, IAbilityUse
{
    public int abilityLevel { get; set; }

    public float damage;

    public bool canUseAbility { get; set; }

    public List<IHealthManager> enemyList = new();

    public int enemyLayer;

    public bool canGiveDamage;

    private void Awake()
    {
        enemyLayer = LayerMask.NameToLayer("Egg");
    }
    private void Start()
    {
        canUseAbility = true;
       
    }

    public void UseAbility()
    {
        if (canUseAbility)
        {
            canUseAbility = false;
            canGiveDamage = true;
            transform.DOScaleY(200, 1f).OnComplete(() =>
            {
                transform.DORotate(new Vector3(0, 0, transform.eulerAngles.z + 180), 1).OnComplete(() =>
                {
                    transform.DOScaleY(0, 1f).OnComplete(() =>
                    {
                        canUseAbility = true;
                        canGiveDamage = false;
                        enemyList.Clear();
                    });
                });
            });
        }

    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (enemyList.Count > 0)
        {
            foreach (var health in enemyList)
            {
                if (health != null)
                {
                   health.TakeDamage(damage * Time.deltaTime);
                }
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (canGiveDamage)
        {
            if (collision.gameObject.layer == enemyLayer)
            {
                enemyList.Add(collision.GetComponent<IHealthManager>());

            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (canGiveDamage)
        {
            if (collision.gameObject.layer == enemyLayer)
            {
                enemyList.Remove(collision.GetComponent<IHealthManager>());
            }
        }
    }
}
