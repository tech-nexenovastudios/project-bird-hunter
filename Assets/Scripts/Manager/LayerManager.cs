using UnityEngine;


//This Class Responsible for layer so you can manage layer in a single place
public static class LayerManager
{
    public static readonly LayerMask GroundMask = LayerMask.GetMask("Ground");
    public static readonly LayerMask PlayerMask = LayerMask.GetMask("Player");
}
