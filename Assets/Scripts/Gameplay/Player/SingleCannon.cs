using DG.Tweening;
using Gameplay.Events;
using Gameplay.VFX;
using UnityEngine;

namespace Gameplay.Player
{
    public class SingleCannon : BaseCannon
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

        [Header("Death Animation")]
        [SerializeField] private Transform middleTransform;
        [SerializeField] private Transform leftWheel;
        [SerializeField] private Transform rightWheel;
        [SerializeField] private float wheelFlyDistance = 2f;
        [SerializeField] private float wheelHopHeight = 1f;
        [SerializeField] private float wheelSpin = 720f;

        [Header("Power-Up VFX")]
        [SerializeField] private GameObject healVFXPrefab;
        [SerializeField] private GameObject shieldAbsorbVFXPrefab;
        [SerializeField] private GameObject reviveVFXPrefab;

        private Vector3 muzzleStartPosition;
        private Vector3 muzzleStartScale;
        private Quaternion muzzleStartRotation;
        private Vector3 middleStartScale;
        private Quaternion middleStartRotation;
        private Vector3 leftWheelStartPos, rightWheelStartPos;
        private Quaternion leftWheelStartRot, rightWheelStartRot;
        private Transform leftWheelParent, rightWheelParent;

        private Sequence muzzleSequence;
        private Sequence deathSequence;
        private ParticleSystem _currentFlash;

        protected override void Awake()
        {
            base.Awake();
            if (muzzleTransform != null)
            {
                muzzleStartPosition = muzzleTransform.localPosition;
                muzzleStartScale = muzzleTransform.localScale;
                muzzleStartRotation = muzzleTransform.localRotation;
            }
            if (middleTransform != null)
            {
                middleStartScale = middleTransform.localScale;
                middleStartRotation = middleTransform.localRotation;
            }
            if (leftWheel != null)
            {
                leftWheelParent = leftWheel.parent;
                leftWheelStartPos = leftWheel.localPosition;
                leftWheelStartRot = leftWheel.localRotation;
            }
            if (rightWheel != null)
            {
                rightWheelParent = rightWheel.parent;
                rightWheelStartPos = rightWheel.localPosition;
                rightWheelStartRot = rightWheel.localRotation;
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

        protected override bool DeferDeathEvent => true;

        protected override void OnDeathVFX()
        {
            if (deathVFXPrefab != null)
            {
                Vector3 pos = deathVFXPoint != null ? deathVFXPoint.position : transform.position;
                VFXPoolManager.Instance.Play(deathVFXPrefab, pos);
            }
            PlayDeathAnimation();
        }

        private void PlayDeathAnimation()
        {
            muzzleSequence?.Kill();
            deathSequence?.Kill();

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            deathSequence = DOTween.Sequence();
            deathSequence.Append(transform.DOShakePosition(0.3f, 0.15f, 20));

            if (leftWheel != null)
            {
                leftWheel.SetParent(null, true);
                deathSequence.Join(leftWheel.DOMoveX(leftWheel.position.x - wheelFlyDistance, 1.6f).SetEase(Ease.OutQuad));
                deathSequence.Join(leftWheel.DORotate(new Vector3(0, 0, wheelSpin), 1.6f, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
            }

            if (rightWheel != null)
            {
                rightWheel.SetParent(null, true);
                deathSequence.Join(rightWheel.DOMoveX(rightWheel.position.x + wheelFlyDistance, 1.6f).SetEase(Ease.OutQuad));
                deathSequence.Join(rightWheel.DORotate(new Vector3(0, 0, -wheelSpin), 1.6f, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
            }

            if (muzzleTransform != null)
            {
                deathSequence.Join(muzzleTransform.DOLocalMoveY(muzzleStartPosition.y + 0.4f, 0.45f).SetEase(Ease.OutQuad));
                deathSequence.Insert(0.45f, muzzleTransform.DOLocalMoveY(muzzleStartPosition.y - 0.6f, 1.15f).SetEase(Ease.InQuad));
                deathSequence.Join(muzzleTransform.DOLocalRotate(new Vector3(0, 0, 75f), 1.6f).SetEase(Ease.InQuad));
            }

            if (middleTransform != null)
            {
                deathSequence.Join(middleTransform.DOScaleY(middleStartScale.y * 0.6f, 0.45f).SetEase(Ease.OutQuad));
                deathSequence.Join(middleTransform.DOLocalRotate(new Vector3(0, 0, 8f), 0.7f).SetEase(Ease.OutSine));
            }

            deathSequence.OnComplete(RaisePlayerDeath);
        }

        private void RestoreParts()
        {
            deathSequence?.Kill();

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;

            if (leftWheel != null)
            {
                leftWheel.SetParent(leftWheelParent, false);
                leftWheel.localPosition = leftWheelStartPos;
                leftWheel.localRotation = leftWheelStartRot;
            }
            if (rightWheel != null)
            {
                rightWheel.SetParent(rightWheelParent, false);
                rightWheel.localPosition = rightWheelStartPos;
                rightWheel.localRotation = rightWheelStartRot;
            }
            if (muzzleTransform != null)
            {
                muzzleTransform.localPosition = muzzleStartPosition;
                muzzleTransform.localRotation = muzzleStartRotation;
                muzzleTransform.localScale = muzzleStartScale;
            }
            if (middleTransform != null)
            {
                middleTransform.localScale = middleStartScale;
                middleTransform.localRotation = middleStartRotation;
            }
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
            RestoreParts();
            if (reviveVFXPrefab != null)
                VFXPoolManager.Instance.Play(reviveVFXPrefab, transform.position);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            muzzleSequence?.Kill();
            deathSequence?.Kill();
        }
    }
}