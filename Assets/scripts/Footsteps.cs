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

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        characterScript = GetComponent<character>();
        lastPosition = transform.position;
    }

    void Update()
    {
        Vector3 currentPos = transform.position;
        Vector3 horizDelta = currentPos - lastPosition;
        horizDelta.y = 0f;
        float moved = horizDelta.magnitude;
        lastPosition = currentPos;

        if (characterScript != null && !characterScript.IsGrounded()) return;

        accumulatedDistance += moved;
        if (accumulatedDistance >= stepDistance)
        {
            if (footstepClip != null)
                audioSource.PlayOneShot(footstepClip);
            accumulatedDistance = 0f;
        }
    }
}