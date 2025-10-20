using UnityEngine;

public class mouselook : MonoBehaviour
{
    [Tooltip("Mouse sensitivity")]
    public float sensitivity = 200f;

    [Tooltip("Invert vertical look")]
    public bool invertY = false;

    [Tooltip("Player transform to rotate for yaw. If left empty, parent will be used.")]
    public Transform playerBody;

    float pitch = 0f;
    Texture2D transparentCursor;

    void Start()
    {
        if (playerBody == null && transform.parent != null)
            playerBody = transform.parent;

        CreateTransparentCursor();
        SetCursorLocked(true);
    }

    void Update()
    {
        // Toggle cursor lock state with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }

        // If cursor is unlocked, allow re-lock by left click but don't rotate the view
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetMouseButtonDown(0))
                SetCursorLocked(true);
            return;
        }

        // Read mouse input and apply look when cursor is locked
        float mx = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime * (invertY ? 1f : -1f);

        // yaw: rotate player around Y
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mx, Space.Self);

        // pitch: rotate camera locally around X and clamp
        pitch += my;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        transform.localEulerAngles = Vector3.right * pitch;
    }

    void ToggleCursorLock()
    {
        SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);
    }

    void SetCursorLocked(bool locked)
    {
        // lock state + visible
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;

        // Also set a transparent hardware/software cursor when locked.
        // Some platforms/editors still show the OS cursor; forcing a transparent cursor hides it.
        Cursor.SetCursor(locked ? transparentCursor : null, Vector2.zero, CursorMode.Auto);
    }

    void CreateTransparentCursor()
    {
        // 1x1 transparent texture used to effectively hide the cursor on platforms that
        // still draw an OS cursor even when Cursor.visible = false.
        transparentCursor = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        transparentCursor.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
        transparentCursor.Apply();
    }
}