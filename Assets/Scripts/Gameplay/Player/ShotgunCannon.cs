using DG.Tweening;
using Gameplay.VFX;
using UnityEngine;

namespace Gameplay.Player
{
    public class ShotgunCannon : BaseCannon
    {
        private const int TIP_COUNT = 4;
        private const int MUZZLE_COUNT = 2;

        [Header("Spread Settings (degrees per gun tip, index 0..3)")]
      

        [Header("Muzzle Flash (0=Left, 1=Right)")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private Transform[] muzzleFlashPoints = new Transform[MUZZLE_COUNT];

        [Header("Muzzle Recoil (0=Left, 1=Right)")]
        [SerializeField] private Transform[] muzzleTransforms = new Transform[MUZZLE_COUNT];
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

        private Vector3[] muzzleStartPositions = new Vector3[MUZZLE_COUNT];
        private Vector3[] muzzleStartScales = new Vector3[MUZZLE_COUNT];
        private Sequence[] muzzleSequences = new Sequence[MUZZLE_COUNT];
        private ParticleSystem[] _currentFlashes = new ParticleSystem[MUZZLE_COUNT];

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
            if (gunTips == null || gunTips.Length < TIP_COUNT)
            {
                base.Shoot();
                return;
            }

            // Fire all 4 gun tips (middle tips just spawn bullets, no visual)
            for (int i = 0; i < TIP_COUNT; i++)
            {
                if (gunTips[i] == null) continue;
           
                SpawnBullet(gunTips[i].position, gunTips[i].rotation);
            }

            // Only the 2 outer muzzles play recoil + flash
            for (int m = 0; m < MUZZLE_COUNT; m++)
                PlayMuzzleEffect(m);

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