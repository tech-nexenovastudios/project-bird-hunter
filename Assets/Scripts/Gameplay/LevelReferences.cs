using UnityEngine;
using UnityEngine.UI;


using UnityEngine;
using UnityEngine.UI;

public class LevelReferences : MonoBehaviour
{
    [SerializeField] Collider2D leftWall;
    [SerializeField] Collider2D rightWall;

    [SerializeField] private Image cannonHealthBar;     
    [SerializeField] private Image cannonDamageBar;     
    //[SerializeField] private TMPro.TextMeshProUGUI cannonHealthPercent;

    public (Collider2D, Collider2D) GetWall() => (leftWall, rightWall);

    public (Image healthBar, Image damageBar ) GetCannonHealth()
        => (cannonHealthBar, cannonDamageBar );
}