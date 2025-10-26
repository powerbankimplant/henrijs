using UnityEngine;

[RequireComponent(typeof(Light))]
public class flashlight : MonoBehaviour
{
    [Tooltip("Key to toggle the flashlight")]
    public KeyCode toggleKey = KeyCode.F;

    [Tooltip("Start with the light enabled")]
    public bool startOn = true;

    [Tooltip("Optional sound to play when toggling")]
    public AudioClip toggleSound;

    Light lightComp;
    AudioSource audioSource;

    void Awake()
    {
        lightComp = GetComponent<Light>();
        audioSource = GetComponent<AudioSource>();
        lightComp.enabled = startOn;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            lightComp.enabled = !lightComp.enabled;

            if (audioSource != null && toggleSound != null)
                audioSource.PlayOneShot(toggleSound);
        }
    }
}
