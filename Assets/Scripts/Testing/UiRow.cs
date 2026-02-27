using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiRow : MonoBehaviour
{
    [SerializeField] List<RectTransform> childObj = new();

    [SerializeField] float spacing;
    [SerializeField] float leftSpacing;
    [SerializeField] float rightSpacing;
    [SerializeField] float topSpacing;
    [SerializeField] float bottomSpacing;


    [SerializeField] bool debug;
/*    private void OnValidate()
    {
       
        int counter = 0;
        foreach (var obj in childObj)
        {
      
            obj.anchorMin = new Vector2(0.5f, 1f);
            obj.anchorMax = new Vector2(0.5f, 1f);
            obj.anchoredPosition = new Vector2(0f, ((-(obj.sizeDelta.y * counter) - spacing * counter) - obj.sizeDelta.y /2));
            obj.transform.GetComponentInChildren<TextMeshProUGUI>().text = (counter+1).ToString();
           // obj.GetComponent<Image>().color = Random.ColorHSV();
            counter++;

        }
        if (debug)
        {
            Debug.Log("Update");
        }
    }*/
}
