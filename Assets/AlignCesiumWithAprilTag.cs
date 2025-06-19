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
        AlignCesiumToAprilTag(tagPose);
    }

    public void AlignCesiumToAprilTag(TagUnityPose tagPose)
    {
        // 1. Convert LLH to ECEF
        double3 ecef = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(
            new double3(longitude, latitude, height)
        );

        // 2. Set Cesium origin at LLH location
        geoReference.SetOriginLongitudeLatitudeHeight(longitude, latitude, height);

        // 3. Place the Unity origin (0,0,0) at the AprilTag pose
        geoReference.transform.position = tagPose.position;
        geoReference.transform.rotation = tagPose.rotation;

        Debug.Log($"Cesium georeference aligned to AprilTag pose at {tagPose.position}");
    }
}
