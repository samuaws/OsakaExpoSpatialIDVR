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
                        Debug.Log($" Detected {d.name} (confidence: {d.confidence}) at LLH: {d.llh[0]}, {d.llh[1]}, {d.llh[2]}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($" JSON Parse Error: {ex.Message}");
                }
            };

            node.OnError += (sender, e) =>
            {
                Debug.Log("WebSocket Error Message: " + e.Message);
            };

            node.OnClose += (sender, e) =>
            {
                Debug.Log("WebSocket Closed. Attempting reconnect...");
                node.ConnectAsync(); // Might cause infinite reconnect loop – add backoff if needed
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
}
