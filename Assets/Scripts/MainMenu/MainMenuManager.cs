using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
   public List<MonoBehaviour> list = new List<MonoBehaviour>();

    [SerializeField]TaskGenerator taskGenerator;    

    private void OnValidate()
    {
       // list = FindObjectsOfType<MonoBehaviour>().ToList();
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        StartCoroutine(taskGenerator.GenerateTask());
    }
}
