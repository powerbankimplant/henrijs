using System.Collections.Generic;
using UnityEngine;

public class highlights : MonoBehaviour
{
    [Tooltip("Which layers should be considered for hover detection.")]
    public LayerMask hoverMask = ~0;

    [Tooltip("Color used for the outline copies.")]
    public Color outlineColor = new Color(0f, 0f, 0.9f);

    [Tooltip("Distance (in world units) from the sprite center for outline copies.")]
    public float outlineThickness = 0.02f;

    [Tooltip("Number of outline copies placed around the sprite. Higher = rounder outline, costlier.")]
    [Range(4, 24)]
    public int outlineSamples = 8;

    [Tooltip("Whether to outline all child SpriteRenderers of the hit object. If false, only the nearest parent renderer is outlined.")]
    public bool outlineChildren = false;

    [Tooltip("Optional camera to use. If null, Camera.main will be used.")]
    public Camera sourceCamera;

    [Tooltip("How many sample points along the camera ray will be checked for 2D colliders. Increase for deep scenes.")]
    [Range(3, 64)]
    public int depthSamples = 12;

    [Tooltip("Enable debug logging to see why outlines may not be created.")]
    public bool debug = false;

    // runtime state
    private Transform currentRoot;
    private Dictionary<SpriteRenderer, List<GameObject>> createdOutlines = new Dictionary<SpriteRenderer, List<GameObject>>();

    void Awake()
    {
        if (sourceCamera == null) sourceCamera = Camera.main;
    }

    void Update()
    {
        if (sourceCamera == null) return;

        Vector3 mousePos = Input.mousePosition;

        // Build a set of Collider2D found by sampling points along the camera ray.
        var found = new HashSet<Collider2D>();
        var ray = sourceCamera.ScreenPointToRay(mousePos);

        float near = sourceCamera.nearClipPlane;
        float far = sourceCamera.farClipPlane;
        // If far is not set / extremely large, clamp to a reasonable value
        if (float.IsInfinity(far) || far <= near) far = near + 1000f;

        // Sample along the ray (works for orthographic as well — samples collapse to same point)
        for (int i = 0; i < depthSamples; i++)
        {
            float t = (depthSamples == 1) ? 0f : (float)i / (depthSamples - 1);
            float dist = Mathf.Lerp(near, far, t);
            Vector3 worldPoint3 = ray.GetPoint(dist);
            Vector2 worldPoint2 = new Vector2(worldPoint3.x, worldPoint3.y);

            var hits = Physics2D.OverlapPointAll(worldPoint2, hoverMask);
            if (hits != null && hits.Length > 0)
            {
                for (int h = 0; h < hits.Length; h++)
                {
                    var c = hits[h];
                    if (c != null)
                        found.Add(c);
                }
            }
        }

        if (found.Count == 0)
        {
            if (debug) Debug.Log("highlights: no collider under mouse after sampling depth (check LayerMask and Collider2D presence).");
            ClearCurrent();
            return;
        }

        // Choose best candidate from the union of found colliders (prefer SpriteRenderer sortingOrder, then Z)
        Collider2D bestHit = null;
        SpriteRenderer bestRenderer = null;
        int bestOrder = int.MinValue;
        float bestZ = float.MinValue;

        foreach (var c in found)
        {
            if (c == null) continue;
            var sr = c.GetComponentInParent<SpriteRenderer>();
            if (sr != null)
            {
                int order = sr.sortingOrder;
                float z = sr.transform.position.z;
                if (bestRenderer == null || order > bestOrder || (order == bestOrder && z > bestZ))
                {
                    bestRenderer = sr;
                    bestOrder = order;
                    bestZ = z;
                    bestHit = c;
                }
            }
            else if (bestHit == null)
            {
                bestHit = c;
            }
        }

        if (bestHit != null)
        {
            if (bestRenderer != null)
                HandleHoverEnter(bestRenderer.gameObject);
            else
                HandleHoverEnter(bestHit.gameObject);
        }
        else
        {
            ClearCurrent();
        }
    }

