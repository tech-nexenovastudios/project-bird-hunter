using System.Collections.Generic;
using UnityEngine;

public class GroundSpikeGenerator : MonoBehaviour
{
    public float distanceBetweenSpike;
    public GameObject spikePrefab;
    public List<GameObject> spikeList;
    public int numberOfSpikeRequired;
    public float startPosX;

    [ContextMenu("GenerateSpike")]
    public void CreateSpike()
    {
        var ray = Physics2D.Raycast(transform.position,Vector2.down,float.PositiveInfinity,LayerManager.GroundMask);
      
        // Ensure we are in Edit Mode
#if UNITY_EDITOR
        if (spikeList.Count > 0)
        {
            foreach (var spike in spikeList)
            {
                if (spike != null)
                    DestroyImmediate(spike.gameObject);
            }
            spikeList.Clear();
        }


        distanceBetweenSpike = startPosX;
        for (int i = 0; i < numberOfSpikeRequired; i++)
        {
            var newSpike = Instantiate(spikePrefab, new Vector3(distanceBetweenSpike, Random.Range(ray.point.y-0.2f, ray.point.y - 0f), 0), Quaternion.identity,transform);
            newSpike.GetComponent<SpriteRenderer>().sortingOrder = -1;
            spikeList.Add(newSpike);
            distanceBetweenSpike += 0.25f;
        }
#endif
    }

}
