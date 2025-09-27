using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
    public string apiBaseUrl = "http://157.82.204.226:5000/api/attributes/";
    public int zoomLevel = 25;

    private List<Collider> overlappingCells = new List<Collider>();
    private List<GameObject> selectedCells = new List<GameObject>();

    public Collider currentHoverCell;

    private static readonly HttpClient httpClient = new HttpClient();

    public void SendTestRequestFromButton()
    {
        _ = SendTestRequestAsync();
    }

    private async Task SendTestRequestAsync()
    {
        string testSpatialId = "25/29/29801115/13210757"; // Replace with a real one
        string url = apiBaseUrl + testSpatialId;

        Debug.Log("Sending test request to: " + url);

        var payload = new Payload
        {
            zoom_level = zoomLevel,
            attributes = new Dictionary<string, object>
            {
                { "selected", true },
                { "source", "unity_test_button" }
            }
        };

        string jsonData = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await httpClient.PostAsync(url, content);
            string responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Debug.Log("Test request success: " + responseText);
            }
            else
            {
                Debug.LogError($"Test request failed ({response.StatusCode}): {responseText}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Test request exception: " + ex.Message);
        }
    }

    void Update()
    {
        // Adjust stick length
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        stickLength += input.y * adjustSpeed * Time.deltaTime;
        stickLength = Mathf.Clamp(stickLength, minLength, maxLength);

        // Cursor follows controller
        cursor.position = controllerTransform.position + controllerTransform.forward * stickLength;
        cursor.rotation = controllerTransform.rotation;

        UpdateHoverCell();

        // Select on trigger
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            if (currentHoverCell != null)
            {
                Transform child = currentHoverCell.transform.GetChild(0);
                Renderer rend = child.GetComponent<Renderer>();
                if (rend) rend.material = selectedMaterial;

                if (!selectedCells.Contains(currentHoverCell.gameObject))
                    selectedCells.Add(currentHoverCell.gameObject);

                Debug.Log("Cell selected: " + currentHoverCell.gameObject.name);
            }
        }

        // Send to API
        if (OVRInput.GetDown(OVRInput.Button.Three)) // "X" button
        {
            SaveSelectedCells();
        }
    }

    private void UpdateHoverCell()
    {
        if (currentHoverCell != null && !selectedCells.Contains(currentHoverCell.gameObject))
        {
            Transform child = currentHoverCell.transform.GetChild(0);
            Renderer rend = child.GetComponent<Renderer>();
            if (rend) rend.material = transparentMaterial;
        }

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
        foreach (GameObject cell in new List<GameObject>(selectedCells))
        {
            SpatialIdCell idCell = cell.GetComponent<SpatialIdCell>();
            if (idCell != null)
            {
                string spatialId = idCell.spatialId;
                _ = SendPostRequestAsync(spatialId, cell);
            }
        }
    }

    private async Task SendPostRequestAsync(string spatialId, GameObject cell)
    {
        string url = apiBaseUrl + spatialId;
        Debug.Log("Posting to " + url);

        var payload = new Payload
        {
            zoom_level = zoomLevel,
            attributes = new Dictionary<string, object>
            {
                { "selected", true }
            }
        };

        string jsonData = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await httpClient.PostAsync(url, content);
            string responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Debug.Log("Successfully saved SpatialID " + spatialId);
                selectedCells.Remove(cell);
            }
            else
            {
                Debug.LogError($"Error saving SpatialID {spatialId}: {response.StatusCode} {responseText}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Post request exception: " + ex.Message);
        }
    }

    private class Payload
    {
        public int zoom_level;
        public Dictionary<string, object> attributes;
    }
}
