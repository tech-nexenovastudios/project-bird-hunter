using UnityEngine;

public class EggDamage : MonoBehaviour
{
    EggType eggType;
    EggHealth eggHealth;
    private void Start()
    {
        eggType = GetComponent<EggHealth>().eggType;
        eggHealth = this.GetComponent<EggHealth>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.tag == "Player")
        {
            var health = collision.GetComponent<CannonHealth>();
            if (health != null)
            {
                health.GetDamage(eggHealth);
            }
            var damageEffect = collision.GetComponent<IDamageEffect>();
            switch (eggType)
            {
                case EggType.IceEgg:
                    damageEffect.FreezeEffect(3f);
                    break;
                case EggType.FireEgg:
                    damageEffect.igniteDamage(1f, 3f);
                    break;
            }
            /*  if (eggType == EggType.IceEgg)
              {
                  var damageEffect = collision.GetComponent<IDamageEffect>();
                  damageEffect.FreezeEffect(3f);
              }*/
        }
    }
}
