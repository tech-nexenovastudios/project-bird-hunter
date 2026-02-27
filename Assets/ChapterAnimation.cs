using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChapterAnimator : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float zoomScale = 1.2f;
    public float normalScale = 1f;
    public float smoothTime = 0.2f;
    public float snapSpeed = 5f;
    public float accelerationFactor = 2f;

    private RectTransform content;
    private List<RectTransform> children = new List<RectTransform>();
    private RectTransform closestChild;

    void Start()
    {
        content = scrollRect.content;

        foreach (Transform child in content)
        {
            if (child is RectTransform)
                children.Add(child as RectTransform);
        }
    }

    void Update()
    {
        if (children.Count == 0) return;

        closestChild = GetClosestChildToCenter();

        if (closestChild != null)
        {
            float distanceToCenter = GetDistanceToCenter(closestChild);

            if (Mathf.Abs(distanceToCenter) > 10f)
            {
                float speed = Mathf.Lerp(scrollRect.velocity.x, -distanceToCenter * accelerationFactor, Time.deltaTime * snapSpeed);
                scrollRect.velocity = new Vector2(speed, 0);
            }
        }

        foreach (RectTransform child in children)
        {
            float distance = Mathf.Abs(GetDistanceToCenter(child));
            float centerX = Screen.width / 2f;

            // Find normalized distance (0 = center, 1 = edge)
            float normalized = Mathf.Clamp01(distance / (Screen.width / 2f));

            // Scale based on distance from center (only center scaled)
            float scale = Mathf.Lerp(zoomScale, normalScale, normalized);
            child.localScale = Vector3.Lerp(child.localScale, Vector3.one * scale, Time.deltaTime / smoothTime);

            // Optional: Adjust transparency if you want fade effect
            // Image img = child.GetComponent<Image>();
            // if (img) img.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0.5f, normalized));
        }
    }

    private RectTransform GetClosestChildToCenter()
    {
        RectTransform closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (RectTransform child in children)
        {
            float distance = Mathf.Abs(GetDistanceToCenter(child));

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = child;
            }
        }

        return closest;
    }

    private float GetDistanceToCenter(RectTransform child)
    {
        Vector3 worldPos = child.position;
        float centerX = Screen.width / 2f;
        return worldPos.x - centerX;
    }
}
