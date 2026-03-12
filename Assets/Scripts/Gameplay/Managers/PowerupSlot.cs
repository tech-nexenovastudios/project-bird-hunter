namespace Gameplay
{
    [System.Serializable]
    public class PowerUpSlot
    {
        public int slotIndex;           // 0-3
        public string equippedPowerupId; // PowerupConfig.powerupId
        public bool isOnCooldown;
        public float cooldownRemaining;

        public PowerUpSlot(int index)
        {
            slotIndex = index;
            equippedPowerupId = null;
            isOnCooldown = false;
            cooldownRemaining = 0f;
        }

        public bool IsEmpty => string.IsNullOrEmpty(equippedPowerupId);
    }
}