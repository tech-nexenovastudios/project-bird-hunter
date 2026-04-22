using UnityEngine;

public class PowerUpsMenuView : MonoBehaviour, IMenuPage
{
    public PageType PageType   => PageType.PowerUps;
    
    private void Start()
    {
        PageManager.Instance.Register(this);
    }
    public void OnPageEnter()
    {
        
    }

    public void OnPageExit()
    {
        
    }
}
