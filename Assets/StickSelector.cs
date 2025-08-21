using UnityEngine;
using System.Collections.Generic;

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

    // Track overlapping cells
    private List<Collider> overlappingCells = new List<Collider>();
    private List<GameObject> selectedCells = new List<GameObject>();

    void Update()
    {
        // Adjust stick length with joystick
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        stickLength += input.y * adjustSpeed * Time.deltaTime;
        stickLength = Mathf.Clamp(stickLength, minLength, maxLength);

        // Move cursor
        cursor.position = controllerTransform.position + controllerTransform.forward * stickLength;

        // On trigger press select first overlapping cell
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            if (overlappingCells.Count > 0)
            {
                Collider selected = overlappingCells[0];
                Transform child = selected.transform.GetChild(0); // assume first child is the visual
                Renderer rend = child.GetComponent<Renderer>();

                if (rend)
                {
                    rend.material = selectedMaterial;
                }

                if (!selectedCells.Contains(selected.gameObject))
                    selectedCells.Add(selected.gameObject);

                Debug.Log("Cell selected: " + selected.gameObject.name);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!overlappingCells.Contains(other))
        {
            overlappingCells.Add(other);

            // Change child material to selectable
            Transform child = other.transform.GetChild(0);
            Renderer rend = child.GetComponent<Renderer>();
            if (rend && !selectedCells.Contains(other.gameObject)) // only if not already selected
            {
                rend.material = selectableMaterial;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (overlappingCells.Contains(other))
        {
            overlappingCells.Remove(other);

            // Reset to transparent if not selected
            Transform child = other.transform.GetChild(0);
            Renderer rend = child.GetComponent<Renderer>();
            if (rend && !selectedCells.Contains(other.gameObject))
            {
                rend.material = transparentMaterial;
            }
        }
    }
}
