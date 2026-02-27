using UnityEngine;


//All the spawn logic comes here of bomb bird
public class BombBird : MonoBehaviour
{
    public GameObject bomb;
   

    private void Start()
    {
        InvokeRepeating("BombSpawner", 2, 2);
      
    }

    public void BombSpawner()
    {
        GameObject go = Instantiate(bomb, transform.position, Quaternion.identity);
    }
}
