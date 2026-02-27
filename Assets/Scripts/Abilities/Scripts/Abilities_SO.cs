using UnityEngine;

//Base Class for abilities use this as base class
public abstract class Abilities_SO : ScriptableObject
{
    public string abilityName;
    public int powerLevel;
    public GameObject abilityBird;
    public float manaRequire;
    public Sprite abilitySprite;

    public abstract void UpgradeAbility();

    public abstract void UseAbility(GameObject User);



}
