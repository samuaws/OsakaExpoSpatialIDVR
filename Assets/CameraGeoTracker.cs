using UnityEngine;
using Unity.Mathematics;
using CesiumForUnity;
using System.Collections;

public class CameraGeoTracker : MonoBehaviour
{
    [Header("Scene References")]
    public CesiumGeoreference geoRef; // Cesium georeference in scene
    public OVRCameraRig ovrRig;       // Assigned via inspector

    [Header("Tag Geographic Info")]
    public double tagLongitude = 139.0;
    public double tagLatitude = 35.0;
    public double tagHeight = 14.0; // meters
    public float tagYawDeg = 100.0f; // yaw measured counter-clockwise from East

    private bool hasAligned = false;

    void Start()
    {
        StartCoroutine(AlignAfterDelay());
        
    }

    IEnumerator AlignAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);
        AlignCesiumToAprilTag();
    }

    void AlignCesiumToAprilTag()
    {
        if (geoRef == null || ovrRig == null)
        {
            Debug.LogError("CesiumGeoreference or OVRCameraRig not assigned.");
            return;
        }

        TagUnityPose tagPose = GameManager.Instance.GetSavedTagPose();
        Transform centerEye = ovrRig.centerEyeAnchor;
        Vector3 headsetPosition = centerEye.position;

        Vector3 localOffset = Quaternion.Inverse(tagPose.rotation) * (headsetPosition - tagPose.position);
        float3 enuOffset = new float3(localOffset.x, localOffset.y, localOffset.z);

        double3 tagLLH = new double3(tagLongitude, tagLatitude, tagHeight);
        double3 tagECEF = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(tagLLH);

        float yawRad = math.radians(tagYawDeg);
        quaternion rot = quaternion.AxisAngle(math.up(), yawRad);
        float3 east = math.mul(rot, new float3(1, 0, 0));
        float3 north = math.mul(rot, new float3(0, 0, 1));
        float3 up = math.mul(rot, new float3(0, 1, 0));
        float3x3 enuToECEF = new float3x3(east, north, up);

        float3 ecefOffsetF = math.mul(enuToECEF, enuOffset);
        double3 cameraECEF = tagECEF + new double3(ecefOffsetF.x, ecefOffsetF.y, ecefOffsetF.z);

        double3 unityHeadsetShouldBe = geoRef.TransformEarthCenteredEarthFixedPositionToUnity(cameraECEF);
        Vector3 correctUnityHeadsetPos = new Vector3(
            (float)unityHeadsetShouldBe.x,
            (float)unityHeadsetShouldBe.y,
            (float)unityHeadsetShouldBe.z
        );

        Vector3 currentHeadsetPos = centerEye.position;
        Vector3 offset = currentHeadsetPos - correctUnityHeadsetPos;
        geoRef.transform.position += offset;
        geoRef.transform.rotation = tagPose.rotation;

        Debug.Log($"Cesium aligned. Offset applied: {offset}");
        hasAligned = true;
    }

    void Update()
    {
        if (!hasAligned)
            return;

        TagUnityPose tagPose = GameManager.Instance.GetSavedTagPose();
        Transform centerEye = ovrRig.centerEyeAnchor;

        Vector3 localOffset = Quaternion.Inverse(tagPose.rotation) * (centerEye.position - tagPose.position);
        float3 enuOffset = new float3(localOffset.x, localOffset.y, localOffset.z);

        float yawRad = math.radians(tagYawDeg);
        quaternion rot = quaternion.AxisAngle(math.up(), yawRad);
        float3x3 enuToECEF = new float3x3(
            math.mul(rot, new float3(1, 0, 0)),
            math.mul(rot, new float3(0, 0, 1)),
            math.mul(rot, new float3(0, 1, 0))
        );

        float3 ecefOffsetF = math.mul(enuToECEF, enuOffset);
        double3 tagLLH = new double3(tagLongitude, tagLatitude, tagHeight);
        double3 tagECEF = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(tagLLH);
        double3 cameraECEF = tagECEF + new double3(ecefOffsetF.x, ecefOffsetF.y, ecefOffsetF.z);

        double3 cameraLLH = CesiumWgs84Ellipsoid.EarthCenteredEarthFixedToLongitudeLatitudeHeight(cameraECEF);
        Debug.Log($"Camera LLH: Latitude={cameraLLH.y:F6}, Longitude={cameraLLH.x:F6}, Altitude={cameraLLH.z:F2} meters");
    }
}
