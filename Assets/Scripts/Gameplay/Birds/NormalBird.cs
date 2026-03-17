using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Spine.Unity;
using UnityEngine;

namespace Gameplay.Birds
{
    public class NormalBird : BaseBird
    {
        private SkeletonAnimation skeletonAnimation;
        private Spine.AnimationState animationState;

        private string currentAnimation;

        // Animation names (safe constants)
        public const string FLY_NORMAL = "fly_N";
        public const string FLY_FAST = "fly_Fast";
        public const string LAY_EGG = "lay_egg";
        public const string DEATH = "death";
        
        private void PlayAnimation(string animationName, bool loop)
        {
            if (currentAnimation == animationName)
                return;

            animationState.SetAnimation(0, animationName, loop);
            currentAnimation = animationName;
        }
        
        private void Awake()
        {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
            animationState = skeletonAnimation.state;
        }

        private void Start()
        {
            FlyNormal();
        }

        private void OnEnable()
        {
            OnLayEgg += LayEgg;
            OnDestroyed += Death;
        }

        void FlyNormal()
        {
            PlayAnimation(FLY_NORMAL, true);
        }

        public void FlyFast()
        {
            PlayAnimation(FLY_FAST, true);
        }

        public void LayEgg(BaseBird bird)
        {
            animationState.SetAnimation(0, LAY_EGG, false);
            animationState.AddAnimation(0, FLY_NORMAL, true, 0f);
            currentAnimation = FLY_NORMAL;
        }

        public void Death(BaseBird bird)
        {
            PlayAnimation(DEATH, false);
        }

    }
}
