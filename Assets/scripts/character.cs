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

    [Tooltip("Reference to the camera (used to orient movement)")]
    public Transform cameraTransform;

    [Tooltip("Degrees per second the player will rotate to face camera yaw")]
    public float rotationSpeed = 720f;

    [Tooltip("Minimum input magnitude to trigger rotation")]
    public float rotateThreshold = 0.01f;

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
        // compute movement direction relative to camera yaw (keeps movement intuitive)
        float yaw = (cameraTransform != null) ? cameraTransform.eulerAngles.y : transform.eulerAngles.y;
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);

        Vector3 rawInput = new Vector3(inputH, 0f, inputV);
        Vector3 move = yawRot * rawInput;
        if (move.sqrMagnitude > 1f) move.Normalize();

        Vector3 desiredVelocity = move * speed;

        // preserve vertical velocity (use project's linearVelocity API)
        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(desiredVelocity.x, v.y, desiredVelocity.z);

        // rotate player to face camera yaw when there's input
        if (rawInput.sqrMagnitude > (rotateThreshold * rotateThreshold))
        {
            Quaternion current = rb.rotation;
            Quaternion target = Quaternion.Euler(0f, yaw, 0f);
            Quaternion next = Quaternion.RotateTowards(current, target, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(next);
        }

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