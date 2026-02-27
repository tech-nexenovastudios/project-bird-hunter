using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class BoxManager : MonoBehaviour
{
    [SerializeField] List<Box> boxList = new();
    [SerializeField] TaskCompleteCollect TaskCompleteCollect;
    int Counter = 0;
    private void OnEnable()
    {
        TaskCompleteCollect.onStarChange += BoxCheck;
    }
    private void OnDisable()
    {
        TaskCompleteCollect.onStarChange -= BoxCheck;
    }
    public void BoxCheck()
    {
        foreach(Box box in boxList)
        {
    
            if (box.pointRequired > TaskCompleteCollect.totalStar)
            {
                break;
            }
            else
            {
                StartCoroutine(UnlockBox(box.pointRequired,box));
                Counter++;  
                Debug.Log(box.name);
            };
        }
        for(int i = 0; i < Counter; i++)
        {
            boxList.RemoveAt(0);
        }
        Counter = 0;
    }

    IEnumerator UnlockBox(float value,Box box)
    {
        yield return new WaitUntil(() =>  value <= TaskCompleteCollect.progressSlider.fillAmount*100);
        RectTransform rect = box.GetComponent<RectTransform>();
        rect.DOScale(Vector3.one * 0.7f, 0.2f).SetLoops(2).OnComplete(() =>
        {
            rect.DOScale(Vector3.one, 0.1f);
            rect.GetComponent<Image>().color = Color.green;
        });
    }

}
