//using Gameplay.Interfaces;
//using Gameplay.Player;
//using Gameplay.PowerUps;
//using System.Collections.Generic;
//using UnityEngine;

//[System.Serializable]
//public class BounceEffect : IEffect<IEntity>
//{
//    public int bounceCount = 2;
//    public float searchRadius = 15f;
//    public float damageMultiplier = 0.8f;

//    private readonly List<IEntity> hitTargets = new();

//    private static readonly int eggLayer = LayerMask.NameToLayer("Egg");

//    public void Apply(IEntity target, BaseBullet bullet)
//    {
//        if (target == null || bullet == null) return;

//        hitTargets.Add(target);

//        if (bounceCount <= 0)
//        {
//            bullet.Deactivate();
//            return;
//        }

//        bounceCount--;

//        IEntity next = FindNextTarget(bullet.transform.position);

//        if (next == null)
//        {
//            bullet.Deactivate();
//            return;
//        }

//        // Reduce damage slightly on each bounce
//        bullet.damage *= damageMultiplier;

//        // Redirect bullet toward next target
//        Vector2 dir = (next.Transform.position - bullet.transform.position).normalized;

//        bullet.transform.up = dir;

//        if (bullet.Rigidbody != null)
//            bullet.Rigidbody.linearVelocity = dir * bullet.Speed;
//    }

//    private IEntity FindNextTarget(Vector2 position)
//    {
//        Collider2D[] cols = Physics2D.OverlapCircleAll(position, searchRadius);

//        float minDist = float.MaxValue;
//        IEntity nearest = null;

//        foreach (var col in cols)
//        {
//            // ✅ Layer check (FAST)
//            if (col.gameObject.layer != eggLayer) continue;

//            // ✅ Tag check (EXTRA SAFETY)
//            if (!col.CompareTag("Egg")) continue;

//            IEntity e = col.GetComponent<IEntity>();
//            if (e == null || !e.IsAlive || hitTargets.Contains(e)) continue;

//            float dist = Vector2.Distance(position, col.transform.position);
//            if (dist < minDist)
//            {
//                minDist = dist;
//                nearest = e;
//            }
//        }

//        return nearest;
//    }
//}