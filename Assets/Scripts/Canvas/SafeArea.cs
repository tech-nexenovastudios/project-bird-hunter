using System;
using UnityEngine;
using UnityEngine.UIElements;

public class SafeArea : MonoBehaviour
{
    float m_LeftBorder;
    float m_RightBorder;
    float m_TopBorder;
    float m_BottomBorder;


    float LeftBorder;
    float RightBorder;
    float TopBorder;
    float BottomBorder;

    public float Multiplier = 1;

    public static SafeArea instance;


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;  // Ensure no further execution after destroying the game object
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }



    public void ApplySafeArea(VisualElement SafeAreaVE)
    {
        Rect SafeAreaRect = Screen.safeArea;

        m_LeftBorder = SafeAreaRect.x;
        m_RightBorder = Screen.width - SafeAreaRect.xMax;
        m_TopBorder = Screen.height - SafeAreaRect.yMax;
        m_BottomBorder = SafeAreaRect.y;

        SafeAreaVE.style.borderTopWidth = m_TopBorder * Multiplier;
        SafeAreaVE.style.borderBottomWidth = BottomBorder * Multiplier;
        SafeAreaVE.style.borderLeftWidth = m_LeftBorder * Multiplier;
        SafeAreaVE.style.borderRightWidth = m_RightBorder * Multiplier;
    }
}
