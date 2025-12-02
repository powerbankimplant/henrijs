using UnityEngine;

public class mouselook : MonoBehaviour
{
    [Tooltip("Mouse sensitivity")]
    public float sensitivity = 200f;

    [Tooltip("Invert vertical look")]
    public bool invertY = false;

    [Tooltip("Lock cursor on start")]
    public bool lockCursor = true;

    float pitch = 0f;
    float yaw = 0f;
    Texture2D transparentCursor;

    void Start()
    {
        // initialize yaw from current rotation so we don't snap
        Vector3 e = transform.localEulerAngles;
        yaw = e.y;
        pitch = e.x;

        CreateTransparentCursor();
        if (lockCursor) SetCursorLocked(true);
    }

    void Update()
    {
        // Toggle cursor lock state with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleCursorLock();

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetMouseButtonDown(0))
                SetCursorLocked(true);
            return;
        }

        // read raw mouse deltas and accumulate
        float mx = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime * (invertY ? 1f : -1f);

        yaw += mx;
        pitch += my;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
    }

    void LateUpdate()
    {
        // apply pitch and yaw on the camera only
        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void ToggleCursorLock() => SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        Cursor.SetCursor(locked ? transparentCursor : null, Vector2.zero, CursorMode.Auto);
    }

    void CreateTransparentCursor()
    {
        transparentCursor = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        transparentCursor.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
        transparentCursor.Apply();
    }
}