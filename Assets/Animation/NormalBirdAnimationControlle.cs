using UnityEngine;
using Spine.Unity;

public class BirdSpineAnimationController : MonoBehaviour
{
    private SkeletonAnimation skeletonAnimation;
    private Spine.AnimationState animationState;

    private string currentAnimation;

    // Animation names (safe constants)
    public const string FLY_NORMAL = "fly_N";
    public const string FLY_FAST = "fly_Fast";
    public const string LAY_EGG = "lay_egg";
    public const string DEATH = "death";

    void Awake()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        animationState = skeletonAnimation.AnimationState;
    }

    void Start()
    {
        PlayAnimation(FLY_NORMAL, true);
    }

    void PlayAnimation(string animationName, bool loop)
    {
        if (currentAnimation == animationName)
            return;

        animationState.SetAnimation(0, animationName, loop);
        currentAnimation = animationName;
    }

    public void FlyNormal()
    {
        PlayAnimation(FLY_NORMAL, true);
    }

    public void FlyFast()
    {
        PlayAnimation(FLY_FAST, true);
    }

    public void LayEgg()
    {
        animationState.SetAnimation(0, LAY_EGG, false);
        animationState.AddAnimation(0, FLY_NORMAL, true, 0f);
        currentAnimation = FLY_NORMAL;
    }

    public void Death()
    {
        PlayAnimation(DEATH, false);
    }

    [Header("Debug")]
    public bool testFly;
    public bool testFast;
    public bool testEgg;
    public bool testDeath;

    void Update()
    {
        if (testFly) { testFly = false; FlyNormal(); }
        if (testFast) { testFast = false; FlyFast(); }
        if (testEgg) { testEgg = false; LayEgg(); }
        if (testDeath) { testDeath = false; Death(); }
    }
}