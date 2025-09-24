using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;

public class NoderedConnector : MonoBehaviour
{
    WebSocket node;
    public string url = "wss://home.kenchitaru.studio/ws/unity";
    public bool Connect = true;
    private Thread WebSock1;
    private Thread KeepAlive;
    public static List<DetectionData> detections = new List<DetectionData>();

    void Start()
    {
        node = new WebSocket(url);

        if (Connect)
        {
            node.OnMessage += (sender, e) =>
            {
                try
                {
                    // Deserialize JSON array into a list of DetectionData
                    detections = JsonConvert.DeserializeObject<List<DetectionData>>(e.Data);

                    foreach (var d in detections)
                    {
                        foreach (var sid in d.spatial_ids)
                        {
                            sid.CalculateCenter();
                            Debug.Log("Zoom " + sid.zoom + " Center: " + string.Join(",", sid.center));

                            // Example: get one neighbor (+1 in X)
                            string neighbor = SpatialIdUtility.GetNeighborSpatialId(sid.min_corner, new Vector3Int(1, 0, 0));
                            Debug.Log("Neighbor spatialId (+X): " + neighbor);
                        }

                        Debug.Log("Detected " + d.name + " (confidence: " + d.confidence + ") at LLH: "
                            + d.llh[0] + ", " + d.llh[1] + ", " + d.llh[2]);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("JSON Parse Error: " + ex.Message);
                }
            };

            node.OnError += (sender, e) =>
            {
                Debug.Log("WebSocket Error Message: " + e.Message);
            };

            node.OnClose += (sender, e) =>
            {
                Debug.Log("WebSocket Closed. Attempting reconnect...");
                node.ConnectAsync(); // Might cause infinite reconnect loop - add backoff if needed
            };
        }

        node.ConnectAsync();
    }

    private void OnApplicationQuit()
    {
        node.CloseAsync();
    }
}

// Root object type
[Serializable]
public class DetectionData
{
    public List<SpatialID> spatial_ids;
    public string name;
    public string confidence;
    public List<double> llh; // [lon, lat, height]
    public string timestamp;
}

[Serializable]
public class SpatialID
{
    public int zoom;
    public string min_corner;
    public string max_corner;

    // New field for storing center
    public int[] center;

    // Compute center of spatial ID
    public void CalculateCenter()
    {
        try
        {
            var minParts = min_corner.Split('/');
            var maxParts = max_corner.Split('/');

            int minX = int.Parse(minParts[1]);
            int minY = int.Parse(minParts[2]);
            int minZ = int.Parse(minParts[3]);

            int maxX = int.Parse(maxParts[1]);
            int maxY = int.Parse(maxParts[2]);
            int maxZ = int.Parse(maxParts[3]);

            int centerX = (minX + maxX) / 2;
            int centerY = (minY + maxY) / 2;
            int centerZ = (minZ + maxZ) / 2;

            center = new int[] { centerX, centerY, centerZ };
        }
        catch (Exception ex)
        {
            Debug.LogError("Center calculation failed for SpatialID: " + ex.Message);
            center = new int[] { 0, 0, 0 }; // fallback
        }
    }
}

// Utility functions for Spatial IDs
public static class SpatialIdUtility
{
    // Convert a spatialId string into the center position in Unity world space
    public static Vector3 GetCenterPosition(string spatialId)
    {
        var parts = spatialId.Split('/');
        if (parts.Length < 4)
        {
            Debug.LogError("Invalid spatialId: " + spatialId);
            return Vector3.zero;
        }

        int zoom = int.Parse(parts[0]);
        int x = int.Parse(parts[1]);
        int y = int.Parse(parts[2]);
        int z = int.Parse(parts[3]);

        // For now we just return the indices as coordinates
        // Replace with real conversion if you have LLH -> Unity transform
        return new Vector3(x, y, z);
    }

    // Get a neighboring SpatialID by applying an offset
    public static string GetNeighborSpatialId(string spatialId, Vector3Int offset)
    {
        var parts = spatialId.Split('/');
        if (parts.Length < 4)
        {
            Debug.LogError("Invalid spatialId: " + spatialId);
            return spatialId;
        }

        int zoom = int.Parse(parts[0]);
        int x = int.Parse(parts[1]) + offset.x;
        int y = int.Parse(parts[2]) + offset.y;
        int z = int.Parse(parts[3]) + offset.z;

        return zoom + "/" + x + "/" + y + "/" + z;
    }
}
