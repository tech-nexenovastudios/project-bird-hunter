using UnityEngine;

public class FireRingController : MonoBehaviour, IPoolable
{
    private Vector3 targetPosition;
    private float moveSpeed;
    private float lifetime;
    private float age;
    private bool isActive;


    public float Age { get; private set; }
    public bool HasHitCannon { get; set; }

    public void Launch(Vector3 target, float speed, float lifetime)
    {
        this.targetPosition = target;
        this.moveSpeed = speed;
        this.lifetime = lifetime;
        this.age = 0f;
        this.isActive = true;
        Age = 0f;
        HasHitCannon = false;
        var ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear();
            ps.Play(true);
        }
    }

    private void Update()
    {
        if (!isActive) return;

        age += Time.deltaTime;

        if (age >= lifetime)
        {
            isActive = false;
            PoolManager.Return(gameObject);
            return;
        }

        float dist = Vector3.Distance(transform.position, targetPosition);
        if (dist > 0.1f)
        {
            Vector3 dir = (targetPosition - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
        }
    }

    public void OnPoolSpawned()
    {
        age = 0f;
        isActive = false;
        Age = 0f;
        HasHitCannon = false;
        isActive = false;
    }

    public void OnPoolDespawned()
    {
        isActive = false;
        var ps = GetComponentInChildren<ParticleSystem>();
        ps?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}