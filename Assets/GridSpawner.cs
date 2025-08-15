using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    [Header("Grid Settings")]
    public GameObject prefab;       // Your box prefab
    public Vector3 boundingBoxSize; // Width, Length, Height in meters
    public Vector3 prefabSize = Vector3.one; // Size of the prefab in meters

    [Header("Parent Settings")]
    public Transform gridParent;    // Parent GameObject for all boxes

    private void Start()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        if (prefab == null || gridParent == null)
        {
            Debug.LogError("Prefab or grid parent not assigned!");
            return;
        }

        // Clear old children
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridParent.GetChild(i).gameObject);
        }

        // Calculate number of boxes in each axis
        int countX = Mathf.FloorToInt(boundingBoxSize.x / prefabSize.x);
        int countY = Mathf.FloorToInt(boundingBoxSize.y / prefabSize.y);
        int countZ = Mathf.FloorToInt(boundingBoxSize.z / prefabSize.z);

        Vector3 startOffset = Vector3.zero; // First box at (0,0,0) local

        for (int x = 0; x < countX; x++)
        {
            for (int y = 0; y < countY; y++)
            {
                for (int z = 0; z < countZ; z++)
                {
                    Vector3 localPos = startOffset + new Vector3(
                        x * prefabSize.x,
                        y * prefabSize.y,
                        z * prefabSize.z
                    );

                    GameObject newBox = Instantiate(prefab, gridParent);
                    newBox.transform.localPosition = localPos;
                    newBox.transform.localRotation = Quaternion.identity;
                }
            }
        }
    }

    /// <summary>
    /// Snaps the grid parent to the given world position (center of Spatial ID)
    /// </summary>
    public void SnapGridToPosition(Vector3 spatialIDCenter)
    {
        gridParent.position = spatialIDCenter;
    }

    private void OnDrawGizmos()
    {
        if (gridParent != null)
        {
            Gizmos.color = Color.yellow;

            // Draw bounding box in local space, centered on the gridParent's position
            Vector3 center = gridParent.position + boundingBoxSize / 2f;
            Gizmos.matrix = Matrix4x4.TRS(gridParent.position, gridParent.rotation, Vector3.one);
            Gizmos.DrawWireCube(boundingBoxSize / 2f, boundingBoxSize);
        }
    }
}
