using System;
using UnityEngine;
using DG.Tweening;
using UnityUtils;

public class LoginScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup loginCanvas;
    [SerializeField] private GameObject[] loginOptions;
    
    [SerializeField] private GameObject guestLogin;

    private void Start()
    {
        loginCanvas.alpha = 0;
        loginCanvas.blocksRaycasts = false;
        loginCanvas.interactable = false;
        
        loginOptions.ForEach(option => option.SetActive(false));
    }

    public void DisableLoginOptions()
    {
        loginCanvas.DOFade(0,0.25f).OnComplete(()=>
        {
            loginCanvas.blocksRaycasts = false;
            loginCanvas.interactable = false;
        });

        foreach (var option in loginOptions)
        {
            option.SetActive(false);
            option.TryGetComponent(out RectTransform rectTransform);
            rectTransform.DOAnchorPosY(-5,0.25f);
        }
    }
    
    public void EnableLoginOptions()
    {
        loginCanvas.DOFade(1,0.25f).OnComplete(()=>
        {
            loginCanvas.blocksRaycasts = true;
            loginCanvas.interactable = true;
        });
        
        foreach (var option in loginOptions)
        {
            option.SetActive(true);
            option.TryGetComponent(out RectTransform rectTransform);
            rectTransform.DOAnchorPosY(0,0.25f);
        }
    }
}
