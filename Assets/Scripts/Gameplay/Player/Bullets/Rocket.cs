using Gameplay.Player;
using UnityEngine;

public class Rocket : BaseBullet
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   

   
    protected override void HandleMovement()
    {
        // Pure movement — never touch the trail here
        transform.position += transform.up * bulletSpeed * Time.deltaTime;
    }
}
