#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class AddButtonsToSlotIcons : MonoBehaviour
{
    [MenuItem("Tools/Slot Machine/Add Buttons to Icons")]
    public static void AddButtonsToSelectedIcons()
    {
        if (Selection.activeTransform == null)
        {
            Debug.LogWarning("Please select an ItemContainer in the Hierarchy.");
            return;
        }

        Transform container = Selection.activeTransform;

        foreach (Transform child in container)
        {
            // Ensure Image
            Image img = child.GetComponent<Image>();
            if (img == null)
            {
                img = child.gameObject.AddComponent<Image>();
                img.color = new Color(1, 1, 1, 0); // transparent by default
            }

            // Ensure Button
            Button btn = child.GetComponent<Button>();
            if (btn == null)
            {
                btn = child.gameObject.AddComponent<Button>();
                Debug.Log($"Added Button to: {child.name}");
            }

            // ✅ Change size to 400x400
            RectTransform rect = child.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(200, 200);
            }
        }

        Debug.Log("Finished adding buttons and setting size to 400x400.");
    }
}
#endif
