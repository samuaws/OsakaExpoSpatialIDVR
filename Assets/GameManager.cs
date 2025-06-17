using System.Collections.Generic;
using UnityEngine;
using TryAR.MarkerTracking;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Reference to the ArUco Tracking Coordinator")]
    public ArUcoTrackingAppCoordinator arucoCoordinator;
    public TextMeshProUGUI unityPosText;

    private bool aButtonWasPressed = false;

    // Struct to store position and rotation
    public struct TagUnityPose
    {
        public Vector3 position;
        public Quaternion rotation;

        public TagUnityPose(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }

        public override string ToString()
        {
            return $"Position: {position}, Rotation (Euler): {rotation.eulerAngles}";
        }
    }

    private TagUnityPose savedTagPose;

    void Awake()
    {
        // Singleton logic
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Update()
    {
        if (OVRInput.Get(OVRInput.Button.One) && !aButtonWasPressed)
        {
            aButtonWasPressed = true;
            if (arucoCoordinator.gameObject.activeSelf)
            {
                SaveFirstMarkerPose();
            }

            ToggeleTracking();
        }

        if (OVRInput.GetUp(OVRInput.Button.One))
        {
            aButtonWasPressed = false;
        }
    }

    /// <summary>
    /// Saves the position and rotation of the first GameObject tracked by ArUco
    /// </summary>
    private void SaveFirstMarkerPose()
    {
        Dictionary<int, GameObject> markerDict = arucoCoordinator.m_markerGameObjectDictionary;

        if (markerDict != null && markerDict.Count > 0)
        {
            foreach (var pair in markerDict)
            {
                GameObject trackedObj = pair.Value;

                if (trackedObj != null)
                {
                    Vector3 pos = trackedObj.transform.position;
                    Quaternion rot = trackedObj.transform.rotation;

                    savedTagPose = new TagUnityPose(pos, rot);
                    Debug.Log($"[Saved Tag Pose] {savedTagPose}");
                    unityPosText.text = savedTagPose.ToString();
                    if(trackedObj.TryGetComponent<OVRSpatialAnchor>(out OVRSpatialAnchor comp)) Destroy(comp);
                    trackedObj.AddComponent<OVRSpatialAnchor>();
                }
                else
                {
                    Debug.LogWarning("Tracked GameObject is null.");
               
                }

                break; // Only save the first one
            }
        }
        else
        {
            Debug.LogWarning("Marker dictionary is empty.");
        }
    }
    void ToggeleTracking()
    {
        arucoCoordinator.gameObject.SetActive(!arucoCoordinator.gameObject.activeSelf);
    }
    

    public TagUnityPose GetSavedTagPose()
    {
        return savedTagPose;
    }
}
