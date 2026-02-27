using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Gameplay.Birds
{
    public class NormalBird : BaseBird
    {
        // ─────────────────────────────────────────
        // FX — wire in prefab Inspector
        // ─────────────────────────────────────────
        [Header("FX")]
        [SerializeField] private ParticleSystem hitParticle;
        [SerializeField] private ParticleSystem deathParticle;

        // ─────────────────────────────────────────
        // NOTE: No extra fields — lifetime/hp all live in BaseBird
        // ─────────────────────────────────────────

        protected override void Update()    => base.Update();

        // ─────────────────────────────────────────
        // Hit FX — squish punch + particle flash
        // ─────────────────────────────────────────
        // ── FX placeholder ──────────────────────────────
        public override UniTask PlayFX()
        {
            Debug.Log($"[NormalBird] Hit FX placeholder | HP left: {currentHp}");
            return UniTask.CompletedTask;
        }

        protected override void Die(bool killedByPlayer)
        {
            Debug.Log($"[NormalBird] Died | killedByPlayer: {killedByPlayer}");
            base.Die(killedByPlayer); // fires OnDestroyed → SpawnController cleanup
        }
    }
}
