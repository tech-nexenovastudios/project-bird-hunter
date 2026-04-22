using System;
using UnityEngine;

public class MainMenuView : MonoBehaviour, IMenuPage
{
    public PageType PageType  => PageType.MainMenu;

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
