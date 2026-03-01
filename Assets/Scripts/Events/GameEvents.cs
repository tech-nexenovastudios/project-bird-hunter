using System;
using UnityEngine;
using Gameplay.Interfaces;

namespace Gameplay.Events
{
    public static class GameEvents
    {
        // Egg events
        public static event Action<IDamageable, int, Vector3> OnEggHit;
        public static event Action<IDamageable, int, Vector3> OnEggDestroyed;

        // Bird events
        public static event Action<IDamageable, int, Vector3> OnBirdHit;
        public static event Action<IDamageable, int, Vector3> OnBirdDestroyed;

        // Cannon hit (for combo/feedback)
        public static event Action<Vector3, int> OnCannonHit;

        // Score
        public static event Action<int, int> OnLevelScoreUpdated;

        public static void FireEggHit(IDamageable egg, int damage, Vector3 hitPoint) =>
            OnEggHit?.Invoke(egg, damage, hitPoint);

        public static void FireEggDestroyed(IDamageable egg, int scoreAwarded, Vector3 position) =>
            OnEggDestroyed?.Invoke(egg, scoreAwarded, position);

        public static void FireBirdHit(IDamageable bird, int damage, Vector3 hitPoint) =>
            OnBirdHit?.Invoke(bird, damage, hitPoint);

        public static void FireBirdDestroyed(IDamageable bird, int scoreAwarded, Vector3 position) =>
            OnBirdDestroyed?.Invoke(bird, scoreAwarded, position);

        public static void FireCannonHit(Vector3 position, int damage) =>
            OnCannonHit?.Invoke(position, damage);

        public static void FireLevelScoreUpdated(int currentScore, int delta) =>
            OnLevelScoreUpdated?.Invoke(currentScore, delta);
    }
}
