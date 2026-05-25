using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Gameplay.Events;
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

            // Flap SFX tracks fly_N specifically. Transitions in/out of fly_N drive the
            // looping AudioSource that SFXController attaches to the bird.
            bool wasFlapping = currentAnimation == FLY_NORMAL;
            bool isFlapping = animationName == FLY_NORMAL;
            if (isFlapping && !wasFlapping) GameEvents.FireBirdFlapStart(this);
            else if (wasFlapping && !isFlapping) GameEvents.FireBirdFlapStop(this);

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

        // Normal birds can't be killed by the player and don't play a death animation —
        // they simply despawn (lifetime/flee). See BaseBird.IsInvincible / PlaysDeathAnimation.
        protected override bool IsInvincible => true;
        protected override bool PlaysDeathAnimation => false;

        private void OnEnable()
        {
            OnLayEgg += LayEgg;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnLayEgg -= LayEgg;

            // Guarantee the flap loop is torn down if the bird is disabled mid-flight
            // (pooling, scene unload) before any animation transition fires it for us.
            if (currentAnimation == FLY_NORMAL)
            {
                GameEvents.FireBirdFlapStop(this);
                currentAnimation = null;
            }
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
            // animationState.SetAnimation(0, LAY_EGG, false);
            // animationState.AddAnimation(0, FLY_NORMAL, true, 0f);
            
            currentAnimation = FLY_NORMAL;
        }

    }
}
