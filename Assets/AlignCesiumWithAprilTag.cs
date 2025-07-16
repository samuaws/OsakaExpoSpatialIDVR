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

        //TagUnityPose tagPose = GameManager.Instance.GetSavedTagPose();
        //AlignCesiumToAprilTag(tagPose);
    }

    public void AlignCesiumToAprilTag(TagUnityPose tagPose)
    {
        // 1. Set the georeference origin to the known LLH of the AprilTag
        geoReference.SetOriginLongitudeLatitudeHeight(longitude, latitude, height);

        // 2. Compute where Cesium *thinks* the origin should be in Unity space
        double3 ecef = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(
            new double3(longitude, latitude, height));
        double3 unityPos = geoReference.TransformEarthCenteredEarthFixedPositionToUnity(ecef);

        // 3. Compute offset between AprilTag pose and Cesium origin
        Vector3 cesiumUnityPos = new Vector3(
            (float)unityPos.x,
            (float)unityPos.y,
            (float)unityPos.z);

        Vector3 offset = tagPose.position - cesiumUnityPos;

        // 4. Apply the offset to move Cesium's content into alignment
        geoReference.transform.position += offset;
        geoReference.transform.rotation = tagPose.rotation;

        Debug.Log($"[Cesium] Aligned georeference to AprilTag. Offset: {offset}");
    }
}
