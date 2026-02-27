using UnityEngine;

public class GunBird : MonoBehaviour
{
    [SerializeField] Transform gunPos;
    Transform player;
    [SerializeField] GameObject bullet;
    [SerializeField]float timeBetweenFire = 5f;
    float timer = 0;
    private void Start()
    {
        player = CannonSpawner.cannon.transform;
    }
    private void Update()
    {
        var direction = gunPos.transform.position - player.position;
        var angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        gunPos.transform.rotation = Quaternion.Euler(0, 0, -angle);
        timer += Time.deltaTime;
        if (timeBetweenFire <= timer)
        {
            timer = 0;
            var bulletGo = Instantiate(bullet, gunPos.position-transform.up*0.5f, gunPos.rotation);
            bulletGo.SetActive(true);
        }
    }

}
