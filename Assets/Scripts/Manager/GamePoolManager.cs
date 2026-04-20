using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;



public static class GamePoolManager
{
    // public static Dictionary<BirdType, Queue<GameObject>> birdPool = new();
    // public static Dictionary<EggType, Queue<GameObject>> EggPool = new();
    public static Queue<GameObject> blastParticlePool = new Queue<GameObject>();
    // public static Dictionary<BulletType, Queue<GameObject>> bulletQueue = new(); //Bullet pool
    public static Queue<GameObject> bulletDamageTextQueue = new(); //Bullet pool
    public static Queue<GameObject> cannonBulletQueue = new();


    public async static void SetBackToPool<T>(int timer, Queue<T> queue, T Obj)
    {
        await Task.Delay(timer * 1000);
        queue.Enqueue(Obj);
        if (Obj is GameObject go)
        {
            if (go != null)
            {
                go.SetActive(false);
            }
        }
    }


}
