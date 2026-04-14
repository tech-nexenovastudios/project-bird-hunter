// PoolTracker.cs
// Lightweight component attached to every pooled object.
// Stores which pool it belongs to so Return() doesn't need a dictionary lookup.

using UnityEngine;

[DisallowMultipleComponent]
public class PoolTracker : MonoBehaviour
{
    [HideInInspector] public int PrefabID;
}