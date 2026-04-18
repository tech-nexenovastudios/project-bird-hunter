using UnityEngine;

namespace Gameplay.Interfaces
{
    public interface IDamageable
    {
        int CurrentHp { get; }
        int MaxHp { get; }
        bool IsAlive { get; }
        void TakeDamage(int damage);
    }

    public interface IEntity : IDamageable
    {
        void Heal(int amount);
    }

    /// <summary>
    /// Implement on your cannon MonoBehaviour.
    /// All power-up modifiers talk to the cannon through this.
    /// </summary>
    public interface ICannon : IEntity
    {
        bool IsMoving { get; }
        float ManaFillRateBonus { get; set; }
        bool IsFiring { get; }
        bool SuppressBullets { get; set; }
        int BaseAttack { get; }
        int CurrentAttack { get; }
        void AddAttackModifier(float flatBonus, float percentBonus);
        void RemoveAttackModifier(float flatBonus, float percentBonus);
        void IncreaseMaxHp(int amount);
        bool IsInvincible { get; set; }
        float HitboxScale { get; set; }
        int ShieldHits { get; set; }
        bool HasRevive { get; set; }
        float ReviveHealthPercent { get; set; }
        Transform Transform { get; }
    }

    /// <summary>
    /// Implement on any entity that can be slowed/frozen.
    /// BaseBird implements this — freeze effects call ApplyFreeze
    /// which multiplies movement speed by (1 - slowPercent).
    /// </summary>
    public interface IFreezable
    {
        void ApplyFreeze(float slowPercent);
        void RemoveFreeze();
    }
}