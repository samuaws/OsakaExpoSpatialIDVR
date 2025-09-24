using UnityEngine;

public class SpatialIdCell : MonoBehaviour
{
    public string spatialId;   // The spatial ID string (or key)
    public Vector3Int index;   // Optional: the grid index (x,y,z)

    public void Init(string id, Vector3Int gridIndex)
    {
        spatialId = id;
        index = gridIndex;
    }
}
