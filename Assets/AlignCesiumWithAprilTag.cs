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

    void Start()
    {
        if (geoReference == null)
        {
            Debug.LogError("CesiumGeoreference is not assigned.");
            return;
        }

        TagUnityPose tagPose = GameManager.Instance.GetSavedTagPose();
       // AlignCesiumToAprilTag(tagPose);
    }

    public void AlignCesiumToAprilTag(TagUnityPose tagPose)
    {
        // 1. Convert geodetic coordinates to ECEF
        double3 ecef = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(
            new double3(longitude, latitude, height)
        );

        // 2. Convert ECEF to Unity coordinates
        double3 unityDoublePos = geoReference.TransformEarthCenteredEarthFixedPositionToUnity(ecef);

        // 3. Convert to Vector3
        Vector3 cesiumUnityPos = new Vector3(
            (float)unityDoublePos.x,
            (float)unityDoublePos.y,
            (float)unityDoublePos.z
        );

        // 4. Calculate offset between AprilTag pose and Cesium reference position
        Vector3 offset = tagPose.position - cesiumUnityPos;

        // 5. Apply the offset to Cesium's origin
        geoReference.transform.position += offset;

        Debug.Log($" Cesium aligned to AprilTag. Offset: {offset}");
    }
}
