using UnityEngine;

public class door_anim : MonoBehaviour
{
    private Animator doorAnimator;
    // Simple open/closed state
    public bool is_door_open = false;

    // Hover detection
    public bool is_mouse_over = false; // true while mouse is over this door
    private Camera mainCamera;
    private float interact_distance = 5f; // max raycast distance for hovering

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        doorAnimator = GetComponent<Animator>();
        mainCamera = Camera.main;

        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("TrClose");
            is_door_open = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (doorAnimator == null) return;

        UpdateHoverState();

        if (Input.GetKeyDown(KeyCode.E) && is_mouse_over)
        {
            // Toggle based on the boolean state
            if (!is_door_open)
            {
                doorAnimator.SetTrigger("TrOpen");
                is_door_open = true;
            }
            else
            {
                doorAnimator.SetTrigger("TrClose");
                is_door_open = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            doorAnimator.SetTrigger("TrClose");
            is_door_open = false;
        }
    }

    // Raycast from the main camera through the mouse position to detect hover.
    private void UpdateHoverState()
    {
        is_mouse_over = false;

        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, interact_distance))
        {
            // consider the object hovered if the raycast hit this GameObject or a child collider
            if (hit.collider != null && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)))
            {
                is_mouse_over = true;
            }
        }
    }
}
