using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PageManager : MonoBehaviour
{
    int CurrentIndex;  
    int previousIndex;
    [Tooltip("Parent of pages")]
    [SerializeField] RectTransform PageRect;
   
    [SerializeField] List<GameObject> Pages = new List<GameObject>();

    float previousClickTime;


    [SerializeField] float animationTime;
    
    private void Start()
    {
        UpdatePage(0);
    }

    // -800 * index: This works by getting the index from the button. Since the index starts from -2, we add 2 to it to get the correct index from the list.
    public void UpdatePage(int index)
    {
        CurrentIndex = index+2;
        if (CurrentIndex != previousIndex)
        {
            StopCoroutine("SetPage");
            previousClickTime = Time.time;
            for (int i = Mathf.Min(previousIndex+1, CurrentIndex); i <= Mathf.Max(previousIndex-1, CurrentIndex); i++)
            {
              
                Pages[i].SetActive(true);
            }

            PageRect.DOAnchorPos(new Vector2(-1080 * index, PageRect.anchoredPosition.y), animationTime);
            previousIndex = CurrentIndex;
            StartCoroutine(SetPage(index));

        }

    }

    IEnumerator SetPage(int index)
    {
        yield return new WaitForSeconds(animationTime+0.5f);
        if (Time.time - previousClickTime < animationTime + 0.5f) yield break;
        int i = 0;
        foreach (GameObject page in Pages)
        {
            if (i == index + 2)
            {
                page.SetActive(true);
            }
            else
            {
                page.SetActive(false);
            }
            i++;
        }

    }
}
