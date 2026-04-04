using DG.Tweening;
using Gameplay.VFX;
using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Triple-barrel cannon. Center barrel fires straight,
    /// left and right barrels fire at spread angles.
    /// Each barrel has its own independent recoil and muzzle flash.
    /// </summary>
    public class TripleCannon : BaseCannon
    {
        [Header("Spread Settings")]
        [SerializeField, Range(0f, 45f)] private float spreadAngle = 5f;

        [Header("Muzzle Flash")]
        [SerializeField] private GameObject muzzleFlashPrefab;

        [Header("Left Muzzle")]
        [SerializeField] private Transform leftMuzzleTransform;
        [SerializeField] private Transform leftMuzzleFlashPoint;

        [Header("Center Muzzle")]
        [SerializeField] private Transform centerMuzzleTransform;
        [SerializeField] private Transform centerMuzzleFlashPoint;

        [Header("Right Muzzle")]
        [SerializeField] private Transform rightMuzzleTransform;
        [SerializeField] private Transform rightMuzzleFlashPoint;

        [Header("Recoil Settings")]
        [SerializeField] private float recoilYOffset = -0.25f;
        [SerializeField] private Vector3 recoilEndScale = new(1.1f, 0.9f, 1f);
        [SerializeField] private float recoilDuration = 0.1f;
        [SerializeField] private float recoveryDuration = 0.1f;

        [Header("Hit VFX")]
        [SerializeField] private GameObject hitVFXPrefab;
        [SerializeField] private Transform hitVFXPoint;

        [Header("Death VFX")]
        [SerializeField] private GameObject deathVFXPrefab;
        [SerializeField] private Transform deathVFXPoint;

        // ── Cached start states ──
        private Vector3 leftStartPos, leftStartScale;
        private Vector3 centerStartPos, centerStartScale;
        private Vector3 rightStartPos, rightStartScale;

        // ── Independent recoil sequences ──
        private Sequence leftSequence;
        private Sequence centerSequence;
        private Sequence rightSequence;

        protected override void Awake()
        {
            base.Awake();

            if (leftMuzzleTransform != null)
            {
                leftStartPos = leftMuzzleTransform.localPosition;
                leftStartScale = leftMuzzleTransform.localScale;
            }

            if (centerMuzzleTransform != null)
            {
                centerStartPos = centerMuzzleTransform.localPosition;
                centerStartScale = centerMuzzleTransform.localScale;
            }

            if (rightMuzzleTransform != null)
            {
                rightStartPos = rightMuzzleTransform.localPosition;
                rightStartScale = rightMuzzleTransform.localScale;
            }
        }

        protected override void Shoot()
        {
            if (gunTips == null || gunTips.Length < 3)
            {
                base.Shoot();
                return;
            }

            // ── Spawn bullets: left angled, center straight, right angled ──
            Quaternion leftRot = gunTips[0].rotation * Quaternion.Euler(0f, 0f, spreadAngle);
            SpawnBullet(gunTips[0].position, leftRot);

            SpawnBullet(gunTips[1].position, gunTips[1].rotation);

            Quaternion rightRot = gunTips[2].rotation * Quaternion.Euler(0f, 0f, -spreadAngle);
            SpawnBullet(gunTips[2].position, rightRot);

            // ── Recoil all three barrels independently ──
            PlayBarrelRecoil(
                leftMuzzleTransform, leftStartPos, leftStartScale,
                leftMuzzleFlashPoint, ref leftSequence
            );

            PlayBarrelRecoil(
                centerMuzzleTransform, centerStartPos, centerStartScale,
                centerMuzzleFlashPoint, ref centerSequence
            );

            PlayBarrelRecoil(
                rightMuzzleTransform, rightStartPos, rightStartScale,
                rightMuzzleFlashPoint, ref rightSequence
            );

            OnShootVFX();
        }

        private void PlayBarrelRecoil(
            Transform muzzle, Vector3 startPos, Vector3 startScale,
            Transform flashPoint, ref Sequence sequence)
        {
            if (muzzle == null) return;

            sequence?.Kill();
            muzzle.localPosition = startPos;
            muzzle.localScale = startScale;

            ParticleSystem capturedFlash = null;
            Vector3 recoilTarget = startPos + new Vector3(0f, recoilYOffset, 0f);

            Sequence seq = DOTween.Sequence();

            // ── Flash + kick ──
            seq.AppendCallback(() =>
            {
                if (muzzleFlashPrefab != null && flashPoint != null)
                    capturedFlash = VFXPoolManager.Instance.PlayAttached(muzzleFlashPrefab, flashPoint);
            });

            seq.Append(
                muzzle.DOLocalMove(recoilTarget, recoilDuration)
                    .SetEase(Ease.OutSine)
            );

            // ── Stop flash + return ──
            seq.AppendCallback(() =>
            {
                if (capturedFlash != null && muzzleFlashPrefab != null)
                {
                    VFXPoolManager.Instance.StopAndReturn(muzzleFlashPrefab, capturedFlash);
                    capturedFlash = null;
                }
            });

            seq.Append(
                muzzle.DOLocalMove(startPos, recoveryDuration)
                    .SetEase(Ease.OutBack)
            );

            // ── Scale punch ──
            seq.Append(
                muzzle.DOScale(recoilEndScale, 0.05f)
                    .SetEase(Ease.OutSine)
            );
            seq.Append(
                muzzle.DOScale(startScale, 0.05f)
                    .SetEase(Ease.InSine)
            );

            seq.OnKill(() =>
            {
                muzzle.localPosition = startPos;
                muzzle.localScale = startScale;

                if (capturedFlash != null && muzzleFlashPrefab != null)
                {
                    VFXPoolManager.Instance.StopAndReturn(muzzleFlashPrefab, capturedFlash);
                    capturedFlash = null;
                }
            });

            seq.Play();
            sequence = seq;
        }

        // ════════════════════════════════════════════════════════
        //  VFX HOOKS
        // ════════════════════════════════════════════════════════

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