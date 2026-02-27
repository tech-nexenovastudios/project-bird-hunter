using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricTesting : MonoBehaviour
{
    public List<GameObject> gameObjects = new List<GameObject>();
    LineRenderer lineRenderer;
    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.enabled = true;
        lineRenderer.positionCount = gameObjects.Count;
        for (int i = 0; i < gameObjects.Count; i++)
        {
            lineRenderer.SetPosition(i, gameObjects[i].transform.position);
        }
        StartCoroutine(Move());
    }
    public void Update()
    {
      
    }

    IEnumerator Move()
    {
        yield return null;
      for(int i = 0; i<lineRenderer.positionCount-1;i++)
       {
            float time = 0;
            while(time < 1)
            {
                //Debug.Log(time);
                Debug.Log(i);
              
                lineRenderer.SetPosition(i, Vector3.Lerp(lineRenderer.GetPosition(0), lineRenderer.GetPosition(1), time));
                time += Time.deltaTime;
                yield return null;
            }

            time = 0;
            yield return null;

       }
    }
}
