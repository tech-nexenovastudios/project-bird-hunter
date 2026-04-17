using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Event Bus")]
public class BossEventBus : ScriptableObject
{
    private static BossEventBus instance;
    public static BossEventBus Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<BossEventBus>("BossEventBus");
            return instance;
        }
    }
    //boss spawn events
    public System.Action<string, float> OnBossRetreated;
    public static void RaiseBossRetreated(string name, float hpNorm)  => Instance?.OnBossRetreated?.Invoke(name, hpNorm);

    public event System.Action<string> OnBossSpawned;
    public event System.Action<string, int> OnBossDefeated;
    public event System.Action<float> OnHealthChanged;
    public event System.Action OnEnraged;
    //public event System.Action<ScreenEffectAttackConfig.EffectType, float> OnScreenEffect;
    public event System.Action OnClearScreenEffect;

    public static void RaiseBossSpawned(string name) => Instance?.OnBossSpawned?.Invoke(name);
    public static void RaiseBossDefeated(string name, int score) => Instance?.OnBossDefeated?.Invoke(name, score);
    public static void RaiseHealthChanged(float n) => Instance?.OnHealthChanged?.Invoke(n);
    public static void RaiseEnraged() => Instance?.OnEnraged?.Invoke();
    //public static void RaiseScreenEffect(ScreenEffectAttackConfig.EffectType t, float i)
        //=> Instance?.OnScreenEffect?.Invoke(t, i);
    public static void RaiseClearScreenEffect() => Instance?.OnClearScreenEffect?.Invoke();

    private void OnEnable() => hideFlags = HideFlags.DontUnloadUnusedAsset;
}