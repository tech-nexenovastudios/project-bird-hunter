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
}
