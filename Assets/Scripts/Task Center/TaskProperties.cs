using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// TaskProperties contains all the necessary variables, such as the title.
public class TaskProperties : MonoBehaviour
{
    public TMP_Text title;
    public TMP_Text description;
    public Button btn;
    private TaskCreator creator;
    public static Action<int> taskComplete;// This is invoked when any task is successfully completed.


    // Assigning the creator to the individual so it can be accessed later if needed.
    public void AssignCreator(TaskCreator creator)
    {
        this.creator = creator;
    }

    // The ValueSet method changes the button's color and interactable properties by checking if all tasks are successfully completed.
    public void ValueSet()
    {

        if (creator.killBird <= 0 && creator.collectPoints <= 0 && creator.collectCoin <= 0 && creator.collectDiamond <= 0 && creator.destroyEggs <= 0)
        {
            btn.image.color = Color.green;
            btn.interactable = true;
        }
        else
        {
            btn.image.color = Color.yellow;
            btn.interactable = false;
        }
    }

    // Task check is called through the button to collect the star or reward.
    public void TaskCheck()
    {


        if (creator.killBird <= 0 && creator.collectPoints <= 0 && creator.collectCoin <= 0 && creator.collectDiamond <= 0 && creator.destroyEggs <= 0)
        {
            taskComplete?.Invoke(creator.reward);
            //creator.isCompleted = true;    //testing phase 
        }
        else
        {
            btn.interactable = false;
        }
    }
}
