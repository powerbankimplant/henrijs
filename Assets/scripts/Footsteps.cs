using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Footsteps : MonoBehaviour
{
    public AudioClip footstepClip;
    public float stepDistance = 2f;

    AudioSource audioSource;
    character characterScript;
    Vector3 lastPosition;
    float accumulatedDistance;
    bool wasGrounded;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        characterScript = GetComponent<character>();
        lastPosition = transform.position;
        wasGrounded = characterScript != null ? characterScript.IsGrounded() : true;
    }

    void Update()
    {
        Vector3 currentPos = transform.position;
        Vector3 horizDelta = currentPos - lastPosition;
        horizDelta.y = 0f;
        float moved = horizDelta.magnitude;
        lastPosition = currentPos;

        bool grounded = characterScript != null ? characterScript.IsGrounded() : true;

        // Play landing sound when transitioning from air -> ground (first contact)
        if (!wasGrounded && grounded)
        {
            if (footstepClip != null)
                audioSource.PlayOneShot(footstepClip);
            accumulatedDistance = 0f; // avoid immediate step after landing
            wasGrounded = grounded;
            return;
        }

        // If not grounded, don't accumulate steps
        if (!grounded)
        {
            wasGrounded = grounded;
            return;
        }

        wasGrounded = grounded;

        accumulatedDistance += moved;
        if (accumulatedDistance >= stepDistance)
        {
            if (footstepClip != null)
                audioSource.PlayOneShot(footstepClip);
            accumulatedDistance = 0f;
        }
    }
}