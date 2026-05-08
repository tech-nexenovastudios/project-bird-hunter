namespace Gameplay
{
    [System.Serializable]
    public class PowerUpSlot
    {
        public int slotIndex;           // 0-3
        public string equippedPowerupId; // PowerupConfig.powerupId
        public bool isOnCooldown;
        public float cooldownRemaining;
        // True once a non-cooldown (one-shot) powerup has been applied to the cannon.
        // Cooldown powerups (config.cooldown > 0) ignore this flag and persist across levels.
        public bool hasBeenApplied;

        public PowerUpSlot(int index)
        {
            slotIndex = index;
            equippedPowerupId = null;
            isOnCooldown = false;
            cooldownRemaining = 0f;
            hasBeenApplied = false;
        }

        public bool IsEmpty => string.IsNullOrEmpty(equippedPowerupId);
    }
}