using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using TMPro;
using CesiumForUnity;

public class SpatialIdVisualizer : MonoBehaviour
{
    [Header("References")]
    public CesiumGeoreference geoReference;
    public Material boxMaterial;
    public GameObject labelPrefab;
    public TextMeshProUGUI distanceText;

    [Header("Zoom Filtering")]
    public int targetZoomLevel = 27;

    private Transform referencePoint;
    private List<GameObject> cubes = new List<GameObject>();

    void Start()
    {
        referencePoint = Camera.main?.transform;
        if (referencePoint == null)
            Debug.LogWarning("Headset (Main Camera) not found.");
    }

    void Update()
    {
        if (geoReference == null || NoderedConnector.detections == null)
            return;

        // Clear previous cubes
        foreach (var c in cubes)
            Destroy(c);
        cubes.Clear();

        foreach (var detection in NoderedConnector.detections)
        {
            if (detection.spatial_ids == null) continue;

            foreach (var sid in detection.spatial_ids)
            {
                if (sid.zoom != targetZoomLevel)
                    continue;

                Vector3 minWorld = SpatialIdToUnityPosition(sid.min_corner);
                Vector3 maxWorld = SpatialIdToUnityPosition(sid.max_corner);

                Vector3 center = (minWorld + maxWorld) / 2f;
                Vector3 size = new Vector3(
                    Mathf.Abs(maxWorld.x - minWorld.x),
                    Mathf.Abs(maxWorld.y - minWorld.y),
                    Mathf.Abs(maxWorld.z - minWorld.z)
                );

                // Box
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Box_{detection.name}_zoom{sid.zoom}";
                cube.transform.position = center;
                cube.transform.localScale = size;
                cube.GetComponent<MeshRenderer>().material = boxMaterial;
                cubes.Add(cube);

                // Label Prefab
                if (labelPrefab != null)
                {
                    GameObject label = Instantiate(labelPrefab, center, Quaternion.identity);
                    label.name = $"Label_{detection.name}_zoom{sid.zoom}";
                    label.transform.LookAt(referencePoint);
                    cubes.Add(label); // So it also gets cleared on next frame

                    // Enter second child and modify TextMesh
                    if (label.transform.childCount >= 2)
                    {
                        Transform secondChild = label.transform.GetChild(1);
                        TextMesh textMesh = secondChild.GetComponentInChildren<TextMesh>();

                        if (textMesh != null)
                            textMesh.text = detection.name;
                        else
                            Debug.LogWarning("TextMesh not found in second child of label prefab.");
                    }
                    else
                    {
                        Debug.LogWarning("Label prefab must have at least two children.");
                    }
                }
            }
        }

        // Distance display
        if (cubes.Count == 0)
        {
            if (distanceText != null)
                distanceText.text = "No boxes created";
        }
        else if (referencePoint != null && distanceText != null)
        {
            float minDistance = float.MaxValue;
            foreach (var cube in cubes)
            {
                float dist = Vector3.Distance(referencePoint.position, cube.transform.position);
                if (dist < minDistance)
                    minDistance = dist;
            }

            distanceText.text = $"Closest box: {minDistance:F2}m";
        }
    }

    Vector3 SpatialIdToUnityPosition(string sid)
    {
        var parts = sid.Split('/');
        if (parts.Length != 4)
        {
            Debug.LogError("Invalid spatial ID format: " + sid);
            return Vector3.zero;
        }

        double zoom = double.Parse(parts[0]);
        double x = double.Parse(parts[1]);
        double y = double.Parse(parts[2]);
        double z = double.Parse(parts[3]);

        double n = math.PI - 2.0 * math.PI * y / Math.Pow(2.0, zoom);
        double latitude = 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
        double longitude = x / Math.Pow(2.0, zoom) * 360.0 - 180.0;
        double height = z;

        double3 ecef = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(
            new double3(longitude, latitude, height)
        );

        double3 unityPos = geoReference.TransformEarthCenteredEarthFixedPositionToUnity(ecef);
        return new Vector3((float)unityPos.x, (float)unityPos.y, (float)unityPos.z);
    }
}
