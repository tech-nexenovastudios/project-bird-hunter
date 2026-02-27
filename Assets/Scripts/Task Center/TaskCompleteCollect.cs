using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

// This class is responsible for checking task completion and handling reward collection.
public class TaskCompleteCollect : MonoBehaviour
{
    public Image progressSlider;// I am using an Image as a slider in Filled mode.
    public float totalStar;

    public static Action onStarChange;
    private void OnEnable()
    {
        TaskProperties.taskComplete += collectStar;
    }
    private void OnDisable()
    {
        TaskProperties.taskComplete -= collectStar;
    }

    public void collectStar(int collect)
    {
        totalStar += collect;
        progressSlider.DOFillAmount(totalStar / 100, 0.2f);
        onStarChange?.Invoke();
    }
}
