using UnityEngine;
using Unity.Mathematics;
using CesiumForUnity;

public class CameraGeoTracker : MonoBehaviour
{
    [Header("Scene References")]
    public CesiumGeoreference geoRef; // Cesium georeference in scene
    public OVRCameraRig ovrRig;       // Assigned via inspector

    [Header("Tag Geographic Info")]
    public double tagLongitude = 139.0;
    public double tagLatitude = 35.0;
    public double tagHeight = 14.0; // meters
    public float tagYawDeg = 100.0f; // yaw measured CCW from East

    void Update()
    {
        // 0. Get saved AprilTag Unity Pose
        TagUnityPose tagPose = GameManager.Instance.GetSavedTagPose();

        // 1. Get headset world pose from OVR
        Transform centerEye = ovrRig.centerEyeAnchor;
        Vector3 headsetPosition = centerEye.position;

        // 2. Calculate local offset in AprilTag's ENU frame
        Vector3 localOffset = Quaternion.Inverse(tagPose.rotation) * (headsetPosition - tagPose.position);
        float3 enuOffset = new float3(localOffset.x, localOffset.y, localOffset.z);

        // 3. Convert tag LLH to ECEF
        double3 tagLLH = new double3(tagLongitude, tagLatitude, tagHeight);
        double3 tagECEF = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(tagLLH);

        // 4. Create ENU to ECEF rotation matrix from yaw
        float yawRad = math.radians(tagYawDeg);
        quaternion rot = quaternion.AxisAngle(math.up(), yawRad);
        float3 east = math.mul(rot, new float3(1, 0, 0));
        float3 north = math.mul(rot, new float3(0, 0, 1));
        float3 up = math.mul(rot, new float3(0, 1, 0));
        float3x3 enuToECEF = new float3x3(east, north, up);

        // 5. Rotate ENU offset to ECEF
        float3 ecefOffsetF = math.mul(enuToECEF, enuOffset);
        double3 ecefOffset = new double3(ecefOffsetF.x, ecefOffsetF.y, ecefOffsetF.z);

        // 6. Add to get camera ECEF
        double3 cameraECEF = tagECEF + ecefOffset;

        // 7. Optionally: convert to Unity world coordinates
        double3 unityPos = geoRef.TransformEarthCenteredEarthFixedPositionToUnity(cameraECEF);

        // 8. Debug output
        double3 cameraLLH = CesiumWgs84Ellipsoid.EarthCenteredEarthFixedToLongitudeLatitudeHeight(cameraECEF);
        Debug.Log($" Camera LLH: Lat={cameraLLH.y:F6}, Lon={cameraLLH.x:F6}, Alt={cameraLLH.z:F2}m");
    }
}
