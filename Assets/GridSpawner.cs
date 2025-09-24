using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    [Header("Grid Settings")]
    public GameObject prefab;            // Your box prefab
    public Vector3Int gridSize = new Vector3Int(3, 3, 3); // Number of cells in each axis
    public Vector3 prefabSize = Vector3.one; // Size of each prefab cell in meters

    [Header("Parent Settings")]
    public Transform gridParent;         // Parent GameObject for all boxes

    private string centerSpatialId;

    private void Start()
    {
        print("Is this working ");
        GenerateGridFromStartingSpatialID("28/23841151/105672204/573");
    }

    /// <summary>
    /// Generate a grid around the given SpatialID
    /// </summary>
    public void GenerateGridFromSpatialId(string spatialId)
    {
        if (prefab == null || gridParent == null)
        {
            Debug.LogError("Prefab or grid parent not assigned!");
            return;
        }

        centerSpatialId = spatialId;

        // Clear old children
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridParent.GetChild(i).gameObject);
        }

        // Get world position of center spatial ID
        Vector3 centerPos = SpatialIdUtility.GetCenterPosition(spatialId);

        // Snap grid parent to that center
        gridParent.position = centerPos;

        // Calculate offsets so that the given spatialId is in the middle of the grid
        Vector3Int half = new Vector3Int(gridSize.x / 2, gridSize.y / 2, gridSize.z / 2);

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    Vector3 localPos = new Vector3(
                        (x - half.x) * prefabSize.x,
                        (y - half.y) * prefabSize.y,
                        (z - half.z) * prefabSize.z
                    );

                    GameObject newBox = Instantiate(prefab, gridParent);
                    newBox.transform.localPosition = localPos;
                    newBox.transform.localRotation = Quaternion.identity;

                    // Assign SpatialIDCell
                    SpatialIdCell cell = newBox.AddComponent<SpatialIdCell>();

                    Vector3Int offset = new Vector3Int(x - half.x, y - half.y, z - half.z);
                    string neighborId = SpatialIdUtility.GetNeighborSpatialId(spatialId, offset);

                    cell.Init(neighborId, new Vector3Int(x, y, z));
                }
            }
        }
    }

    /// <summary>
    /// Shortcut: Generate grid around the first SpatialID received from NoderedConnector
    /// </summary>
    public void GenerateGridFromFirstSpatialId()
    {
        if (NoderedConnector.detections != null && NoderedConnector.detections.Count > 0)
        {
            var firstDetection = NoderedConnector.detections[0];
            if (firstDetection.spatial_ids != null && firstDetection.spatial_ids.Count > 0)
            {
                var firstSid = firstDetection.spatial_ids[0];

                // Construct the string form zoom/x/y/z from the SpatialID center
                string firstId = firstSid.zoom + "/" +
                                 firstSid.center[0] + "/" +
                                 firstSid.center[1] + "/" +
                                 firstSid.center[2];

                GenerateGridFromSpatialId(firstId);
            }
            else
            {
                Debug.LogWarning("First detection has no spatial IDs!");
            }
        }
        else
        {
            Debug.LogWarning("No detections available in NoderedConnector!");
        }
    }

    /// <summary>
    /// Generate a grid around a starting SpatialID you provide.
    /// </summary>
    public void GenerateGridFromStartingSpatialID(string spatialId)
    {
        if (prefab == null || gridParent == null)
        {
            Debug.LogError("Prefab or grid parent not assigned!");
            return;
        }

        centerSpatialId = spatialId;

        // Clear old children
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridParent.GetChild(i).gameObject);
        }

        // Get world position of center spatial ID
        Vector3 centerPos = SpatialIdUtility.GetCenterPosition(spatialId);

        // Snap grid parent to that center
        //gridParent.position = centerPos;

        // Calculate offsets so that the given spatialId is in the middle of the grid
        Vector3Int half = new Vector3Int(gridSize.x / 2, gridSize.y / 2, gridSize.z / 2);

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    // Local grid offset position
                    Vector3 localPos = new Vector3(
                        (x - half.x) * prefabSize.x,
                        (y - half.y) * prefabSize.y,
                        (z - half.z) * prefabSize.z
                    );

                    // Instantiate cube
                    GameObject newBox = Instantiate(prefab, gridParent);
                    newBox.transform.localPosition = localPos;
                    newBox.transform.localRotation = Quaternion.identity;

                    // Attach SpatialIdCell component
                    SpatialIdCell cell = newBox.AddComponent<SpatialIdCell>();

                    // Offset in terms of spatial ID neighbors
                    Vector3Int offset = new Vector3Int(x - half.x, y - half.y, z - half.z);
                    string neighborId = SpatialIdUtility.GetNeighborSpatialId(spatialId, offset);

                    // Initialize cell
                    cell.Init(neighborId, new Vector3Int(x, y, z));
                }
            }
        }

        Debug.Log($"Generated grid centered on SpatialID: {spatialId}");
    }

    private void OnDrawGizmos()
    {
        if (gridParent != null)
        {
            Gizmos.color = Color.yellow;

            Vector3 totalSize = Vector3.Scale(gridSize, prefabSize);
            Gizmos.matrix = Matrix4x4.TRS(gridParent.position, gridParent.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, totalSize);
        }
    }
}
