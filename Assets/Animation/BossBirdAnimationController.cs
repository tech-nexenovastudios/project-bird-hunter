using UnityEngine;
using Spine.Unity;

public class BossBirdAnimationController : MonoBehaviour
{
    private SkeletonAnimation skeletonAnimation;
    private Spine.AnimationState animationState;
    private string currentAnimation;

    // Animation names
    public const string ATTACK = "Attack";
    public const string FLY = "Fly";
    public const string FLEW = "Flew";
    public const string LAY_EGG = "Lay_egg";
    public const string DEATH = "Death";
    public const string IDLE = "Idle";
    public const string HIT = "Take_damage";

    // Blend durations
    [SerializeField] private float defaultBlendDuration = 0.2f;
    [SerializeField] private float attackBlendDuration = 0.1f;
    [SerializeField] private float deathBlendDuration = 0.3f;
    [SerializeField] private float damageBlendDuration = 0.1f;

    void Awake()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        animationState = skeletonAnimation.AnimationState;
    }

    void Start()
    {
        PlayAnimation(FLY, true, defaultBlendDuration);
    }

    private void PlayAnimation(string animationName, bool loop, float blendDuration)
    {
        if (currentAnimation == animationName)
            return;

        var trackEntry = animationState.SetAnimation(0, animationName, loop);
        if (trackEntry != null)
            trackEntry.MixDuration = blendDuration;

        currentAnimation = animationName;
    }

    private void PlayAnimationThenQueue(string animationName, string nextAnimation, float blendDuration)
    {
        var trackEntry = animationState.SetAnimation(0, animationName, false);
        if (trackEntry != null)
        {
            trackEntry.MixDuration = blendDuration;
            animationState.AddAnimation(0, nextAnimation, true, 0f);
        }

        currentAnimation = nextAnimation;
    }

    public void Idle() => PlayAnimation(IDLE, true, defaultBlendDuration);
    public void FlyNormal() => PlayAnimation(FLY, true, defaultBlendDuration);
    public void FlyFast() => PlayAnimation(FLEW, true, defaultBlendDuration);
    public void Attack() => PlayAnimation(ATTACK, false, attackBlendDuration);
    public void Hit() => PlayAnimation(HIT, false, damageBlendDuration);
    public void Death() => PlayAnimation(DEATH, false, deathBlendDuration);
    public void LayEgg() => PlayAnimationThenQueue(LAY_EGG, FLY, defaultBlendDuration);

    [Header("Debug")]
    public bool testFly;
    public bool testFast;
    public bool testEgg;
    public bool testDeath;
    public bool testHit;
    public bool testIdle;
    public bool testAttack;

    void Update()
    {
        if (testFly) { testFly = false; FlyNormal(); }
        if (testFast) { testFast = false; FlyFast(); }
        if (testEgg) { testEgg = false; LayEgg(); }
        if (testDeath) { testDeath = false; Death(); }
        if (testAttack) { testAttack = false; Attack(); }
        if (testIdle) { testIdle = false; Idle(); }
        if (testHit) { testHit = false; Hit(); }
    }
}
