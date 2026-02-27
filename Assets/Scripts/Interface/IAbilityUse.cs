
// Use this interface for any type of ability to apply power and track the power level.
public interface IAbilityUse
{
    public void UseAbility();

    public int abilityLevel { get; set; }

    public bool canUseAbility { get; set; }
}
