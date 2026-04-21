using System;
using UnityEngine;

public class MainMenuView : MonoBehaviour, IMenuPage
{
    public PageType PageType  => PageType.MainMenu;

    private void OnEnable()
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
