using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class ShadowCannonMove : MonoBehaviour
{
    public CannonMove cannonMove;
    public Transform Cannon;
    public float offset;
    public Vector2 boundOffset;
    public Transform tyre1, tyre2;
 

    private void Update()
    {
        tyre1.transform.rotation = cannonMove.tyre1.rotation;
        tyre2.transform.rotation = cannonMove.tyre2.rotation;
    }
}