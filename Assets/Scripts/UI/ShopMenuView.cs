using UnityEngine;

public class ShopMenuView : MonoBehaviour, IMenuPage
{
    public PageType PageType => PageType.ShopMenu;
    
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
