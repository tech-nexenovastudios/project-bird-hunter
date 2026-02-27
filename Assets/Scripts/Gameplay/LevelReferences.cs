using UnityEngine;
using UnityEngine.UI;

public class LevelReferences : MonoBehaviour
{
    [SerializeField] Collider2D leftWall;
    [SerializeField] Collider2D rightWall;

    [SerializeField] private Image cannonHealthSlider;
    [SerializeField] private TMPro.TextMeshProUGUI cannonHealthPercent;

    public (Collider2D leftWall,  Collider2D rightWall) GetWall()
    {
        return (leftWall, rightWall);
    }

    public (Image slider, TMPro.TextMeshProUGUI healthText) GetCannonHealth()
    {
        return (cannonHealthSlider, cannonHealthPercent);
    }
}
