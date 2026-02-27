using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceField : MonoBehaviour
{
    [SerializeField] LineRenderer forceFieldLine; 
    [SerializeField] List<Transform> drones = new List<Transform>();
    [SerializeField] EdgeCollider2D edgeCollider;
    [SerializeField] List<Vector2> points = new();

    public float activationTime;
    public float deactivationTime;

    private void Start()
    {
        forceFieldLine.positionCount = drones.Count;
      //  StartCoroutine(UpdateCoroutine());
       
    }
    // Update is called once per frame
    void Update()
    {

    }

/*    IEnumerator UpdateCoroutine()
    {

       *//* foreach (var drone in drones)
        {
            drone.gameObject.SetActive(false);
          
        }
 

        for (int i = 0; i < drones.Count; i++)
        {
            forceFieldLine.SetPosition(i, drones[i].position);
            points.Add(drones[i].position);
        }

        foreach (var drone in drones)
        {
            drone.gameObject.SetActive(true);
           
        }
        forceFieldLine.gameObject.SetActive(true);
        edgeCollider.SetPoints(points);
        points.Clear();
        yield return new WaitForSeconds(deactivationTime);
        forceFieldLine.gameObject.SetActive(false); *//*

    }*/
}
