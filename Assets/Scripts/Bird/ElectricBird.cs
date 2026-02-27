using System.Collections;
using UnityEngine;

public class ElectricBird : MonoBehaviour
{
    SpriteRenderer sprite;
    [SerializeField] ParticleSystem particle;

    private void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
    }
    float timer = 0;
    private void Update()
    {
        timer += Time.deltaTime;
        if (timer > 3)
        {
            timer = 0;
            particle.Play();
            var ray = Physics2D.Raycast(transform.position, Vector2.down, float.PositiveInfinity, LayerManager.PlayerMask);
            if (ray)
            {
                Debug.Log(ray.collider.gameObject.name);
            }
          
        }
    }

    
}
