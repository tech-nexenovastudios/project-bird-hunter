namespace Gameplay.PowerUps
{
    [System.Serializable]
    public class PowerupSlot
    {
        public int slotIndex;           // 0-3
        public string equippedPowerupId; // PowerupConfig.powerupId
        public bool isOnCooldown;
        public float cooldownRemaining;

        public PowerupSlot(int index)
        {
            slotIndex = index;
            equippedPowerupId = null;
            isOnCooldown = false;
            cooldownRemaining = 0f;
        }

        public bool IsEmpty => string.IsNullOrEmpty(equippedPowerupId);
    }
}