using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;

public class StickSelector : MonoBehaviour
{
    [Header("Stick Settings")]
    public Transform controllerTransform;
    public Transform cursor;
    public float minLength = 0.2f;
    public float maxLength = 10f;
    public float adjustSpeed = 2f;

    private float stickLength = 2f;

    [Header("Materials")]
    public Material transparentMaterial;
    public Material selectableMaterial;
    public Material selectedMaterial;

    [Header("API Settings")]
    public string apiBaseUrl = "http://localhost:5000/api/attributes/";
    public int zoomLevel = 25;

    // Track overlapping cells
    private List<Collider> overlappingCells = new List<Collider>();
    private List<GameObject> selectedCells = new List<GameObject>();

    public Collider currentHoverCell; // Only one hover at a time

    void Update()
    {
        // Adjust stick length with joystick
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        stickLength += input.y * adjustSpeed * Time.deltaTime;
        stickLength = Mathf.Clamp(stickLength, minLength, maxLength);

        // Move cursor
        cursor.position = controllerTransform.position + controllerTransform.forward * stickLength;
        cursor.rotation = controllerTransform.rotation;

        // Update hover (choose first in overlapping list)
        UpdateHoverCell();

        // On trigger press select current hover cell
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            if (currentHoverCell != null)
            {
                Transform child = currentHoverCell.transform.GetChild(0); // assume first child is the visual
                Renderer rend = child.GetComponent<Renderer>();

                if (rend)
                {
                    rend.material = selectedMaterial;
                }

                if (!selectedCells.Contains(currentHoverCell.gameObject))
                    selectedCells.Add(currentHoverCell.gameObject);

                Debug.Log("Cell selected: " + currentHoverCell.gameObject.name);
            }
        }

        // Left controller button -> send to API
        if (OVRInput.GetDown(OVRInput.Button.Three)) // "X" button on left controller
        {
            SaveSelectedCells();
        }
    }

    private void UpdateHoverCell()
    {
        // Clear old hover
        if (currentHoverCell != null && !selectedCells.Contains(currentHoverCell.gameObject))
        {
            Transform child = currentHoverCell.transform.GetChild(0);
            Renderer rend = child.GetComponent<Renderer>();
            if (rend) rend.material = transparentMaterial;
        }

        // Pick new hover
        currentHoverCell = overlappingCells.Count > 0 ? overlappingCells[0] : null;

        if (currentHoverCell != null && !selectedCells.Contains(currentHoverCell.gameObject))
        {
            Transform child = currentHoverCell.transform.GetChild(0);
            Renderer rend = child.GetComponent<Renderer>();
            if (rend) rend.material = selectableMaterial;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!overlappingCells.Contains(other))
        {
            overlappingCells.Add(other);
            UpdateHoverCell();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (overlappingCells.Contains(other))
        {
            overlappingCells.Remove(other);

            // Reset material if not selected
            if (!selectedCells.Contains(other.gameObject))
            {
                Transform child = other.transform.GetChild(0);
                Renderer rend = child.GetComponent<Renderer>();
                if (rend) rend.material = transparentMaterial;
            }

            UpdateHoverCell();
        }
    }

    private void SaveSelectedCells()
    {
        Debug.Log("Saving selected cells to API...");
        foreach (GameObject cell in new List<GameObject>(selectedCells)) // copy to avoid modifying list while iterating
        {
            SpatialIdCell idCell = cell.GetComponent<SpatialIdCell>();
            if (idCell != null)
            {
                string spatialId = idCell.spatialId;
                StartCoroutine(SendPostRequest(spatialId, cell));
            }
        }
    }

    private IEnumerator SendPostRequest(string spatialId, GameObject cell)
    {
        string url = apiBaseUrl + spatialId;
        Debug.Log("Posting to " + url);

        // Example attributes you want to save (customize this!)
        var payload = new
        {
            zoom_level = zoomLevel,
            attributes = new Dictionary<string, object>
            {
                { "selected", true }
            }
        };

        string jsonData = JsonUtility.ToJson(new Wrapper(payload));

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Successfully saved SpatialID " + spatialId);
                selectedCells.Remove(cell); // remove from temp list -> stays visually selected
            }
            else
            {
                Debug.LogError("Error saving SpatialID " + spatialId + ": " + request.error);
            }
        }
    }

    // Helper to wrap dictionary since JsonUtility does not support it directly
    [System.Serializable]
    private class Wrapper
    {
        public int zoom_level;
        public SerializableDict attributes;

        public Wrapper(object data)
        {
            var dict = (Dictionary<string, object>)((dynamic)data).attributes;
            zoom_level = ((dynamic)data).zoom_level;
            attributes = new SerializableDict(dict);
        }
    }

    [System.Serializable]
    private class SerializableDict
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();

        public SerializableDict(Dictionary<string, object> dict)
        {
            foreach (var kvp in dict)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value.ToString());
            }
        }
    }
}
