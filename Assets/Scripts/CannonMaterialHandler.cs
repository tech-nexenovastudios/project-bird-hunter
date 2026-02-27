using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CannonMaterialHandler : MonoBehaviour
{
    public List<SpriteRenderer> materials;
    [Range(0, 1)]
    public float alphaValue;

    private void OnValidate()
    {
        materials = transform.GetComponentsInChildren<SpriteRenderer>().ToList();

        foreach (var renderer in materials)
        {
            // Avoid instancing by using sharedMaterial
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Opacity"))
            {
                renderer.sharedMaterial.SetFloat("_Opacity", alphaValue);
            }
        }
    }
}
