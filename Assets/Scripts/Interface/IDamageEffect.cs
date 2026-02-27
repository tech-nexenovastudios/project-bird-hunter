using System.Collections;
using UnityEngine;

public interface IDamageEffect
{
    public void ElectricDamage(float applyDamage, float effectTime);
    public void igniteDamage(float applyDamage, float effectTime);

    public void PoisonDamage(float applyDamage,float effectTime);

    public void FreezeEffect(float effectTime);
}
