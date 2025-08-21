using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using TMPro;
using CesiumForUnity;
using UnityEngine.UI;
using UnityEngine.InputSystem; // New Input System

public class SpatialIdVisualizer : MonoBehaviour
{
    [Header("References")]
    public CesiumGeoreference geoReference;
    public Material boxMaterial; // Default material
    public Material personMaterial;
    public Material chairMaterial;
    public GameObject labelPrefab;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI tagOffest;

    [Header("UI Sliders")]
    public Slider offsetXSlider;
    public Slider offsetYSlider;
    public Slider offsetZSlider;

    [Header("Zoom Filtering")]
    public int targetZoomLevel = 27;

    private Transform referencePoint;
    private List<GameObject> cubes = new List<GameObject>();

    // Controller-based slider selection
    private enum OffsetSliderTarget { X, Y, Z }
    private OffsetSliderTarget selectedSlider = OffsetSliderTarget.X;
    private float sliderStep = 0.01f;
    private float stickDeadZone = 0.2f;
    private float sliderCooldown = 0.2f;
    private float lastSwitchTime = 0f;

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

        tagOffest.text = GameManager.Instance.aprilTagGeoreferenceAligner.tagOffset.ToString();
        HandleSliderControl();

        float offsetX = offsetXSlider != null ? offsetXSlider.value : 0f;
        float offsetY = offsetYSlider != null ? offsetYSlider.value : 0f;
        float offsetZ = offsetZSlider != null ? offsetZSlider.value : 0f;
        Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);

        foreach (var c in cubes)
            Destroy(c);
        cubes.Clear();

        foreach (var detection in NoderedConnector.detections)
        {
            if (detection.spatial_ids == null) continue;
            //if(!GameManager.Instance.anchorLocalised) continue;

            foreach (var sid in detection.spatial_ids)
            {
                if (sid.zoom != targetZoomLevel)
                    continue;

                Vector3 minWorld = SpatialIdToUnityPosition(sid.min_corner);
                Vector3 maxWorld = SpatialIdToUnityPosition(sid.max_corner);

                Vector3 center = (minWorld + maxWorld) / 2f + offset;
                Vector3 size = new Vector3(
                    Mathf.Abs(maxWorld.x - minWorld.x),
                    Mathf.Abs(maxWorld.y - minWorld.y),
                    Mathf.Abs(maxWorld.z - minWorld.z)
                );

                // Box
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Box_{detection.name}_zoom{sid.zoom}";
                cube.transform.position = center + new Vector3(0, 2f, 0f) + GameManager.Instance.aprilTagGeoreferenceAligner.tagOffset;
                cube.transform.localScale = size;

                // Select material
                Material selectedMaterial = boxMaterial;
                string nameLower = detection.name.ToLower();
                if (nameLower.Contains("chair") && chairMaterial != null)
                    selectedMaterial = chairMaterial;
                else if (nameLower.Contains("person") && personMaterial != null)
                    selectedMaterial = personMaterial;

                if (selectedMaterial == null)
                {
                    Debug.LogWarning($"Material for '{detection.name}' is not assigned. Using default boxMaterial.");
                    selectedMaterial = boxMaterial;
                }

                cube.GetComponent<MeshRenderer>().material = selectedMaterial;
                cubes.Add(cube);

                // Label
                if (labelPrefab != null)
                {
                    GameObject label = Instantiate(labelPrefab, center + new Vector3(0, 2f, 0f) + GameManager.Instance.aprilTagGeoreferenceAligner.tagOffset, Quaternion.identity);
                    label.name = $"Label_{detection.name}_zoom{sid.zoom}";
                    label.transform.LookAt(referencePoint);
                    cubes.Add(label);

                    if (label.transform.childCount >= 2)
                    {
                        Transform secondChild = label.transform.GetChild(1);
                        TextMesh textMesh = secondChild.GetComponentInChildren<TextMesh>();
                        if (textMesh != null)
                            textMesh.text = detection.name;
                        else
                            Debug.LogWarning("TextMesh not found in label.");
                    }
                    else
                    {
                        Debug.LogWarning("Label prefab must have at least 2 children.");
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

    void HandleSliderControl()
    {
        if (Time.time - lastSwitchTime > sliderCooldown)
        {
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonEast.wasPressedThisFrame) // B
                {
                    selectedSlider = OffsetSliderTarget.X;
                    lastSwitchTime = Time.time;
                }
                else if (Gamepad.current.buttonWest.wasPressedThisFrame) // X
                {
                    selectedSlider = OffsetSliderTarget.Y;
                    lastSwitchTime = Time.time;
                }
                else if (Gamepad.current.buttonNorth.wasPressedThisFrame) // Y
                {
                    selectedSlider = OffsetSliderTarget.Z;
                    lastSwitchTime = Time.time;
                }
            }
        }

        if (Gamepad.current != null)
        {
            float horizontal = Gamepad.current.leftStick.x.ReadValue();
            if (Mathf.Abs(horizontal) > stickDeadZone)
            {
                float delta = horizontal * sliderStep * Time.deltaTime * 60f;
                switch (selectedSlider)
                {
                    case OffsetSliderTarget.X:
                        if (offsetXSlider != null)
                            offsetXSlider.value += delta;
                        break;
                    case OffsetSliderTarget.Y:
                        if (offsetYSlider != null)
                            offsetYSlider.value += delta;
                        break;
                    case OffsetSliderTarget.Z:
                        if (offsetZSlider != null)
                            offsetZSlider.value += delta;
                        break;
                }
            }
        }
    }
}
