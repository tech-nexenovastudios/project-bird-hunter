using UnityEngine;

public static class ExtensionMethods
{
   public static void GlobalScale(this Transform transform,Vector3 globalScale)
    {
        transform.localScale = Vector3.one;
        transform.localScale = new Vector3(globalScale.x / transform.lossyScale.x, globalScale.y / transform.lossyScale.y, globalScale.z / transform.lossyScale.z);
    }   

    public static void PlayerBoundCalculate(this Transform transform,BoxCollider2D collider,out Vector2 size)
    {
        size = new Vector2(transform.localScale.x * (collider.size.x/2), transform.localScale.y * (collider.size.y/2));
    }
}
