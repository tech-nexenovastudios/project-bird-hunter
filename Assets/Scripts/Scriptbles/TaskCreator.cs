 using UnityEngine;


[CreateAssetMenu(menuName = "Task Creator/New Task")]
public class TaskCreator : ScriptableObject
{
    public string title;
    public string missionId;
    public int killBird;
    public float collectCoin;
    public int collectDiamond;
    public int collectPoints;
    public int destroyEggs;
    public int reward;
    public bool isCompleted;

    public void UpdateTask(float collectCoin,int collectDiamond,int collectPoints,int destroyEggs,int killBird)
    {
 
        this.collectCoin -= collectCoin;
        this.collectDiamond -= collectDiamond;
        this.collectPoints -= collectPoints;
        this.destroyEggs -= destroyEggs;
        this.killBird -= killBird;
    }
}
