using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Player
{
    public class SingleCannon : BaseCannon
    {
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private Transform muzzleTransform;

        [SerializeField] private Vector3 muzzleEndPosition;
        [SerializeField] private Vector3 muzzleEndScale;

        private Vector3 muzzleStartPosition;
        private Vector3 muzzleStartScale;
        private Sequence muzzleSequence;

        protected override void Awake()
        {
            base.Awake();  
            // ↑ Call base FIRST — it sets up Rigidbody2D
            //   Your original called base.Awake() last,
            //   which is fine here but bad habit if base
            //   ever initializes something you depend on

            muzzleStartPosition = muzzleTransform.localPosition;
            muzzleStartScale = muzzleTransform.localScale;

            // Keep the GameObject ACTIVE — just make sure particles aren't playing
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // ↑ Stop + Clear in one call
            //   true = also stop children particle systems
            //   StopEmittingAndClear = immediately invisible
        }
        
        protected override void Shoot()
        {
            // Kill any running sequence so rapid-firing doesn't
            // stack tweens and leave the muzzle in a broken state
            muzzleSequence?.Kill();

            // Reset to known state before starting new sequence
            muzzleTransform.localPosition = muzzleStartPosition;
            muzzleTransform.localScale = muzzleStartScale;

            muzzleSequence = DOTween.Sequence();

            // ── Recoil phase (0.1s) ──
            // Muzzle flash plays, barrel kicks back
            muzzleSequence.AppendCallback(() =>
            {
                muzzleFlash.Play(true);
            });
            muzzleSequence.Append(
                muzzleTransform
                    .DOLocalMove(muzzleEndPosition, 0.1f)
                    .SetEase(Ease.OutSine)
            );

            // ── Recovery phase (0.1s) ──
            // Barrel returns, flash stops, scale punch plays
            muzzleSequence.AppendCallback(() =>
            {
                muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            });
            muzzleSequence.Append(
                muzzleTransform
                    .DOLocalMove(muzzleStartPosition, 0.1f)
                    .SetEase(Ease.OutBack)
            );
            muzzleSequence.Append(
                muzzleTransform
                    .DOScale(muzzleEndScale, 0.05f)
                    .SetEase(Ease.OutSine)
            );
            muzzleSequence.Append(
                muzzleTransform
                    .DOScale(muzzleStartScale, 0.05f)
                    .SetEase(Ease.InSine)
            );

            // Ensure clean state if sequence gets killed mid-way
            muzzleSequence.OnKill(() =>
            {
                muzzleTransform.localPosition = muzzleStartPosition;
                muzzleTransform.localScale = muzzleStartScale;
                muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            });

            muzzleSequence.Play();

            // base.Shoot() spawns bullets AND plays fireParticles
            // So don't manually play fireParticles in the sequence — let base handle it
            base.Shoot();
        }
        private void OnDrawGizmos()
        {
            if (wheels == null) return;
            foreach (var wheel in wheels)
            {
                if (wheel != null)
                {
#if UNITY_EDITOR
                    Handles.DrawWireDisc(wheel.position, Vector3.back, wheelRadius);
#endif
                }
            }
        }
    }
}