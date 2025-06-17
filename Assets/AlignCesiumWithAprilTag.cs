using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;

public class AprilTagGeoreferenceAligner : MonoBehaviour
{
    [Header("Geospatial Settings")]
    public CesiumGeoreference geoReference;

    [Tooltip("Longitude in degrees (WGS84)")]
    public double longitude = 139.7528;

    [Tooltip("Latitude in degrees (WGS84)")]
    public double latitude = 35.6852;

    [Tooltip("Height in meters above WGS84 ellipsoid")]
    public double height = 20.0;

    [Header("AprilTag GameObject in Unity")]
    public Transform aprilTagTransform;

    void Start()
    {
        if (geoReference == null || aprilTagTransform == null)
        {
            Debug.LogError("Please assign both geoReference and aprilTagTransform in the Inspector.");
            return;
        }

        AlignCesiumToAprilTag();
    }

    void AlignCesiumToAprilTag()
    {
        // 1. Convert geodetic coordinates (lon, lat, height) to ECEF
        double3 ecef = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(
            new double3(longitude, latitude, height)
        );

        // 2. Convert ECEF to Unity world position (Relative to Cesium origin)
        double3 unityDoublePos = geoReference.TransformEarthCenteredEarthFixedPositionToUnity(ecef);

        // 3. Cast double3 to Vector3
        Vector3 cesiumUnityPos = new Vector3(
            (float)unityDoublePos.x,
            (float)unityDoublePos.y,
            (float)unityDoublePos.z
        );

        // 4. Compute offset between detected AprilTag position and Cesium's computed position
        Vector3 offset = aprilTagTransform.position - cesiumUnityPos;

        // 5. Apply the offset to the Cesium Georeference GameObject
        geoReference.transform.position += offset;

        Debug.Log($" Cesium aligned to AprilTag. Offset: {offset}");
    }
}
