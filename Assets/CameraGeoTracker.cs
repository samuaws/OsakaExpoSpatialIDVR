using UnityEngine;
using Unity.Mathematics;
using CesiumForUnity;

public class CameraGeoTracker : MonoBehaviour
{
    [Header("Scene References")]
    public Transform aprilTag;         // AprilTag in-game transform
    public Transform cameraTransform;  // AR/VR headset camera
    public CesiumGeoreference geoRef;  // CesiumGeoreference component

    [Header("Tag Geographic Info")]
    public double tagLongitude = 139.0;
    public double tagLatitude = 35.0;
    public double tagHeight = 14.0; // meters
    public float tagYawDeg = 100.0f; // yaw angle  measured CCW from East

    void Update()
    {
        // 1. Camera position relative to AprilTag (in ENU frame)
        Vector3 localOffset = aprilTag.InverseTransformPoint(cameraTransform.position); // Unity Vector3
        float3 enuOffset = new float3(localOffset.x, localOffset.y, localOffset.z);     // convert to float3

        // 2. Convert tag LLH to ECEF
        double3 tagLLH = new double3(tagLongitude, tagLatitude, tagHeight);
        double3 tagECEF = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(tagLLH);

        // 3. Create ENU to ECEF rotation matrix using tag yaw
        float yawRad = math.radians(tagYawDeg);
        quaternion rot = quaternion.AxisAngle(math.up(), yawRad);
        float3 east = math.mul(rot, new float3(1, 0, 0));
        float3 north = math.mul(rot, new float3(0, 0, 1));
        float3 up = math.mul(rot, new float3(0, 1, 0));
        float3x3 enuToECEF = new float3x3(east, north, up);

        // 4. Rotate ENU offset and convert to double3
        float3 ecefOffsetFloat = math.mul(enuToECEF, enuOffset);
        double3 ecefOffset = new double3(ecefOffsetFloat.x, ecefOffsetFloat.y, ecefOffsetFloat.z);

        // 5. Compute camera ECEF
        double3 cameraECEF = tagECEF + ecefOffset;

        // 6. Convert to geographic coordinates
        double3 cameraLLH = CesiumWgs84Ellipsoid.EarthCenteredEarthFixedToLongitudeLatitudeHeight(cameraECEF);

        // 7. Optional: convert to Unity world space (if needed)
        double3 cameraUnity = geoRef.TransformEarthCenteredEarthFixedPositionToUnity(cameraECEF);

        // 8. Log result
        Debug.Log($" Camera Position (LLH): Lat={cameraLLH.y:F6}, Lon={cameraLLH.x:F6}, Alt={cameraLLH.z:F2}m");
    }
}
