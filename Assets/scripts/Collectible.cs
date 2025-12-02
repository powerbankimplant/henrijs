using UnityEngine;

public class Collectible : MonoBehaviour
{
    // Prevent double-collect
    private bool collected;

    // Hover detection (matches door_anim's behavior)
    private Camera mainCamera;
    private bool is_mouse_over = false;
    private float interact_distance = 5f; // same range as door_anim

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        UpdateHoverState();

        if (collected) return;

        // Wait for E key while hovering (same rule as door_anim)
        if (is_mouse_over && Input.GetKeyDown(KeyCode.E))
        {
            Collect();
        }
    }

    // Raycast from camera through mouse position to detect hover (same as door_anim)
    private void UpdateHoverState()
    {
        is_mouse_over = false;

        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, interact_distance))
        {
            // consider hovered if raycast hit this GameObject or a child collider
            if (hit.collider != null && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)))
            {
                is_mouse_over = true;
            }
        }
    }

    private void Collect()
    {
        collected = true;
        Debug.Log($"Collected: {gameObject.name}");
        Destroy(gameObject);
    }
}
