using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomNumberGenerator : MonoBehaviour
{
    [Header("Per-number sprites (index 0 -> number 1, index 4 -> number 5)")]
    [SerializeField] private Sprite[] numberSprites = new Sprite[5];

    [Header("Per-number prefabs (index 0 -> number 1, index 4 -> number 5)")]
    [Tooltip("Optional: if assigned for a number, the prefab will be instantiated.")]
    [SerializeField] private GameObject[] numberPrefabs = new GameObject[5];

    [Header("Prefab spawning")]
    [Tooltip("Where to spawn prefabs. If null, this GameObject's position is used.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Optional parent for spawned prefabs.")]
    [SerializeField] private Transform spawnParent;
    [Tooltip("When true, prefer instantiating prefabs if a prefab exists for the generated number. When false, UI image is preferred.")]
    [SerializeField] private bool preferPrefabs = true;
    [Tooltip("If true, prevent spawning a new prefab while a previous spawned instance still exists.")]
    [SerializeField] private bool singleInstanceOnly = false;

    [Header("Display (UI Image)")]
    [Tooltip("Target UI Image used when displaying sprites. If left empty a Canvas + Image will be created automatically.")]
    [SerializeField] private Image targetImage;

    [Header("Audio")]
    [Tooltip("Audio clip to play when the final image is shown.")]
    [SerializeField] private AudioClip finalAudioClip;
    [Tooltip("Optional AudioSource used to play the final sound. If empty one will be created automatically.")]
    [SerializeField] private AudioSource targetAudioSource;
    [SerializeField, Range(0f, 1f), Tooltip("Volume at which final sound is played (PlayOneShot).")] private float audioVolume = 1f;

    [Header("Display Timing")]
    [SerializeField, Tooltip("Seconds the final generated image remains visible before hiding. Set <= 0 to keep it visible indefinitely.")]
    private float displayDuration = 2f;

    [Header("Auto-hide toggles")]
    [Tooltip("When true the UI image will automatically hide after Display Duration (if > 0).")]
    [SerializeField] private bool autoHideDisplay = true;

    [Header("Generation sequence")]
    [SerializeField, Tooltip("Enable the visual 'generating' animation (flashing). If disabled the final number is chosen immediately.")]
    private bool enableVisualGeneration = true;
    [SerializeField, Tooltip("Seconds the 'generating' animation runs before the final number is chosen.")]
    private float generationDuration = 1.5f;
    [SerializeField, Tooltip("Flash interval at the start (seconds). Smaller = faster flashes.")]
    private float initialFlashInterval = 0.03f;
    [SerializeField, Tooltip("Flash interval at the end (seconds). Larger = slower flashes).")]
    private float finalFlashInterval = 0.20f;
    [SerializeField, Tooltip("Exponent used to ease the flash interval (higher => stronger slow-down).")]
    private float easeExponent = 2f;

    [Header("Cooldown")]
    [SerializeField, Tooltip("Seconds to wait after generating before another generation is allowed.")]
    private float cooldownSeconds = 1f;

    // Next time generation is allowed
    private float nextAvailableTime = 0f;

    // Last generated value (1..5)
    public int LastValue { get; private set; } = 1;

    // Last sprite for LastValue (may be null)
    public Sprite LastSprite => GetSpriteForNumber(LastValue);

    // Runtime-created Image used when no targetImage is assigned
    private Image runtimeImage;

    // Runtime-created AudioSource used when no targetAudioSource is assigned
    private AudioSource runtimeAudioSource;

    // Sequence state
    private bool isGenerating;

    // Hide coroutine handle (so we can cancel/reschedule hides)
    private Coroutine hideCoroutine;

    // Keep a reference to the last spawned prefab instance (for single-instance enforcement)
    private GameObject lastSpawnedInstance;

    // Remaining cooldown in seconds (read-only)
    public float CooldownRemaining => Mathf.Max(0f, nextAvailableTime - Time.time);
    public bool IsOnCooldown => Time.time < nextAvailableTime;
    public bool IsGenerating => isGenerating;

    // Editor-time validation to help catch misconfiguration
    void OnValidate()
    {
        if (numberSprites == null || numberSprites.Length < 5)
            numberSprites = new Sprite[5];

        if (numberPrefabs == null || numberPrefabs.Length < 5)
            numberPrefabs = new GameObject[5];

#if UNITY_EDITOR
        if (numberSprites.Length < 5 || numberPrefabs.Length < 5)
            Debug.LogWarning($"RandomNumberGenerator: expected arrays of length >= 5. numberSprites.Length={numberSprites.Length}, numberPrefabs.Length={numberPrefabs.Length}");
#endif
    }

    // Initiates the generation sequence.
    // If on cooldown or a sequence is already running, prints "not so fast".
    public int Generate()
    {
        if (isGenerating || Time.time < nextAvailableTime)
        {
            Debug.Log("not so fast");
            return LastValue;
        }

        // Cancel any pending auto-hide so the image won't disappear mid-sequence
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (enableVisualGeneration && generationDuration > 0f)
            StartCoroutine(GenerationSequence());
        else
            FinalizeGenerationImmediate();

        return LastValue;
    }

    private IEnumerator GenerationSequence()
    {
        isGenerating = true;

        var image = targetImage ?? EnsureRuntimeImage();
        if (image == null && !HasAnyPrefabAssigned())
        {
            Debug.LogWarning("RandomNumberGenerator: no Image available to display generation and no prefabs assigned.");
            isGenerating = false;
            yield break;
        }

        // Cancel any pending hide and ensure visible for the sequence
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (image != null)
            image.enabled = true;

        // build list of valid sprite indices (only non-null)
        var validIndices = new List<int>();
        if (numberSprites != null)
        {
            for (int i = 0; i < numberSprites.Length; i++)
                if (numberSprites[i] != null)
                    validIndices.Add(i);
        }

        float startTime = Time.time;

        // Flash random sprites while timer runs, starting fast and slowing down
        while (Time.time - startTime < Mathf.Max(0f, generationDuration))
        {
            float elapsed = Time.time - startTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, generationDuration));
            float eased = Mathf.Pow(t, Mathf.Max(0.0001f, easeExponent)); // ease-out curve
            float currInterval = Mathf.Lerp(initialFlashInterval, finalFlashInterval, eased);
            currInterval = Mathf.Max(0.001f, currInterval);

            // pick and show a random sprite for the flash (UI only)
            if (image != null)
            {
                if (validIndices.Count > 0)
                {
                    int pick = validIndices[Random.Range(0, validIndices.Count)];
                    var s = numberSprites[pick];
                    image.sprite = s;
                    if (s != null) image.SetNativeSize();
                }
                else if (numberSprites != null && numberSprites.Length > 0)
                {
                    int pick = Random.Range(0, numberSprites.Length);
                    var s = numberSprites[pick];
                    image.sprite = s;
                    if (s != null) image.SetNativeSize();
                }
            }

            yield return new WaitForSeconds(currInterval);
        }

        // After animation finishes pick final number and display its sprite / prefab
        FinalizeGenerationInternal(image);

        // start cooldown after the sequence completes
        nextAvailableTime = Time.time + Mathf.Max(0f, cooldownSeconds);
        isGenerating = false;
    }

    // Immediate finalization (used when visuals disabled)
    private void FinalizeGenerationImmediate()
    {
        // Cancel any pending hide so the new display is scheduled fresh
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        var image = targetImage ?? EnsureRuntimeImage();
        FinalizeGenerationInternal(image);
        nextAvailableTime = Time.time + Mathf.Max(0f, cooldownSeconds);
    }

    private void FinalizeGenerationInternal(Image image)
    {
        LastValue = Random.Range(1, 6); // 1..5 inclusive
        var finalSprite = GetSpriteForNumber(LastValue);
        var prefab = GetPrefabForNumber(LastValue);

        int index = LastValue - 1;
        Debug.Log($"RandomNumberGenerator: Finalized LastValue={LastValue} (index {index}). prefab={(prefab != null ? prefab.name : "null")}, sprite={(finalSprite != null ? finalSprite.name : "null")}");

        // If a prefab exists and prefabs are preferred, instantiate prefab
        if (preferPrefabs && prefab != null)
        {
            SpawnPrefab(prefab, LastValue);
            Debug.Log($"RandomNumberGenerator generated: {LastValue} -> Prefab \"{prefab.name}\" instantiated");
            PlayFinalAudio();
            return;
        }

        // Otherwise, fallback to UI image display (existing behavior)
        if (image == null)
        {
            if (finalSprite != null)
                Debug.Log($"RandomNumberGenerator generated: {LastValue} -> Sprite \"{finalSprite.name}\" (no Image to display)");
            else if (prefab != null)
                Debug.Log($"RandomNumberGenerator generated: {LastValue} -> Prefab \"{prefab.name}\" available but no Image configured.");
            else
                Debug.LogWarning($"RandomNumberGenerator generated: {LastValue} -> No sprite or prefab assigned for this number.");

            // If a prefab exists but preferPrefabs == false, we can still optionally spawn it:
            if (!preferPrefabs && prefab != null)
            {
                SpawnPrefab(prefab, LastValue);
                Debug.Log($"RandomNumberGenerator generated: {LastValue} -> Prefab \"{prefab.name}\" instantiated (preferPrefabs=false)");
                PlayFinalAudio();
            }
            return;
        }

        if (finalSprite != null)
        {
            image.sprite = finalSprite;
            image.enabled = true;
            image.SetNativeSize();
            image.transform.SetAsLastSibling();
            Debug.Log($"RandomNumberGenerator generated: {LastValue} -> Sprite \"{finalSprite.name}\" (UI Image)");

            PlayFinalAudio();

            // schedule auto-hide if requested
            if (autoHideDisplay && displayDuration > 0f)
            {
                // cancel previous hide if any
                if (hideCoroutine != null)
                {
                    StopCoroutine(hideCoroutine);
                    hideCoroutine = null;
                }
                hideCoroutine = StartCoroutine(HideAfterDelay(image, displayDuration));
            }
        }
        else
        {
            // No UI sprite; optionally spawn prefab if exists
            if (prefab != null)
            {
                SpawnPrefab(prefab, LastValue);
                Debug.Log($"RandomNumberGenerator generated: {LastValue} -> Prefab \"{prefab.name}\" instantiated (no UI sprite assigned).");
                PlayFinalAudio();
            }
            else
            {
                Debug.LogWarning($"RandomNumberGenerator generated: {LastValue} -> No sprite or prefab assigned for this number. Assign one in the inspector.");
            }
        }
    }

    private void PlayFinalAudio()
    {
        if (finalAudioClip != null)
        {
            var audio = targetAudioSource ?? EnsureRuntimeAudioSource();
            if (audio != null)
            {
                audio.PlayOneShot(finalAudioClip, Mathf.Clamp01(audioVolume));
            }
            else
            {
                Debug.LogWarning("RandomNumberGenerator: audio clip assigned but no AudioSource available to play it.");
            }
        }
    }

    // now accepts number for clearer naming in hierarchy & debugging
    private void SpawnPrefab(GameObject prefab, int number)
    {
        if (prefab == null) return;

        // If single-instance mode is active, prevent spawning while an instance still exists
        if (singleInstanceOnly && lastSpawnedInstance != null)
        {
            // Unity treats destroyed objects as null; this check blocks only when an instance truly exists.
            Debug.Log($"RandomNumberGenerator: spawn blocked because singleInstanceOnly is enabled and an instance already exists ({lastSpawnedInstance.name}).");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        pos.z = -1f; // ensure placed at z = -1 (2D layer)

        // instantiate using parent-aware overload so transform is correct on creation
        var instance = Instantiate(prefab, pos, Quaternion.identity, spawnParent);

        // force position after instantiation to ensure z remains as requested even if parent/prefab modifies it
        instance.transform.position = pos;

        // give the instance a clear name so you can inspect spawned objects at runtime
        instance.name = $"Number_{number}_{prefab.name}";

        // keep reference for single-instance enforcement
        lastSpawnedInstance = instance;

        // Log the SpriteRenderer sprite (if any) for debugging mapping issues
        var sr = instance.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Debug.Log($"Spawned prefab instance has SpriteRenderer with sprite: {(sr.sprite != null ? sr.sprite.name : "null")}");
        }
        else
        {
            Debug.Log("Spawned prefab instance has no SpriteRenderer in root/children.");
        }

        // NOTE: we intentionally do not auto-destroy spawned prefabs here.
        // If singleInstanceOnly is enabled, the presence of lastSpawnedInstance will block further spawns until
        // that GameObject is destroyed elsewhere (or becomes null).
    }

    private IEnumerator HideAfterDelay(Image image, float seconds)
    {
        if (seconds <= 0f)
        {
            if (image != null) image.enabled = false;
            hideCoroutine = null;
            yield break;
        }

        yield return new WaitForSeconds(seconds);

        if (image != null)
            image.enabled = false;

        hideCoroutine = null;
    }

    // Ensure a runtime UI Image exists (tries to reuse any on this GameObject/children, otherwise creates a Canvas + Image)
    private Image EnsureRuntimeImage()
    {
        if (runtimeImage != null) return runtimeImage;
        if (targetImage != null) { runtimeImage = targetImage; return runtimeImage; }

        runtimeImage = GetComponentInChildren<Image>();
        if (runtimeImage != null) return runtimeImage;

        // Find or create Canvas
        var canvas = FindObjectOfType<Canvas>();
        GameObject canvasGO;
        if (canvas == null)
        {
            canvasGO = new GameObject("GeneratedCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        else
        {
            canvasGO = canvas.gameObject;
        }

        var go = new GameObject("GeneratedImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvasGO.transform, false);
        runtimeImage = go.GetComponent<Image>();

        // center on screen
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;

        // hide by default until first generation
        if (runtimeImage != null)
            runtimeImage.enabled = false;

        return runtimeImage;
    }

    // Helper to create/find a runtime audio source when none was assigned
    private AudioSource EnsureRuntimeAudioSource()
    {
        if (runtimeAudioSource != null) return runtimeAudioSource;
        if (targetAudioSource != null) { runtimeAudioSource = targetAudioSource; return runtimeAudioSource; }

        runtimeAudioSource = GetComponentInChildren<AudioSource>();
        if (runtimeAudioSource != null) return runtimeAudioSource;

        var go = new GameObject("GeneratedAudioSource");
        go.transform.SetParent(transform, false);
        runtimeAudioSource = go.AddComponent<AudioSource>();
        runtimeAudioSource.playOnAwake = false;
        runtimeAudioSource.spatialBlend = 0f; // 2D sound
        return runtimeAudioSource;
    }

    // Returns the sprite assigned to a 1-based number, or null if not assigned / out of range
    public Sprite GetSpriteForNumber(int number)
    {
        int index = number - 1;
        if (index < 0 || numberSprites == null || index >= numberSprites.Length)
            return null;
        return numberSprites[index];
    }

    // Returns the prefab assigned to a 1-based number, or null if not assigned / out of range
    public GameObject GetPrefabForNumber(int number)
    {
        int index = number - 1;
        if (index < 0 || numberPrefabs == null || index >= numberPrefabs.Length)
            return null;
        return numberPrefabs[index];
    }

    private bool HasAnyPrefabAssigned()
    {
        if (numberPrefabs == null) return false;
        foreach (var p in numberPrefabs)
            if (p != null) return true;
        return false;
    }

    // Ensure image is hidden at start
    void Start()
    {
        // Do not auto-generate on start.
        // Hide any assigned image so nothing is visible before the first generation.
        if (targetImage != null)
            targetImage.enabled = false;
        else
        {
            var childImage = GetComponentInChildren<Image>();
            if (childImage != null)
                childImage.enabled = false;
        }
    }

    // Also allow manual triggering by pressing Q (Play mode, Game view focused)
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            Generate();
    }

    void OnDisable()
    {
        // Reset running flag and stop coroutine(s) if object is disabled
        isGenerating = false;
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        StopAllCoroutines();
    }
}
