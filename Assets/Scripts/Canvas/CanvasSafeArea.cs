using UnityEngine;

public class CanvasSafeArea : MonoBehaviour
{
    private Rect safeArea;

    // The RectTransform of your canvas
    public RectTransform canvasRectTransform;

    void Start()
    {
        // Get the safe area when the application starts
        UpdateSafeArea();
    }

    void Update()
    {
        // Continuously update the safe area to handle screen orientation changes
        if (safeArea != Screen.safeArea)
        {
            safeArea = Screen.safeArea;
            UpdateSafeArea();
        }
    }


    void UpdateSafeArea()
    {
        Debug.Log("Check");
        // Get the safe area bounds
        RectTransform safeAreaRectTransform = canvasRectTransform;

        // Set the anchor positions to stretch the UI to the safe area
        safeAreaRectTransform.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
        safeAreaRectTransform.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);

        // Reset the pivot point to center
        safeAreaRectTransform.pivot = new Vector2(0.5f, 0.5f);
    }
}
