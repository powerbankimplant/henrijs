using UnityEngine;

public class character : MonoBehaviour
{
    [Tooltip("Horizontal movement speed (m/s)")]
    public float speed = 5f;

    [Tooltip("Upwards impulse applied when jumping")]
    public float jumpForce = 5f;

    [Tooltip("Extra distance for the ground check")]
    public float groundCheckExtra = 0.05f;

    Rigidbody rb;
    CapsuleCollider capsule;

    // cache input so we read it in Update and apply in FixedUpdate
    float inputH;
    float inputV;
    bool jumpRequest;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        // Use physics for movement; prevent physics from rotating the player
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
    }

    void Update()
    {
        // Read input on main thread
        inputH = Input.GetAxis("Horizontal");
        inputV = Input.GetAxis("Vertical");

        if (Input.GetButtonDown("Jump"))
            jumpRequest = true;
    }

    void FixedUpdate()
    {
        // Build movement vector relative to player orientation
        Vector3 move = (transform.forward * inputV + transform.right * inputH);
        if (move.sqrMagnitude > 1f) move.Normalize();

        // Apply horizontal velocity while preserving vertical velocity (gravity/jumps)
        Vector3 desiredVelocity = move * speed;
        rb.linearVelocity = new Vector3(desiredVelocity.x, rb.linearVelocity.y, desiredVelocity.z);

        // Ground check using capsule bounds
        bool isGrounded = IsGrounded();

        // Handle jump
        if (jumpRequest && isGrounded)
        {
            // set vertical velocity directly for a crisp jump
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }

        // reset jump request; it should only be consumed once
        jumpRequest = false;
    }

    public bool IsGrounded()
    {
        if (capsule != null)
        {
            // bottom of capsule in world space
            Vector3 bottom = transform.position + capsule.center - Vector3.up * (capsule.height * 0.5f - capsule.radius);
            float checkDistance = capsule.radius + groundCheckExtra;
            return Physics.SphereCast(bottom, capsule.radius * 0.95f, Vector3.down, out _, checkDistance);
        }
        else
        {
            // fallback raycast
            return Physics.Raycast(transform.position, Vector3.down, 1.1f);
        }
    }
}