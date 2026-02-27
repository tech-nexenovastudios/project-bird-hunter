using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//This class generate task by using taskgenerator scriptble object
public class TaskGenerator : MonoBehaviour
{
    [SerializeField] List<TaskCreator> tasks = new List<TaskCreator>();
    [SerializeField] Transform taskParent;
    [SerializeField] GameObject taskTemplate;
    public static Action taskCheck; //Invoke when you need to update task list


     public IEnumerator GenerateTask()
    {
        foreach (var task in tasks)
        {
            if (!task.isCompleted)
            {
                GameObject go = Instantiate(taskTemplate, taskParent);
                var t = go.GetComponent<TaskProperties>();
                t.title.text = task.title;
                t.btn.onClick.RemoveAllListeners();
                t.AssignCreator(task);
                taskCheck += t.ValueSet;
                taskCheck?.Invoke();
                t.btn.onClick.AddListener(() =>
                {
                    t.TaskCheck();
                });
            }
            yield return null;
        }

    }

   


}
