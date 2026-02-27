using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Sword : MonoBehaviour
{
    [Header("Settings")]
    public float delayTime = 1f;             // Time delay between target searches
    public float damage = 10f;               // Damage dealt to the enemy
    public float speed = 5f;                 // Speed at which sword moves toward the target
    public Transform holder;                 // Parent holder to reset sword position
    [SerializeField] private LayerMask eggMask;  // Layer mask to identify valid targets (e.g., "Eggs")

    private List<Collider2D> eggs = new List<Collider2D>(); // List of nearby targets

    public CannonFire cannonFire;

    private void Start()
    {
        // Start the sword logic when scene starts
        transform.localPosition = Vector3.zero;
        StartCoroutine(SwordRoutine());
    }

    // Main coroutine loop that runs continuously
    private IEnumerator SwordRoutine()
    {
        while (true)
        {
            yield return SearchAndAttack();
        }
    }

    // Searches for the closest enemy and moves to attack
    private IEnumerator SearchAndAttack()
    {
  
        yield return new WaitForSeconds(delayTime);

        // Find all nearby colliders in a radius and sort them by distance
        eggs = Physics2D.OverlapCircleAll(transform.position, 5f, eggMask)
                        .OrderBy(e => Vector2.Distance(transform.position, e.transform.position))
                        .ToList();

        // If no targets found, exit coroutine
        if (eggs.Count == 0)
            yield break;

        // Set the closest target
        Collider2D target = eggs[0];

        // Detach sword so it can move independently
        transform.parent = null;

        // Move toward the target until close enough
        while (target != null && Vector2.Distance(transform.position, target.transform.position) > 0.2f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
            yield return null; // Wait until next frame
        }

        // If target is still valid, apply damage
        if (target != null && target.TryGetComponent<IHealthManager>(out var health))
        {
            health.TakeDamage((cannonFire.initialDamage*damage)/100);
        }
        // Move sword back to holder before searching
        transform.parent = holder;
        transform.DOLocalMove(Vector3.zero, 0.2f);
        // Short delay before next search begins
        yield return new WaitForSeconds(delayTime);
    }
}
