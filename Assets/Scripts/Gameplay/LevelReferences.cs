using UnityEngine;
using UnityEngine.UI;

public class LevelReferences : MonoBehaviour
{
    [SerializeField] Collider2D leftWall;
    [SerializeField] Collider2D rightWall;
  

    public (Collider2D, Collider2D) GetWall() => (leftWall, rightWall);
  
}