using UnityEngine;

public class phone_bob : MonoBehaviour
{
    [Header("Bobbing")]
    [SerializeField] private float amplitude = 0.05f;      // vertical/horizontal distance of bob
    [SerializeField] private float frequency = 4f;         // how fast it bobs (cycles per second)
    [SerializeField] private float smoothSpeed = 10f;      // how fast the phone interpolates to target
    [SerializeField] private float moveThreshold = 0.01f;  // minimum velocity / input to count as moving

    [Header("Player detection (optional)")]
    [Tooltip("If set, movement is detected from this transform (prefers Rigidbody/CharacterController). If left empty, Input axes 'Horizontal'/'Vertical' are used.")]
    [SerializeField] private Transform player;

    private Vector3 initialLocalPos;
    private float bobTimer;
    private Vector3 lastPlayerPos;
    private Vector3 velocityRef; // for SmoothDamp if you prefer
    private bool hasRigidbody;
    private bool hasCharacterController;
    private Rigidbody cachedRigidbody;
    private CharacterController cachedController;

    void Start()
    {
        initialLocalPos = transform.localPosition;
        bobTimer = 0f;

        if (player != null)
        {
            cachedRigidbody = player.GetComponent<Rigidbody>();
            hasRigidbody = (cachedRigidbody != null);

            cachedController = player.GetComponent<CharacterController>();
            hasCharacterController = (cachedController != null);

            lastPlayerPos = player.position;
        }
    }

    void Update()
    {
        bool isMoving = DetectPlayerMoving();
        float targetY = initialLocalPos.y;
        float targetX = initialLocalPos.x;

        if (isMoving)
        {
            // advance bob timer and compute sinusoidal offset
            bobTimer += Time.deltaTime * frequency * Mathf.PI * 2f; // convert frequency (Hz) to radians/sec
            float offset = Mathf.Sin(bobTimer) * amplitude;
            targetY = initialLocalPos.y + offset;
            targetX = initialLocalPos.x + offset; // same movement applied to X axis
        }
        else
        {
            // gently reset timer so that bob doesn't jump when resuming
            bobTimer = 0f;
        }

        // smooth interpolation of local position (preserve z)
        Vector3 current = transform.localPosition;
        Vector3 targetLocal = new Vector3(targetX, targetY, initialLocalPos.z);
        transform.localPosition = Vector3.Lerp(current, targetLocal, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
    }

    private bool DetectPlayerMoving()
    {
        // If a player transform is assigned, prefer reading velocity from components
        if (player != null)
        {
            if (hasRigidbody && cachedRigidbody != null)
            {
                return cachedRigidbody.linearVelocity.sqrMagnitude > (moveThreshold * moveThreshold);
            }

            if (hasCharacterController && cachedController != null)
            {
                return cachedController.velocity.sqrMagnitude > (moveThreshold * moveThreshold);
            }

            // fallback: measure positional delta between frames
            Vector3 delta = (player.position - lastPlayerPos) / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPlayerPos = player.position;
            return delta.sqrMagnitude > (moveThreshold * moveThreshold);
        }

        // No player assigned: use input axes (works for default Unity input)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        return (h * h + v * v) > (moveThreshold * moveThreshold);
    }
}
