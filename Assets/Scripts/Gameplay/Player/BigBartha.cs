using DG.Tweening;
using Gameplay.VFX;
using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Heavy single-barrel cannon. Same structure as SingleCannon
    /// but with slower, heavier recoil timing for a weightier feel.
    /// </summary>
    public class BigBerthaCannon : BaseCannon
    {
        [Header("Muzzle Flash")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private Transform muzzleFlashPoint;

        [Header("Recoil")]
        [SerializeField] private Transform muzzleTransform;
        [SerializeField] private Vector3 recoilEndPosition;
        [SerializeField] private Vector3 recoilEndScale;
        [SerializeField] private float recoilDuration = 0.15f;
        [SerializeField] private float recoveryDuration = 0.2f;

        [Header("Hit VFX")]
        [SerializeField] private GameObject hitVFXPrefab;
        [SerializeField] private Transform hitVFXPoint;

        [Header("Death VFX")]
        [SerializeField] private GameObject deathVFXPrefab;
        [SerializeField] private Transform deathVFXPoint;

        private Vector3 muzzleStartPosition;
        private Vector3 muzzleStartScale;
        private Sequence recoilSequence;
        private ParticleSystem _currentFlash;

        protected override void Awake()
        {
            base.Awake();

            if (muzzleTransform != null)
            {
                muzzleStartPosition = muzzleTransform.localPosition;
                muzzleStartScale = muzzleTransform.localScale;
            }
        }

        protected override void Shoot()
        {
            recoilSequence?.Kill();

            if (muzzleTransform != null)
            {
                muzzleTransform.localPosition = muzzleStartPosition;
                muzzleTransform.localScale = muzzleStartScale;
            }

            recoilSequence = DOTween.Sequence();

            // ── Recoil phase ──
            recoilSequence.AppendCallback(() =>
            {
                if (muzzleFlashPrefab != null && muzzleFlashPoint != null)
                {
                    _currentFlash = VFXPoolManager.Instance.PlayAttached(
                        muzzleFlashPrefab, muzzleFlashPoint);
                }
            });

            if (muzzleTransform != null)
            {
                recoilSequence.Append(
                    muzzleTransform
                        .DOLocalMove(recoilEndPosition, recoilDuration)
                        .SetEase(Ease.OutExpo)
                );
            }

            // ── Recovery phase ──
            recoilSequence.AppendCallback(() =>
            {
                if (_currentFlash != null && muzzleFlashPrefab != null)
                {
                    VFXPoolManager.Instance.StopAndReturn(muzzleFlashPrefab, _currentFlash);
                    _currentFlash = null;
                }
            });

            if (muzzleTransform != null)
            {
                recoilSequence.Append(
                    muzzleTransform
                        .DOLocalMove(muzzleStartPosition, recoveryDuration)
                        .SetEase(Ease.OutBack)
                );
                recoilSequence.Append(
                    muzzleTransform
                        .DOScale(recoilEndScale, 0.06f)
                        .SetEase(Ease.OutSine)
                );
                recoilSequence.Append(
                    muzzleTransform
                        .DOScale(muzzleStartScale, 0.08f)
                        .SetEase(Ease.InSine)
                );
            }

            recoilSequence.OnKill(() =>
            {
                if (muzzleTransform != null)
                {
                    muzzleTransform.localPosition = muzzleStartPosition;
                    muzzleTransform.localScale = muzzleStartScale;
                }

                if (_currentFlash != null && muzzleFlashPrefab != null)
                {
                    VFXPoolManager.Instance.StopAndReturn(muzzleFlashPrefab, _currentFlash);
                    _currentFlash = null;
                }
            });

            recoilSequence.Play();

            base.Shoot();
        }

        protected override void OnShootVFX() { }

        protected override void OnDamageTakenVFX(int damage)
        {
            if (hitVFXPrefab == null || hitVFXPoint == null) return;
            VFXPoolManager.Instance.Play(hitVFXPrefab, hitVFXPoint.position);
        }

        protected override void OnDeathVFX()
        {
            if (deathVFXPrefab == null) return;

            Vector3 pos = deathVFXPoint != null
                ? deathVFXPoint.position
                : transform.position;

            VFXPoolManager.Instance.Play(deathVFXPrefab, pos);
        }

        private void OnDrawGizmos()
        {
            if (wheels == null) return;
            foreach (var wheel in wheels)
            {
                if (wheel != null)
                {
#if UNITY_EDITOR
                    UnityEditor.Handles.DrawWireDisc(wheel.position, Vector3.back, wheelRadius);
#endif
                }
            }
        }
    }
}