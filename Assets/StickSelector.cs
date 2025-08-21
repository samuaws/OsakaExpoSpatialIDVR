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
}
