using DG.Tweening;
using Gameplay.VFX;
using UnityEngine;

namespace Gameplay.Player
{
    public class DoubleCannon : BaseCannon
    {
        [Header("Spread Settings")]
        [SerializeField, Range(0f, 45f)] private float spreadAngle = 5f;

        [Header("Muzzle Flash (0=Left, 1=Right)")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private Transform[] muzzleFlashPoints = new Transform[2];

        [Header("Muzzle Recoil (0=Left, 1=Right)")]
        [SerializeField] private Transform[] muzzleTransforms = new Transform[2];
        [SerializeField] private float recoilYOffset = -0.25f;
        [SerializeField] private Vector3 recoilEndScale;
        [SerializeField] private float recoilDuration = 0.1f;
        [SerializeField] private float recoveryDuration = 0.1f;

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

        private Vector3[] muzzleStartPositions = new Vector3[2];
        private Vector3[] muzzleStartScales = new Vector3[2];
        private Sequence[] muzzleSequences = new Sequence[2];
        private ParticleSystem[] _currentFlashes = new ParticleSystem[2];

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < muzzleTransforms.Length; i++)
            {
                if (muzzleTransforms[i] != null)
                {
                    muzzleStartPositions[i] = muzzleTransforms[i].localPosition;
                    muzzleStartScales[i] = muzzleTransforms[i].localScale;
                }
            }
        }

        protected override void Shoot()
        {
            if (gunTips == null || gunTips.Length < 2)
            {
                base.Shoot();
                return;
            }

            // Left barrel: positive spread, Right barrel: negative spread
            float[] spreads = { spreadAngle, -spreadAngle };

            for (int i = 0; i < 2; i++)
            {
                if (gunTips[i] == null) continue;
                Quaternion rot = gunTips[i].rotation * Quaternion.Euler(0f, 0f, spreads[i]);
                SpawnBullet(gunTips[i].position, rot);
                PlayMuzzleEffect(i);
            }

            OnShootVFX();
        }

        private void PlayMuzzleEffect(int i)
        {
            Transform muzzle = muzzleTransforms[i];
            if (muzzle == null) return;

            muzzleSequences[i]?.Kill();
            muzzle.localPosition = muzzleStartPositions[i];
            muzzle.localScale = muzzleStartScales[i];

            Sequence seq = DOTween.Sequence();

            seq.AppendCallback(() =>
            {
                if (muzzleFlashPrefab != null && i < muzzleFlashPoints.Length && muzzleFlashPoints[i] != null)
                    _currentFlashes[i] = VFXPoolManager.Instance.PlayAttached(muzzleFlashPrefab, muzzleFlashPoints[i]);
            });

            seq.Append(muzzle.DOLocalMove(muzzleStartPositions[i] + new Vector3(0f, recoilYOffset, 0f), recoilDuration).SetEase(Ease.OutSine));

            seq.AppendCallback(() =>
            {
                if (_currentFlashes[i] != null && muzzleFlashPrefab != null)
                {
                    VFXPoolManager.Instance.StopAndReturn(muzzleFlashPrefab, _currentFlashes[i]);
                    _currentFlashes[i] = null;
                }
            });

            seq.Append(muzzle.DOLocalMove(muzzleStartPositions[i], recoveryDuration).SetEase(Ease.OutBack));
            seq.Append(muzzle.DOScale(recoilEndScale, 0.05f).SetEase(Ease.OutSine));
            seq.Append(muzzle.DOScale(muzzleStartScales[i], 0.05f).SetEase(Ease.InSine));

            seq.OnKill(() =>
            {
                muzzle.localPosition = muzzleStartPositions[i];
                muzzle.localScale = muzzleStartScales[i];
                if (_currentFlashes[i] != null && muzzleFlashPrefab != null)
                {
                    VFXPoolManager.Instance.StopAndReturn(muzzleFlashPrefab, _currentFlashes[i]);
                    _currentFlashes[i] = null;
                }
            });

            muzzleSequences[i] = seq;
            seq.Play();
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