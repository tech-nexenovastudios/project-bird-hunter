using UnityEngine;

public class ScreenBoundManager : MonoBehaviour
{

    Camera MainCam;
    public void Awake()
    {
        MainCam = GetComponent<Camera>();
        CalculateScreenBounds();
    }

    private void CalculateScreenBounds()
    {


        if (MainCam == null) return;

        // Mobile/Phone - clamp to screen edges (walls not visible)
        Vector3 leftEdge = MainCam.ScreenToWorldPoint(new Vector3(0, Screen.height / 2, 0));
        Vector3 rightEdge = MainCam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2, 0));

        Vector3 topEdge = MainCam.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height, 0));
        Vector3 bottomEdge = MainCam.ScreenToWorldPoint(new Vector3(Screen.width / 2, 0, 0));

        ScreenBounds.minX = leftEdge.x;
        ScreenBounds.maxX = rightEdge.x;
        ScreenBounds.minY = bottomEdge.y;
        ScreenBounds.maxY = topEdge.y;

       

    }


}
