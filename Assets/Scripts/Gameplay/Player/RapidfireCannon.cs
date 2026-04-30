using DG.Tweening;
using Gameplay.VFX;
using UnityEngine;

namespace Gameplay.Player
{
    public class RapidfireCannon : BaseCannon
    {
        [Header("Muzzle Flash")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private Transform muzzleFlashPoint;

        [Header("Muzzle Recoil")]
        [SerializeField] private Transform muzzleTransform;
        [SerializeField] private Vector3 muzzleEndPosition;
        [SerializeField] private Vector3 muzzleEndScale;

        [Header("Hit VFX")]
        [SerializeField] private GameObject hitVFXPrefab;
        [SerializeField] private Transform hitVFXPoint;

        [Header("Death VFX")]
        [SerializeField] private GameObject deathVFXPrefab;
        [SerializeField] private Transform deathVFXPoint;

        [Header("Power-Up VFX")]
        [SerializeField] private GameObject healVFXPrefab;
        [SerializeField] private GameObject shieldAbsorbVFXPrefab;
        [SerializeField] private GameObject reviveVFXPrefab;

        private Vector3 muzzleStartPosition;
        private Vector3 muzzleStartScale;
        private Sequence muzzleSequence;
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
            muzzleSequence?.Kill();

            if (muzzleTransform != null)
            {
                muzzleTransform.localPosition = muzzleStartPosition;
                muzzleTransform.localScale = muzzleStartScale;
            }

            muzzleSequence = DOTween.Sequence();

            muzzleSequence.AppendCallback(() =>
            {
                if (muzzleFlashPrefab != null && muzzleFlashPoint != null)
                    _currentFlash = VFXPoolManager.Instance.PlayAttached(muzzleFlashPrefab, muzzleFlashPoint);
            });

            if (muzzleTransform != null)
                muzzleSequence.Append(muzzleTransform.DOLocalMove(muzzleEndPosition, 0.1f).SetEase(Ease.OutSine));

            muzzleSequence.AppendCallback(() =>
            {
                if (_currentFlash != null && muzzleFlashPrefab != null)
                {
                    VFXPoolManager.Instance.StopAndReturn(muzzleFlashPrefab, _currentFlash);
                    _currentFlash = null;
                }
            });

            if (muzzleTransform != null)
            {
                muzzleSequence.Append(muzzleTransform.DOLocalMove(muzzleStartPosition, 0.1f).SetEase(Ease.OutBack));
                muzzleSequence.Append(muzzleTransform.DOScale(muzzleEndScale, 0.05f).SetEase(Ease.OutSine));
                muzzleSequence.Append(muzzleTransform.DOScale(muzzleStartScale, 0.05f).SetEase(Ease.InSine));
            }

            muzzleSequence.OnKill(() =>
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

            muzzleSequence.Play();
            base.Shoot();

        }

        protected override void OnShootVFX() { }

        protected override void OnDamageTakenVFX(int damage)
        {
            if (hitVFXPrefab != null && hitVFXPoint != null)
                VFXPoolManager.Instance.Play(hitVFXPrefab, hitVFXPoint.position);
        }

        protected override void OnDeathVFX()
        {
            if (deathVFXPrefab == null) return;
            Vector3 pos = deathVFXPoint != null ? deathVFXPoint.position : transform.position;
            VFXPoolManager.Instance.Play(deathVFXPrefab, pos);
        }

        protected override void OnHealVFX(int amount)
        {
            Vector3 spawnPos = transform.position;
            spawnPos.y = -3.75f;
            if (healVFXPrefab != null)
                VFXPoolManager.Instance.Play(healVFXPrefab, spawnPos);
        }

        protected override void OnShieldAbsorbVFX()
        {
            if (shieldAbsorbVFXPrefab != null)
                VFXPoolManager.Instance.Play(shieldAbsorbVFXPrefab, transform.position);
        }

        protected override void OnReviveVFX()
        {
            if (reviveVFXPrefab != null)
                VFXPoolManager.Instance.Play(reviveVFXPrefab, transform.position);
        }
    }
}