using UnityEngine;

public class BeeGenerator : MonoBehaviour
{
    [Header("Bee Settings")]
    [SerializeField] private GameObject bee;

    [Header("Grid Settings")]
    public int row = 5;
    public int col = 5;

    [Header("Spacing")]
    public float xSpacing = 0.5f;
    public float ySpacing = 0.5f;

    [Header("Start Offset")]
    public Vector2 startOffset = new Vector2(-0.468f, -0.468f);

    [ContextMenu("Generate Bees")]
    public void GenerateBee()
    {
        // Optional: clear existing children before spawning
        for (int c = transform.childCount - 1; c >= 0; c--)
        {
            DestroyImmediate(transform.GetChild(c).gameObject);
        }

        // Generate the grid
        for (int i = 0; i < row; i++)
        {
            for (int j = 0; j < col; j++)
            {
                // Calculate local position
                Vector2 localPos = new Vector2(
                    startOffset.x - (i * xSpacing),
                    startOffset.y - (j * ySpacing)
                );

                // Convert local to world position
                Vector3 worldPos = transform.TransformPoint(localPos);

                // Instantiate bee and set position
                GameObject obj = Instantiate(bee, transform);
                obj.transform.position = worldPos;

                // Debug local & world positions
                Debug.Log($"Local Pos: {localPos}");
                Debug.Log($"InverseTransformPoint: {transform.InverseTransformPoint(obj.transform.position)}");
            }
        }
    }
}
