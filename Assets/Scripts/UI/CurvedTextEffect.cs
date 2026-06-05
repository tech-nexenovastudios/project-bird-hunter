using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways]
public class CurvedTextEffect : MonoBehaviour
{
    [SerializeField]
    private AnimationCurve curve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f));

    [Tooltip("Vertical bend strength in TMP units. Higher = deeper arc.")]
    [SerializeField] private float curveScale = 18f;

    private TMP_Text _text;
    private string _lastText;
    private Vector3 _lastScale;

    private void Awake() => _text = GetComponent<TMP_Text>();

    private void OnEnable()
    {
        if (_text == null) _text = GetComponent<TMP_Text>();
        _lastText = null;
    }

    private void LateUpdate()
    {
        if (_text == null) return;

        if (_text.havePropertiesChanged || _text.text != _lastText || transform.localScale != _lastScale)
            Warp();
    }

    public void Refresh() => _lastText = null;

    private void Warp()
    {
        _text.ForceMeshUpdate();

        TMP_TextInfo textInfo = _text.textInfo;
        if (textInfo == null || textInfo.characterInfo == null) return;
        int charCount = Mathf.Min(textInfo.characterCount, textInfo.characterInfo.Length);

        _lastText = _text.text;
        _lastScale = transform.localScale;

        if (charCount == 0) return;

        float minX = _text.bounds.min.x;
        float maxX = _text.bounds.max.x;
        float width = maxX - minX;
        if (Mathf.Approximately(width, 0f)) return;

        for (int i = 0; i < charCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int vert = textInfo.characterInfo[i].vertexIndex;
            int mat = textInfo.characterInfo[i].materialReferenceIndex;
            if (mat >= textInfo.meshInfo.Length) continue;
            Vector3[] vertices = textInfo.meshInfo[mat].vertices;
            if (vertices == null || vert + 3 >= vertices.Length) continue;

            // Re-center each glyph on its mid-baseline so the warp rotates it in place.
            Vector3 mid = new Vector2(
                (vertices[vert + 0].x + vertices[vert + 2].x) * 0.5f,
                textInfo.characterInfo[i].baseLine);

            vertices[vert + 0] -= mid;
            vertices[vert + 1] -= mid;
            vertices[vert + 2] -= mid;
            vertices[vert + 3] -= mid;

            float x0 = (mid.x - minX) / width;
            float x1 = x0 + 0.0001f;
            float y0 = curve.Evaluate(x0) * curveScale;
            float y1 = curve.Evaluate(x1) * curveScale;

            Vector3 horizontal = Vector3.right;
            Vector3 tangent = new Vector3(width * 0.0001f, y1 - y0);
            float dot = Mathf.Acos(Mathf.Clamp(Vector3.Dot(horizontal, tangent.normalized), -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 cross = Vector3.Cross(horizontal, tangent);
            float angle = cross.z > 0 ? dot : 360f - dot;

            Matrix4x4 m = Matrix4x4.TRS(new Vector3(0f, y0, 0f), Quaternion.Euler(0f, 0f, angle), Vector3.one);

            vertices[vert + 0] = m.MultiplyPoint3x4(vertices[vert + 0]) + mid;
            vertices[vert + 1] = m.MultiplyPoint3x4(vertices[vert + 1]) + mid;
            vertices[vert + 2] = m.MultiplyPoint3x4(vertices[vert + 2]) + mid;
            vertices[vert + 3] = m.MultiplyPoint3x4(vertices[vert + 3]) + mid;
        }

        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
    
    

    private void OnValidate()
    {
        if (_text == null) return;

        if (_text.havePropertiesChanged || _text.text != _lastText || transform.localScale != _lastScale)
            Warp();
    }
}
