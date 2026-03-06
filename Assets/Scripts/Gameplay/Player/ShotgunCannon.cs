using Gameplay.Player;
using UnityEngine;

namespace Gameplay.Player
{
    public class ShotgunCannon : BaseCannon
    {
        // Shotgun Cannon fires multiple projectiles in a spread pattern.
        // It relies on gunTips being configured in a spread in the prefab.
        // Firing logic inherits from BaseCannon which shoots from all tips.
    }
}
