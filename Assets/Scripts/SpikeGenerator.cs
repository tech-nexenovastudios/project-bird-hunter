using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpikeGenerator : MonoBehaviour
{
    [SerializeField] GameObject spike;
    [SerializeField] float distance = 1f;
    [SerializeField] int spikeCount = 5;
    [SerializeField] float angle = 45;

    private void OnValidate()
    {
        if (!spike) return;

#if UNITY_EDITOR
        // Delay execution to avoid OnValidate restriction
        EditorApplication.delayCall += Regenerate;
#endif
    }

#if UNITY_EDITOR
    void Regenerate()
    {
        if (this == null) return; // safety check

        ClearChildren();

        for (int i = 0; i < spikeCount *2; i++)
        {
            if (i % 2 == 0) continue;
            GameObject go = Instantiate(spike, transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0, 0, angle * i);
            Vector3 pos = go.transform.up * distance;
            go.transform.localPosition = pos;
        }
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
#endif
}
