namespace Gameplay.Player
{
    public static class PowerUpBulletModifier
    {
        public static void Apply(IBullet bullet, string powerUpId)
        {
            if (bullet is not BaseBullet<StraightBullet, StraightBehaviour> baseBullet)
                return;
            
            switch (powerUpId)
            {
                case "damage_boost_small":
                    baseBullet.currentDamage *= 1.2f;
                    break;
                case "damage_boost_large":
                    baseBullet.currentDamage *= 1.5f;
                    break;
                case "fire_rate_boost":
                    baseBullet.currentSpeed *= 1.15f;
                    break;
                case "pierce":
                    baseBullet.pierceRemaining += 2;
                    break;
            }
        }
    }
}