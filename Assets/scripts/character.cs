using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class character : MonoBehaviour
{
    [Tooltip("Horizontal movement speed (m/s)")]
    public float speed = 5f;

    [Tooltip("Upwards impulse applied when jumping")]
    public float jumpForce = 5f;

    [Tooltip("Extra distance for the ground check")]
    public float groundCheckExtra = 0.05f;

    [Tooltip("Reference to the camera (used to orient movement). If left empty, Camera.main will be used at runtime.")]
    public Transform cameraTransform;

    Rigidbody rb;
    CapsuleCollider capsule;

    // cached input
    float inputH;
    float inputV;
    bool jumpRequest;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        // fallback to Camera.main if cameraTransform wasn't assigned
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // read player input on main thread
        inputH = Input.GetAxis("Horizontal");
        inputV = Input.GetAxis("Vertical");

        if (Input.GetButtonDown("Jump"))
            jumpRequest = true;
    }

    void FixedUpdate()
    {
        // ensure we have a camera reference
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Build camera-relative movement (flatten camera forward on Y axis)
        Vector3 camForward = (cameraTransform != null) ? cameraTransform.forward : transform.forward;
        Vector3 forward = Vector3.ProjectOnPlane(camForward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Vector3 rawInput = new Vector3(inputH, 0f, inputV);
        Vector3 move = forward * inputV + right * inputH;
        if (move.sqrMagnitude > 1f) move.Normalize();

        Vector3 desiredVelocity = move * speed;

        // preserve vertical velocity (use project's linearVelocity API)
        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(desiredVelocity.x, v.y, desiredVelocity.z);

        // jump
        bool isGrounded = IsGrounded();
        if (jumpRequest && isGrounded)
        {
            v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(v.x, jumpForce, v.z);
        }

        jumpRequest = false;
    }

    public bool IsGrounded()
    {
        if (capsule != null)
        {
            Vector3 bottom = transform.position + capsule.center - Vector3.up * (capsule.height * 0.5f - capsule.radius);
            float checkDistance = capsule.radius + groundCheckExtra;
            return Physics.SphereCast(bottom, capsule.radius * 0.95f, Vector3.down, out _, checkDistance);
        }
        else
        {
            return Physics.Raycast(transform.position, Vector3.down, 1.1f);
        }
    }
}