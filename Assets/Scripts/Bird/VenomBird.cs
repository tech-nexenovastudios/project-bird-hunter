using System.Collections;
using UnityEngine;

public class VenomBird : MonoBehaviour
{
    [SerializeField] GameObject poison;
    float yPos;
    [SerializeField] float duration;
    [SerializeField] float coolDownTimer;
    float size;
    private void Start()
    {
        yPos = Physics2D.Raycast(transform.position, Vector3.down, float.PositiveInfinity, LayerManager.GroundMask).point.y;
        StartCoroutine(PoisonAttack());
    }


    IEnumerator PoisonAttack()
    {
        var go = Instantiate(poison);
        var particle = go.GetComponent<ParticleSystem>();
        while (true)
        {
            var ray = Physics2D.Raycast(transform.position, Vector2.down, float.MaxValue, LayerManager.GroundMask);
            go.transform.position = ray.point;
            go.SetActive(true);
            particle.Play();
            //go.transform.localScale = Vector2.one * (ScreenBounds.maxX/2);
            yield return new WaitForSeconds(duration);
            particle.Stop();
            go.SetActive(false);
            yield return new WaitForSeconds(coolDownTimer);
            yield return null;
        }
    }
}