    private void HandleHoverEnter(GameObject hitObject)
    {
        if (hitObject == null)
        {
            ClearCurrent();
            return;
        }

        // decide a sensible root for outlines
        Transform root = hitObject.transform;
        var parentRenderer = hitObject.GetComponentInParent<SpriteRenderer>();
        if (parentRenderer != null)
            root = parentRenderer.transform;

        if (currentRoot == root)
            return; // already highlighted

        ClearCurrent();
        currentRoot = root;

        if (outlineChildren)
        {
            var rlist = root.GetComponentsInChildren<SpriteRenderer>(includeInactive: false);
            foreach (var r in rlist)
                CreateOutlineForRenderer(r);
        }
        else
        {
            SpriteRenderer sr = hitObject.GetComponentInParent<SpriteRenderer>();
            if (sr == null)
                sr = hitObject.GetComponent<SpriteRenderer>();
            if (sr != null)
                CreateOutlineForRenderer(sr);
            else
            {
                // last resort: try to find child sprite
                var srChild = hitObject.GetComponentInChildren<SpriteRenderer>();
                if (srChild != null)
                    CreateOutlineForRenderer(srChild);
                else if (debug)
                    Debug.Log($"highlights: Hit object '{hitObject.name}' has no SpriteRenderer to outline.");
            }
        }
    }

    private void CreateOutlineForRenderer(SpriteRenderer sr)
    {
        if (sr == null) return;

        if (createdOutlines.ContainsKey(sr)) return; // already created

        if (sr.sprite == null)
        {
            if (debug) Debug.LogWarning($"highlights: SpriteRenderer '{sr.name}' has no sprite assigned — skipping outline.");
            return;
        }

        // Ensure thickness is visible relative to world scale
        float thickness = Mathf.Max(0.0005f, outlineThickness);

        var list = new List<GameObject>();

        // Create a parent container as a sibling of the sprite so transforms don't collapse offsets
        var parentName = $"HoverOutline_for_{sr.gameObject.name}";
        var parentGO = new GameObject(parentName);

        // Parent the container to the same parent as the original sprite so localScale/pivot are consistent.
        var originalParent = sr.transform.parent;
        parentGO.transform.SetParent(originalParent, false);

        // position the container at the same local position as the sprite
        parentGO.transform.localPosition = sr.transform.localPosition;
        parentGO.transform.localRotation = sr.transform.localRotation;
        parentGO.transform.localScale = Vector3.one;

        // Use world-space offsets around the sprite center so outline is visible even when the sprite has non-uniform local scale.
        float twoPI = Mathf.PI * 2f;
        for (int i = 0; i < outlineSamples; i++)
        {
            float angle = twoPI * i / outlineSamples;
            Vector3 offsetWorld = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * thickness;

            var go = new GameObject("OutlineCopy");
            // parent to container so they move with the sprite's parent
            go.transform.SetParent(parentGO.transform, false);

            // compute local offset relative to sprite's localScale by converting offsetWorld into parent's local space
            Vector3 worldPos = sr.transform.TransformPoint(Vector3.zero) + offsetWorld;
            // set world position directly to ensure correct placement
            go.transform.position = worldPos;
            go.transform.rotation = sr.transform.rotation;

            // match scale so the copy overlaps pixel-perfect
            go.transform.localScale = sr.transform.localScale;

            var copy = go.AddComponent<SpriteRenderer>();
            copy.sprite = sr.sprite;
            copy.drawMode = sr.drawMode;
            copy.size = sr.size;
            copy.flipX = sr.flipX;
            copy.flipY = sr.flipY;
            copy.sharedMaterial = sr.sharedMaterial; // share material
            copy.maskInteraction = sr.maskInteraction;
            copy.color = outlineColor;

            // ensure outline is rendered behind the original: same sorting layer, slightly lower order
            copy.sortingLayerID = sr.sortingLayerID;
            copy.sortingOrder = sr.sortingOrder - 1;
            copy.sortingLayerName = sr.sortingLayerName;

            list.Add(go);
        }

        createdOutlines[sr] = list;

        if (debug) Debug.Log($"highlights: Created {list.Count} outline copies for '{sr.name}' (thickness={thickness}).");
    }

    private void ClearCurrent()
    {
        // destroy all created outline GameObjects and their containers
        foreach (var kv in createdOutlines)
        {
            var list = kv.Value;
            if (list == null) continue;
            foreach (var go in list)
            {
                if (go != null)
                    Destroy(go);
            }
        }

        // destroy parent containers (they are parents of created copies)
        foreach (var kv in createdOutlines)
        {
            var list = kv.Value;
            if (list == null || list.Count == 0) continue;
            var any = list[0];
            if (any != null && any.transform.parent != null)
            {
                var parent = any.transform.parent.gameObject;
                Destroy(parent);
            }
        }

        createdOutlines.Clear();
        currentRoot = null;
    }

    void OnDisable()
    {
        ClearCurrent();
    }
}
