using System.Collections.Generic;
using Gameplay.Birds;
using Gameplay.Events;
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
        private GameObject _activeRunningVfx;

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

            // ── Enforce single-slot: clear whatever is active before equipping ──
            if (equippedPowerUps.Count > 0)
            {
                UnityEngine.Debug.Log($"[PowerUpCaster] Replacing active power-up(s) with '{powerUp.config?.id}'");
                UnequipAll();
            }

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

            UnityEngine.Debug.Log($"[PowerUpCaster] ✅ Equip complete. Total equipped: {equippedPowerUps.Count}");
        }

        private void OnEnable()
        {
            GameEvents.OnPowerupSelected += OnPowerupSelectedFromUI;
        }

        private void OnDisable()
        {
            GameEvents.OnPowerupSelected -= OnPowerupSelectedFromUI;
        }

        // Fires only when the player commits a powerup via the spin UI, not on every level reapply.
        // This is the right moment for the "you just picked this!" cast burst + running VFX.
        private void OnPowerupSelectedFromUI(PowerupConfig config)
        {
            if (config == null) return;
            CannonPowerUp powerUp = FindByID(config.id);
            if (powerUp == null) return;
            PlayFeedback(powerUp, cannonRef as MonoBehaviour);
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
                    //Vector3 pos = targetMb.transform.position;
                    //pos.y += castVfxHeightOffset;
                    //Instantiate(powerUp.castVfx, pos, Quaternion.identity);
                }

                // Kill any lingering runningVfx from a previous selection so they don't stack on the cannon.
                if (_activeRunningVfx != null) Destroy(_activeRunningVfx);

                if (powerUp.runningVfx != null)
                {
                    _activeRunningVfx = Instantiate(powerUp.runningVfx, targetMb.transform);
                    // Snap to cannon pivot — ignore the prefab's authored world offset so it never
                    // floats away from the cannon when parented.
                    var t = _activeRunningVfx.transform;
                    t.localPosition = Vector3.zero;
                    t.localRotation = Quaternion.identity;
                    Destroy(_activeRunningVfx, runningVfxLifetime);
                }
            }

            if (powerUp.castSfx != null)
                AudioSource.PlayClipAtPoint(powerUp.castSfx, transform.position);
        }
    }
}