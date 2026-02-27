using UnityEngine;


public enum OrbType
{
    fire = 0,
    freeze = 1,
    poison = 2
}
public class Orb : MonoBehaviour
{
    public OrbType orbType;
    public OrbParent orbParent;

    private void OnTriggerEnter2D(Collider2D collision)
    {
      
        var effect = collision.GetComponent<IDamageEffect>();
        if (effect != null)
        {
            Debug.Log(collision.name);
            if (orbType == OrbType.fire)
            {
                effect.igniteDamage((orbParent.cannonFire.Damage * orbParent.damage) / 100, orbParent.duration);
            }
            else if (orbType == OrbType.freeze)
            {
                effect.FreezeEffect(orbParent.duration);
            }
            else if (orbType == OrbType.poison)
            {
                effect.PoisonDamage((orbParent.cannonFire.Damage * orbParent.damage) / 100, orbParent.duration);
            }
        }
    }
}
