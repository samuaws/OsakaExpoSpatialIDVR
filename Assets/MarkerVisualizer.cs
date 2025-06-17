using OpenCVForUnity.CoreModule;
using Meta.XR.Samples; // Assuming PassthroughCameraUtils and PassthroughCameraEye come from here
using PassthroughCameraSamples;
using Meta.XR;
using PassthroughCameraSamples.MultiObjectDetection;
using TryAR.MarkerTracking;
using System.Collections.Generic;
using UnityEngine;

public class MarkerVisualizer : MonoBehaviour
{
    public ChArUcoMarkerTracking markerTracking;   // Your marker tracking script reference
    public GameObject markerPrefab;                 // Prefab to instantiate (cube, etc.)
    public WebCamTextureManager webCamTextureManager; // To get the PassthroughCameraEye instance
    public EnvironmentRayCastSampleManager m_environmentRaycast;  // Your environment raycast helper

    private List<GameObject> _spawnedMarkers = new List<GameObject>();

    // Get the PassthroughCameraEye from WebCamTextureManager
    private PassthroughCameraEye CameraEye => webCamTextureManager.Eye;

    void Update()
    {
        if (!markerTracking.IsReady || markerTracking._detectedMarkerCorners.Count == 0)
            return;

        // Destroy old markers
        foreach (var obj in _spawnedMarkers)
            Destroy(obj);
        _spawnedMarkers.Clear();

        // Get camera intrinsics and resolution using PassthroughCameraEye
        var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(CameraEye);
        var camRes = intrinsics.Resolution;

        foreach (Mat corners in markerTracking._detectedMarkerCorners)
        {
            if (corners.rows() != 1 || corners.cols() != 4)
                continue;

            // Read corner points of the marker
            Point[] pts = new Point[4];
            for (int i = 0; i < 4; i++)
                pts[i] = new Point(corners.get(0, i));

            // Average to get center pixel in image space
            double centerX = (pts[0].x + pts[1].x + pts[2].x + pts[3].x) / 4.0;
            double centerY = (pts[0].y + pts[1].y + pts[2].y + pts[3].y) / 4.0;

            // Convert center pixel to int and clamp to camera resolution
            int pixelX = Mathf.Clamp(Mathf.RoundToInt((float)centerX), 0, camRes.x - 1);
            int pixelY = Mathf.Clamp(Mathf.RoundToInt((float)centerY), 0, camRes.y - 1);

            // Flip Y because OpenCV origin is top-left but Unity uses bottom-left
            var centerPixel = new Vector2Int(pixelX, camRes.y - pixelY - 1);

            // Create a ray from screen point in world space using PassthroughCameraEye
            Ray ray = PassthroughCameraUtils.ScreenPointToRayInWorld(CameraEye, centerPixel);

            // Raycast into environment to get the actual world position
            Vector3 worldPos = (Vector3)m_environmentRaycast.PlaceGameObjectByScreenPos(ray);

            // Instantiate prefab at that world position
            GameObject marker = Instantiate(markerPrefab, worldPos, Quaternion.identity);
            _spawnedMarkers.Add(marker);
        }
    }
}
