using DG.Tweening;
using Gameplay.VFX;
using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Dual-barrel cannon. Both barrels fire simultaneously at a spread angle,
    /// each with its own independent recoil animation and muzzle flash.
    /// </summary>
    public class DoubleCannon : BaseCannon
    {
        [Header("Spread Settings")]
        [SerializeField, Range(0f, 45f)] private float spreadAngle = 5f;

        [Header("Muzzle Flash")]
        [SerializeField] private GameObject muzzleFlashPrefab;

        [Header("Left Muzzle")]
        [SerializeField] private Transform leftMuzzleTransform;
        [SerializeField] private Transform leftMuzzleFlashPoint;

        [Header("Right Muzzle")]
        [SerializeField] private Transform rightMuzzleTransform;
        [SerializeField] private Transform rightMuzzleFlashPoint;

        [Header("Recoil Settings")]
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

        // ── Cached start states ──
        private Vector3 leftStartPosition;
        private Vector3 leftStartScale;
        private Vector3 rightStartPosition;
        private Vector3 rightStartScale;

        // ── Recoil sequences (independent per barrel) ──
        private Sequence leftRecoilSequence;
        private Sequence rightRecoilSequence;

        protected override void Awake()
        {
            base.Awake();

            if (leftMuzzleTransform != null)
            {
                leftStartPosition = leftMuzzleTransform.localPosition;
                leftStartScale = leftMuzzleTransform.localScale;
            }

            if (rightMuzzleTransform != null)
            {
                rightStartPosition = rightMuzzleTransform.localPosition;
                rightStartScale = rightMuzzleTransform.localScale;
            }
        }

        protected override void Shoot()
        {
            if (gunTips == null || gunTips.Length < 2)
            {
                base.Shoot();
                return;
            }

            // ── Spawn bullets with spread ──
            Quaternion leftRotation = gunTips[0].rotation * Quaternion.Euler(0f, 0f, spreadAngle);
            SpawnBullet(gunTips[0].position, leftRotation);

            Quaternion rightRotation = gunTips[1].rotation * Quaternion.Euler(0f, 0f, -spreadAngle);
            SpawnBullet(gunTips[1].position, rightRotation);

            // ── Recoil both barrels independently ──
            PlayBarrelRecoil(
                leftMuzzleTransform, leftStartPosition, leftStartScale,
                leftMuzzleFlashPoint, ref leftRecoilSequence
            );

            PlayBarrelRecoil(
                rightMuzzleTransform, rightStartPosition, rightStartScale,
                rightMuzzleFlashPoint, ref rightRecoilSequence
            );

            OnShootVFX();
        }

        /// <summary>
        /// Plays a full recoil cycle on one barrel.
        /// Flash → kick → stop flash → return → scale punch.
        /// Each barrel runs its own independent sequence.
        /// </summary>
        private void PlayBarrelRecoil(
            Transform muzzle, Vector3 startPos, Vector3 startScale,
            Transform flashPoint, ref Sequence sequence)
        {
            if (muzzle == null) return;

            sequence?.Kill();
            muzzle.localPosition = startPos;
            muzzle.localScale = startScale;

            // Local variable for closure — ref fields can't be captured in lambdas
            ParticleSystem capturedFlash = null;

            Sequence seq = DOTween.Sequence();

            // ── Flash + kick ──
            seq.AppendCallback(() =>
            {
                if (muzzleFlashPrefab != null && flashPoint != null)
                    capturedFlash = VFXPoolManager.Instance.PlayAttached(muzzleFlashPrefab, flashPoint);
            });

            seq.Append(
      muzzle.DOLocalMove(startPos + new Vector3(0f, recoilYOffset, 0f), recoilDuration)
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

            // Safety reset if killed mid-way
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