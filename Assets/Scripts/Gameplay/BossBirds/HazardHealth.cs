// HazardHealth.cs
using Gameplay.Interfaces;
using UnityEngine;

/// <summary>
/// Real-HP health for the boss "egg" hazard (e.g. Gorgon Griffin's Serpent Egg Drop).
/// Bullets route here through BaseBullet's "Egg"-tag path because this is now the only
/// IDamageable on the prefab — HazardGroundDetector no longer implements it, otherwise
/// GetComponent&lt;IDamageable&gt; would return the detector first (component order) and the
/// egg would die in one hit again.
///
/// When HP runs out, destruction is handed back to HazardGroundDetector.DestroyHazard()
/// so the existing pool-return + death-VFX wiring (SerpentEggDropBehaviour.OnEggDestroyed)
/// still fires. Also implements IDamageablee (double-e) so it keeps working if the egg is
/// ever re-tagged BossBird/BossWeapons.
/// </summary>
[RequireComponent(typeof(HazardGroundDetector))]
public class HazardHealth : MonoBehaviour, IDamageable, IDamageablee, IPoolable
{
    [Tooltip("Fallback HP used if no attack config initializes this egg via Init().")]
    [SerializeField] private int defaultMaxHealth = 30;

    private int maxHealth;
    private int currentHealth;
    private bool isDead;
    private HazardGroundDetector detector;

    // ── IDamageable (single-e) ──
    public int CurrentHp => currentHealth;
    public int MaxHp => maxHealth;
    public bool IsAlive => !isDead;

    private void Awake()
    {
        detector = GetComponent<HazardGroundDetector>();
        if (maxHealth <= 0) maxHealth = Mathf.Max(1, defaultMaxHealth);
    }

    /// <summary>Set max HP from the spawning attack config and reset to full.</summary>
    public void Init(int maxHp)
    {
        maxHealth = Mathf.Max(1, maxHp);
        currentHealth = maxHealth;
        isDead = false;
        if (detector == null) detector = GetComponent<HazardGroundDetector>();
    }

    // IDamageable (single-e) — BaseBullet "Egg"-tag collision path.
    public void TakeDamage(int damage) => ApplyDamage(damage);

    // IDamageablee (double-e) — boss-side float path.
    public void TakeDamage(float damage) => ApplyDamage(Mathf.RoundToInt(damage));

    private void ApplyDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= Mathf.Max(0, damage);
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Hand off to the detector so its OnDestroyed event drives VFX + pool return.
        // DestroyHazard() guards against double-fire, so the detector's own
        // self-destruct-after-stop Invoke and this call can't both return the egg twice.
        if (detector != null)
            detector.DestroyHazard();
        else
            gameObject.SetActive(false);
    }

    // ── IPoolable ──
    public void OnPoolSpawned()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    public void OnPoolDespawned() { }
}
