using System.Collections.Generic;
using Gameplay.Birds;
using Gameplay.Interfaces;
using Gameplay.Player;
using UnityEngine;
using UnityUtils;

namespace Gameplay.PowerUps
{
    public class CannonPowerUpCaster : Singleton<CannonPowerUpCaster>
    {
        [Tooltip("Power-ups the player can cast with number keys.")]
        public CannonPowerUp[] hotbar;

        [Header("VFX Settings")]
        [SerializeField] private float castVfxHeightOffset = 2f;
        [SerializeField] private float runningVfxLifetime = 3f;

        private ICannon cannonRef;
        private IEntity cachedTarget;
        private MonoBehaviour cachedTargetMb;
        private readonly List<CannonPowerUp> equippedPowerUps = new();

        private void Start()
        {
            var cannonMb = FindFirstObjectByType<BaseCannon>();
            cannonRef = cannonMb as ICannon;
        }

        private void Update()
        {
            HandleHotbarInput();

            for (int i = 0; i < equippedPowerUps.Count; i++)
            {
                equippedPowerUps[i].TickReactives();
                equippedPowerUps[i].TickSummons();
                equippedPowerUps[i].TickProjectiles(cannonRef);  //laser part
            }
        }

        private void HandleHotbarInput()
        {
            for (int i = 0, len = hotbar.Length; i < len; i++)
            {
                if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;
                if (hotbar[i] == null) continue;

                CannonPowerUp powerUp = hotbar[i];

                if (powerUp.HasEnemyEffects())
                {
                    IEntity target = GetTarget();
                    if (target != null) Cast(powerUp, target);
                }

                if (powerUp.HasCannonEffects())
                    Equip(powerUp);
            }
        }

        public void Cast(CannonPowerUp powerUp, IEntity target)
        {
            if (powerUp == null || target == null) return;
            powerUp.ExecuteOnEnemy(target);
            PlayFeedback(powerUp, target as MonoBehaviour);
        }

        public void Equip(CannonPowerUp powerUp)
        {
            if (powerUp == null || cannonRef == null) return;
            powerUp.ActivateOnCannon(cannonRef);
            equippedPowerUps.Add(powerUp);

            // Register projectile modifiers on cannon so SpawnBullet applies them
            var cannonMb = cannonRef as BaseCannon;
            if (cannonMb != null)
            {
                for (int i = 0; i < powerUp.effects.Count; i++)
                    if (powerUp.effects[i] is IProjectileModifier pm)
                        cannonMb.RegisterProjectileModifier(pm);
            }
        }

        public void Unequip(CannonPowerUp powerUp)
        {
            if (powerUp == null) return;
            powerUp.DeactivateAll();
            equippedPowerUps.Remove(powerUp);

            var cannonMb = cannonRef as BaseCannon;
            if (cannonMb != null)
            {
                for (int i = 0; i < powerUp.effects.Count; i++)
                    if (powerUp.effects[i] is IProjectileModifier pm)
                        cannonMb.UnregisterProjectileModifier(pm);
            }
        }

        public void UnequipAll()
        {
            var cannonMb = cannonRef as BaseCannon;
            for (int i = equippedPowerUps.Count - 1; i >= 0; i--)
            {
                var pu = equippedPowerUps[i];
                if (pu == null) continue;
                pu.DeactivateAll();

                if (cannonMb != null)
                    for (int j = 0; j < pu.effects.Count; j++)
                        if (pu.effects[j] is IProjectileModifier pm)
                            cannonMb.UnregisterProjectileModifier(pm);
            }
            equippedPowerUps.Clear();
        }

        private IEntity GetTarget()
        {
            if (cachedTargetMb != null && cachedTarget != null && cachedTarget.IsAlive)
                return cachedTarget;

            var bird = FindFirstObjectByType<BaseBird>();
            cachedTarget = bird;
            cachedTargetMb = bird;
            return cachedTarget;
        }

        private void PlayFeedback(CannonPowerUp powerUp, MonoBehaviour targetMb)
        {
            if (targetMb != null)
            {
                if (powerUp.castVfx != null)
                {
                    Vector3 pos = targetMb.transform.position;
                    pos.y += castVfxHeightOffset;
                    Instantiate(powerUp.castVfx, pos, Quaternion.identity);
                }

                if (powerUp.runningVfx != null)
                {
                    var instance = Instantiate(powerUp.runningVfx, targetMb.transform);
                    Destroy(instance, runningVfxLifetime);
                }
            }

            if (powerUp.castSfx != null)
                AudioSource.PlayClipAtPoint(powerUp.castSfx, transform.position);
        }
    }
}