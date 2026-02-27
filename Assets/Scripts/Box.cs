using UnityEngine;


public enum BoxCategory
{
    common = 1,
    uncommon = 2,
    rare = 3,
    epic = 4,
    legend=5,
}

public class Box : MonoBehaviour
{
    [SerializeField]BoxCategory category;
    public float pointRequired;

}
