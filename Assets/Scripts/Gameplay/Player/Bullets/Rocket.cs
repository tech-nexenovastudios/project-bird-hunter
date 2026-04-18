using Gameplay.Player;
using UnityEngine;

public class Rocket : BaseBullet
{
    protected override void HandleMovement()
    {
        // Always move straight up in world space, regardless of
        // transform rotation (the 24° tilt is purely visual to
        // correct the sprite's offset).
        transform.position += Vector3.up * bulletSpeed * Time.deltaTime;
    }
}