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
        [Tooltip("All CannonPowerUp assets. Each must have config.id matching its PowerupConfig.id")]
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
            UnityEngine.Debug.Log($"[PowerUpCaster] Start — hotbar size: {hotbar?.Length ?? 0}");
            for (int i = 0; i < hotbar.Length; i++)
            {
                var pu = hotbar[i];
                if (pu == null)
                    UnityEngine.Debug.LogWarning($"[PowerUpCaster]   hotbar[{i}]: NULL entry.");
                else if (pu.config == null)
                    UnityEngine.Debug.LogWarning($"[PowerUpCaster]   hotbar[{i}]: CannonPowerUp has no config assigned.");
                else
                    UnityEngine.Debug.Log($"[PowerUpCaster]   hotbar[{i}]: '{pu.config.id}' → {pu.config.displayName}");
            }
        }

        // ───────── Cannon injection ─────────
        public void SetCannon(BaseCannon cannon)
        {
            if (cannon == null)
            {
                UnityEngine.Debug.LogError("[PowerUpCaster] ❌ SetCannon called with null cannon.");
                return;
            }
            cannonRef = cannon as ICannon;
            UnityEngine.Debug.Log($"[PowerUpCaster] SetCannon — cannon set to '{cannon.gameObject.name}'. cannonRef null? {cannonRef == null}");
        }

        private void Update()
        {
            for (int i = 0; i < equippedPowerUps.Count; i++)
            {
                equippedPowerUps[i].TickReactives();
                equippedPowerUps[i].TickSummons();
                equippedPowerUps[i].TickProjectiles(cannonRef);
            }
        }

        // ───────── ID lookup ─────────
        public CannonPowerUp FindByID(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                UnityEngine.Debug.LogWarning("[PowerUpCaster] FindByID called with null or empty id.");
                return null;
            }

            foreach (var pu in hotbar)
            {
                if (pu == null) continue;
                if (pu.config == null)
                {
                    UnityEngine.Debug.LogWarning("[PowerUpCaster] A hotbar entry has no config assigned — skipping.");
                    continue;
                }
                if (pu.config.id == id)
                {
                    UnityEngine.Debug.Log($"[PowerUpCaster] FindByID '{id}' → ✅ Found '{pu.config.displayName}'");
                    return pu;
                }
            }

            UnityEngine.Debug.LogError($"[PowerUpCaster] FindByID '{id}' → ❌ Not found in hotbar. Check that a CannonPowerUp with config.id = '{id}' exists in the hotbar array.");
            return null;
        }

        // ───────── Equip / Unequip ─────────
        public void Equip(CannonPowerUp powerUp)
        {
            if (powerUp == null)
            {
                UnityEngine.Debug.LogError("[PowerUpCaster] Equip called with null powerUp.");
                return;
            }

            if (cannonRef == null)
            {
                UnityEngine.Debug.LogError($"[PowerUpCaster] ❌ Equip '{powerUp.config?.id}' failed — cannonRef is null. SetCannon() was not called before Equip.");
                return;
            }

            // ─── Enforce single-powerup rule: unequip anything currently active ───
            if (equippedPowerUps.Count > 0)
            {
                UnityEngine.Debug.Log($"[PowerUpCaster] Enforcing single-powerup rule — unequipping {equippedPowerUps.Count} existing powerup(s).");
                UnequipAll();
            }

            // Guard: if the incoming powerup is the one we just removed, still allow re-equip
            UnityEngine.Debug.Log($"[PowerUpCaster] Equipping '{powerUp.config?.id}'... effects count: {powerUp.effects?.Count ?? 0}");
            powerUp.ActivateOnCannon(cannonRef);
            equippedPowerUps.Add(powerUp);

            var cannonMb = cannonRef as BaseCannon;
            if (cannonMb != null)
            {
                for (int i = 0; i < powerUp.effects.Count; i++)
                {
                    if (powerUp.effects[i] is IProjectileModifier pm)
                    {
                        cannonMb.RegisterProjectileModifier(pm);
                        UnityEngine.Debug.Log($"[PowerUpCaster]   Registered IProjectileModifier: {pm.GetType().Name}");
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("[PowerUpCaster] cannonRef is not a BaseCannon — IProjectileModifier registration skipped.");
            }

            UnityEngine.Debug.Log($"[PowerUpCaster] ✅ Equip complete. Active powerup: '{powerUp.config?.id}'");
        }

        public void Unequip(CannonPowerUp powerUp)
        {
            if (powerUp == null) return;

            UnityEngine.Debug.Log($"[PowerUpCaster] Unequipping '{powerUp.config?.id}'");
            powerUp.DeactivateAll();
            equippedPowerUps.Remove(powerUp);

            var cannonMb = cannonRef as BaseCannon;
            if (cannonMb != null)
                for (int i = 0; i < powerUp.effects.Count; i++)
                    if (powerUp.effects[i] is IProjectileModifier pm)
                        cannonMb.UnregisterProjectileModifier(pm);
        }

        public void UnequipAll()
        {
            UnityEngine.Debug.Log($"[PowerUpCaster] UnequipAll — currently equipped: {equippedPowerUps.Count}");

            var cannonMb = cannonRef as BaseCannon;
            for (int i = equippedPowerUps.Count - 1; i >= 0; i--)
            {
                var pu = equippedPowerUps[i];
                if (pu == null) continue;

                UnityEngine.Debug.Log($"[PowerUpCaster]   Deactivating '{pu.config?.id}'");
                pu.DeactivateAll();

                if (cannonMb != null)
                    for (int j = 0; j < pu.effects.Count; j++)
                        if (pu.effects[j] is IProjectileModifier pm)
                            cannonMb.UnregisterProjectileModifier(pm);
            }
            equippedPowerUps.Clear();
            UnityEngine.Debug.Log("[PowerUpCaster] UnequipAll done.");
        }

        // ───────── Helpers ─────────
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