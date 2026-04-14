using System.Collections;
using UnityEditor.EditorTools;
using UnityEngine;

public abstract class BaseAttackBehaviour : MonoBehaviour
{
    protected BossBirdController boss;
    protected bool isRunning;
    protected float duration;  
    private float timer;
    private float cooldown;
    private float cooldownMultiplier = 1f;
    public System.Action OnAttackStarted;
    public System.Action OnAttackComplete;

    // Call these at the right moments in your base/beam:
    protected void NotifyAttackStarted() => OnAttackStarted?.Invoke();
    protected void NotifyAttackComplete() => OnAttackComplete?.Invoke();
    public void Initialize(BossBirdController controller, float cooldown, float duration)
    {
        this.boss = controller;
        this.cooldown = cooldown;
        this.duration = duration;
        this.timer = cooldown; // ← wait one full cooldown before first attack
    }

    public void Tick(float deltaTime)
    {
        if (!isRunning)
        {
            timer -= deltaTime;
            if (timer <= 0f)
            {
                isRunning = true;
                OnExecute();
                timer = cooldown * cooldownMultiplier;
            }
        }
    }

    public void ApplyCooldownMultiplier(float mult) => cooldownMultiplier = mult;

    protected abstract void OnExecute();
    public abstract void OnStop();
    public abstract void OnCleanup();

    protected void ShowWarning(BaseAttackConfig config, System.Action onComplete)
    {
        if (config.warningPrefab != null)
            StartCoroutine(WarningRoutine(config, onComplete));
        else
            onComplete?.Invoke();
    }

    private IEnumerator WarningRoutine(BaseAttackConfig config, System.Action onComplete)
    {
        var warning = PoolManager.Get(config.warningPrefab, transform.position);
        yield return new WaitForSeconds(config.warningDuration);
        PoolManager.Return(warning);
        onComplete?.Invoke();
    }
}