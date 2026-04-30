using DG.Tweening;
using Gameplay.VFX;
using UnityEngine;

namespace Gameplay.Player
{
    public class LuckyCannon : BaseCannon
    {
        [Header("Muzzle Flash (3 muzzles: 0=Left, 1=Middle, 2=Right)")]
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private Transform[] muzzleFlashPoints = new Transform[3];

        [Header("Muzzle Recoil (3 muzzles: 0=Left, 1=Middle, 2=Right)")]
        [SerializeField] private Transform[] muzzleTransforms = new Transform[3];
        [SerializeField] private Vector3 muzzleEndPosition;
        [SerializeField] private Vector3 muzzleEndScale;

        [Header("Weights (Left, Middle, Right)")]
        [Range(0f, 1f)][SerializeField] private float leftWeight = 0.2f;
        [Range(0f, 1f)][SerializeField] private float middleWeight = 0.6f;
        [Range(0f, 1f)][SerializeField] private float rightWeight = 0.2f;

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

        private Vector3[] muzzleStartPositions = new Vector3[3];
        private Vector3[] muzzleStartScales = new Vector3[3];
        private Sequence[] muzzleSequences = new Sequence[3];
        private ParticleSystem[] _currentFlashes = new ParticleSystem[3];

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
            int index = PickWeightedIndex();

            if (gunTips != null && index < gunTips.Length && gunTips[index] != null)
                SpawnBullet(gunTips[index].position, gunTips[index].rotation);
            else
                SpawnBullet(transform.position, transform.rotation);

            PlayMuzzleEffect(index);
            OnShootVFX();
        }

        private int PickWeightedIndex()
        {
            float total = leftWeight + middleWeight + rightWeight;
            if (total <= 0f) return 1;

            float roll = Random.value * total;
            if (roll < leftWeight) return 0;
            if (roll < leftWeight + middleWeight) return 1;
            return 2;
        }

        private void PlayMuzzleEffect(int i)
        {
            muzzleSequences[i]?.Kill();

            Transform muzzle = muzzleTransforms[i];
            if (muzzle != null)
            {
                muzzle.localPosition = muzzleStartPositions[i];
                muzzle.localScale = muzzleStartScales[i];
            }

            Sequence seq = DOTween.Sequence();

            seq.AppendCallback(() =>
            {
                if (muzzleFlashPrefab != null && i < muzzleFlashPoints.Length && muzzleFlashPoints[i] != null)
                    _currentFlashes[i] = VFXPoolManager.Instance.PlayAttached(muzzleFlashPrefab, muzzleFlashPoints[i]);
            });

            if (muzzle != null)
                seq.Append(muzzle.DOLocalMove(muzzleEndPosition, 0.1f).SetEase(Ease.OutSine));

            seq.AppendCallback(() =>
            {
                if (_currentFlashes[i] != null && muzzleFlashPrefab != null)
                {
                    VFXPoolManager.Instance.StopAndReturn(muzzleFlashPrefab, _currentFlashes[i]);
                    _currentFlashes[i] = null;
                }
            });

            if (muzzle != null)
            {
                seq.Append(muzzle.DOLocalMove(muzzleStartPositions[i], 0.1f).SetEase(Ease.OutBack));
                seq.Append(muzzle.DOScale(muzzleEndScale, 0.05f).SetEase(Ease.OutSine));
                seq.Append(muzzle.DOScale(muzzleStartScales[i], 0.05f).SetEase(Ease.InSine));
            }

            seq.OnKill(() =>
            {
                if (muzzle != null)
                {
                    muzzle.localPosition = muzzleStartPositions[i];
                    muzzle.localScale = muzzleStartScales[i];
                }
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