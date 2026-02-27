using System;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] TMP_Text scoreText;
    [SerializeField] float totalScore;
    public static Action<float> scoreUpdateCallBack;


    private void Start()
    {
        scoreUpdateCallBack?.Invoke(0);
    }

    private void OnEnable()
    {
        scoreUpdateCallBack += UpdateScore;
    }

    private void OnDisable()
    {
        scoreUpdateCallBack -= UpdateScore;
    }

    private void UpdateScore(float score)
    {
        totalScore += score;
        scoreText.text = "Score: " + totalScore.ToString("0");
    }

}
