using System.Linq;
using UnityEngine;

public class LightingAbility : MonoBehaviour, IAbilityUse
{
    public LineRenderer[] lineRenderer;
    public LayerMask enemyMask;
    public int maxNumberOfLaserShoot = 5;
    public int abilityLevel { get; set; }
    public bool canUseAbility { get; set ; }

    public void UseAbility()
    {
        var enemies = Physics2D.OverlapCircleAll(transform.position, 10, enemyMask).OrderBy(obj => Vector2.Distance(transform.position, obj.transform.position)).ToList();
        for(int i = 0; i < enemies.Count && i < maxNumberOfLaserShoot; i++)
        {
            var health = enemies[i].GetComponent<IHealthManager>();
            if (health != null)
            {
                Debug.Log(enemies[i].name);
                health.TakeDamage(5);
                lineRenderer[i].gameObject.SetActive(true);
                lineRenderer[i].SetPosition(0,transform.position);
                lineRenderer[i].SetPosition(1, enemies[i].transform.position);
                
            }
            Invoke("DisableLine", 0.2f);
        }

    }
    public void DisableLine()
    {
        foreach (var line in lineRenderer)
        {
            line.gameObject.SetActive(false);
        }
    }
}
