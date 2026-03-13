using Gameplay.Player;
using UnityEngine;
using DG.Tweening;

namespace Gameplay.Player
{
    public class SingleCannon : BaseCannon
    {
        protected override void UpdateUI()
        {
            base.UpdateUI();
            if (fireParticles is { Length: > 0 })
            {
                fireParticles[0].Play();
            }
        }
        protected override void Shoot()
        {
            // Standard behavior: One bullet from the main gun tip or center
            if (gunTips == null || gunTips.Length == 0)
            {
                SpawnBullet(transform.position, transform.rotation);
            }
            else
            {
                // Single cannon usually only has one tip, but we follow the array pattern
                SpawnBullet(gunTips[0].position, gunTips[0].rotation);
            }

            if (fireParticles != null && fireParticles.Length > 0)
            {
                fireParticles[0].Play();
            }
        }
        
        // ── Trigger (Eggs, PowerUps, hazard zones) ─────────────────────────
 
        protected override void HandleTriggerEnter(CannonCollisionData data)
        {
            base.HandleTriggerEnter(data); // applies part-scaled damage from IDamageSource hazards
 
            switch (data.HitPartType)
            {
                case CannonPartType.Barrel:
                    // Barrel hit — play a heavier impact effect (it has a higher multiplier)
                    Debug.Log($"[PlayerCannon] Barrel hit! Final damage applied: {data.FinalDamage}");
                    break;
 
                case CannonPartType.Shield:
                    // Shield absorbed most of it — play a 'blocked' feedback
                    Debug.Log($"[PlayerCannon] Shield hit. Reduced damage: {data.FinalDamage}");
                    break;
 
                case CannonPartType.Wheel:
                case CannonPartType.Body:
                case CannonPartType.Base:
                    Debug.Log($"[PlayerCannon] {data.HitPartType} hit. Damage: {data.FinalDamage}");
                    break;
            }
        }

        protected override void HandleTriggerExit(CannonCollisionData data)
        {
            
        }
 
        // ── Solid collision (falling debris, walls, etc.) ──────────────────
 
        protected override void HandleCollisionEnter(CannonCollisionData data)
        {
            base.HandleCollisionEnter(data); // applies part-scaled damage
 
            if (data.FinalDamage > 0)
                Debug.Log($"[PlayerCannon] Solid hit on {data.HitPartType} — damage: {data.FinalDamage}");
        }

        protected override void HandleCollisionExit(CannonCollisionData data)
        {
            
        }
    }
}