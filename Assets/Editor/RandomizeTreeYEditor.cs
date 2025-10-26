using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class RandomizeTreeYEditor
{
    const string PrefabAssetName = "tree"; // target prefab asset name (case-insensitive)

    [MenuItem("Tools/Randomize Trees/Randomize Y Rotations (Prefab name 'tree')")]
    public static void RandomizeAllPrefabNamedTrees()
    {
        var matches = FindPrefabInstancesByAssetName(PrefabAssetName);
        if (matches.Count == 0)
        {
            EditorUtility.DisplayDialog("Randomize Trees", $"No prefab instances with asset name '{PrefabAssetName}' found in the active scene.", "OK");
            return;
        }

        Undo.RecordObjects(GetTransforms(matches), "Randomize tree rotations");

        foreach (var go in matches)
        {
            var rot = go.transform.eulerAngles;
            rot.y = UnityEngine.Random.Range(0f, 360f);
            go.transform.eulerAngles = rot;
            EditorUtility.SetDirty(go);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Randomize Trees", $"Randomized {matches.Count} prefab instances named '{PrefabAssetName}'.", "OK");
    }

    static List<GameObject> FindPrefabInstancesByAssetName(string assetName)
    {
        var results = new List<GameObject>();
        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded) return results;

        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
            CollectMatchesRecursive(root.transform, assetName, results);

        return results;
    }

    static void CollectMatchesRecursive(Transform t, string assetName, List<GameObject> outList)
    {
        // Get the corresponding prefab asset (if any) for this GameObject instance
        var asset = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
        if (asset != null && string.Equals(asset.name, assetName, StringComparison.OrdinalIgnoreCase))
        {
            outList.Add(t.gameObject);
        }

        // Recurse children
        for (int i = 0; i < t.childCount; i++)
            CollectMatchesRecursive(t.GetChild(i), assetName, outList);
    }

    static UnityEngine.Object[] GetTransforms(List<GameObject> gos)
    {
        var arr = new UnityEngine.Object[gos.Count];
        for (int i = 0; i < gos.Count; i++) arr[i] = gos[i].transform;
        return arr;
    }
}