using UnityEngine;

namespace Gameplay.Interfaces
{
    public interface IDamageable
    {
        int CurrentHp { get; }
        int MaxHp { get; }
        bool IsAlive { get; }
        void TakeDamage(int damage, Vector3 hitPoint);
    }
}
