using UnityEngine;
using UnityEngine.UI;

public class crosshair : MonoBehaviour
{
    [Header("Assign one (UI or Sprite)")]
    public SpriteRenderer spriteCrosshair;
    public Image uiCrosshair;

    [Header("Detection")]
    public string interactableTag = "Interactable";
    public float interactDistance = 5f;
    public bool requireCursorLocked = true; // set false if you want the crosshair to work while cursor is unlocked

    Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        SetCrosshair(false);
    }

    void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        if (requireCursorLocked && Cursor.lockState != CursorLockMode.Locked)
        {
            SetCrosshair(false);
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        bool show = false;

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            if (hit.collider != null && HasInteractableTag(hit.collider.transform))
                show = true;
        }

        SetCrosshair(show);
    }

    // Walk up the transform chain to allow tag on parent/root
    bool HasInteractableTag(Transform t)
    {
        if (string.IsNullOrEmpty(interactableTag)) return false;
        while (t != null)
        {
            if (t.CompareTag(interactableTag)) return true;
            t = t.parent;
        }
        return false;
    }

    void SetCrosshair(bool on)
    {
        if (spriteCrosshair != null && spriteCrosshair.enabled != on)
            spriteCrosshair.enabled = on;

        // Toggle the Image component rather than the whole GameObject so UI layout remains intact
        if (uiCrosshair != null && uiCrosshair.enabled != on)
            uiCrosshair.enabled = on;
    }
}