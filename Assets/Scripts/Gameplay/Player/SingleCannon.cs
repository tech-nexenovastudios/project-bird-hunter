using System;
using DG.Tweening;
using Gameplay.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay.Player
{
    public class SingleCannon : BaseCannon
    {
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private Transform muzzleTransform;
        
        private Vector3 muzzleStartPosition;
        [SerializeField] private Vector3 muzzleEndPosition;
        
        private Vector3 muzzleStartScale;
        [SerializeField]private Vector3 muzzleEndScale;
        private float muzzleStartY;
        
        private Sequence muzzleSequence;

        protected override void Awake()
        {
            muzzleStartPosition = muzzleTransform.localPosition;
            muzzleStartScale = muzzleTransform.localScale;
            
            muzzleStartY = muzzleTransform.localPosition.y;
            
            muzzleTransform.localPosition = muzzleStartPosition;
            
            muzzleTransform.localScale = muzzleStartScale;
            
            muzzleTransform.gameObject.SetActive(true);
            muzzleFlash.gameObject.SetActive(false);
            muzzleFlash.Stop();
            muzzleFlash.Clear();
            base.Awake();
        }

        //protected override void UpdateUI()
        //{
            
        //}
        protected override void Shoot()
        {
            muzzleSequence?.Kill();
            muzzleSequence = null;
            
            muzzleSequence = DOTween.Sequence();
            
            muzzleSequence.AppendCallback(()=> muzzleFlash.Play());
            muzzleSequence.Append(muzzleTransform.DOLocalMove(muzzleEndPosition, 0.1f).SetEase(Ease.OutSine));
            muzzleSequence.AppendCallback(()=> muzzleTransform.localPosition = muzzleStartPosition);
            muzzleSequence.AppendCallback(() => muzzleFlash.Stop());
            muzzleSequence.AppendCallback(() => fireParticles[0].Play(true));
            muzzleSequence.Append(muzzleTransform.DOScale(muzzleEndScale, 0.1f).SetEase(Ease.Flash));
            muzzleSequence.AppendCallback(()=> muzzleTransform.localScale = muzzleStartScale );
            muzzleSequence.AppendCallback(() => fireParticles[0].Stop(true));
            
            muzzleSequence.Play();
            
            
            // if (muzzleTransform != null)
            // {
            //     muzzleFlash.gameObject.SetActive(true);
            //     
            //     muzzleTransform.localRotation = Quaternion.identity;
            //     muzzleTransform.localScale = Vector3.one;
            //     
            //     muzzleTransform.DOMoveY(-0.25f, 0.1f).SetEase(Ease.OutSine).OnComplete(() =>
            //     {
            //         muzzleTransform.DOMoveY(0, 0.1f).SetEase(Ease.OutSine);
            //     });
            //     
            //     muzzleTransform.DOPunchScale(new Vector3(1, 1.1f, 1), 0.1f).SetEase(Ease.Flash).OnStart(() =>
            //     {
            //         
            //         muzzleFlash.Play();
            //     }).OnComplete(() =>
            //     {
            //         muzzleTransform.DOPunchScale(new Vector3(1, 1, 1), 0.1f).SetEase(Ease.Flash);
            //         muzzleFlash.Stop();
            //         muzzleFlash.gameObject.SetActive(false);
            //     });
            // }
            
            base.Shoot();
        }

        private void OnDrawGizmos()
        {
            foreach (var wheel in wheels)
            {
                //Draw circle of wheel Radius
#if UNITY_EDITOR
                Handles.DrawWireDisc(wheel.position, Vector3.back, wheelRadius);
#endif
            }
        }
    }
}