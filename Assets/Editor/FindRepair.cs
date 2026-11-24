using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Survival > Repair Find Objects (missing scripts).
/// The scripts RandomFind and FindSpot used to live in the wrong file (FindsScatter.cs), so Unity forgot them whenever a scene
/// was saved ("The referenced script (Unknown) on this Behaviour is missing!"). They have their own files now. This puts them
/// back on the objects of the open scene and on the prefabs in Assets/Prefabs/Objects. Safe to run again.
/// </summary>
public static class FindRepair
{
    static readonly HashSet<string> GroundFinds = new HashSet<string>
    {
        "Odd shoe", "Guidebook", "Selfie stick", "Power bank", "Beach ball", "Crashed drone",
        "Foil blanket", "Whistle", "Sealed water", "Chocolate", "Wool hat", "Red berries",
    };
    static readonly HashSet<string> TreeFinds = new HashSet<string> { "Sweater in a tree", "Socks in a tree", "Gloves in a tree" };

    [MenuItem("Survival/Repair Find Objects (missing scripts)")]
    public static void Run()
    {
        int prefabs = 0, objects = 0;

        // 1) the prefabs
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Objects" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(contents) == 0 && !NeedsFind(contents)) continue;
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(contents);
                Ensure(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                prefabs++;
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        // 2) the objects of the open scene (not the ones that come from a prefab: they follow the prefab)
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go.scene.IsValid() || EditorUtility.IsPersistent(go)) continue;
            if (PrefabUtility.IsPartOfPrefabInstance(go)) continue;
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) == 0 && !NeedsFind(go)) continue;
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            Ensure(go);
            objects++;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"Find objects repaired: {prefabs} prefabs, {objects} scene objects. Save the scene (Cmd+S).");
    }

    static bool NeedsFind(GameObject go)
    {
        if (go.name == "Find spot (ground)" || go.name == "Find spot (tree)") return go.GetComponent<FindSpot>() == null;
        if (go.GetComponent<Interactable>() == null) return false;
        if (GroundFinds.Contains(go.name) || TreeFinds.Contains(go.name)) return go.GetComponent<RandomFind>() == null;
        return false;
    }

    static void Ensure(GameObject go)
    {
        string n = go.name;
        if (n == "Find spot (ground)" || n == "Find spot (tree)")
        {
            var spot = go.GetComponent<FindSpot>() ?? go.AddComponent<FindSpot>();
            spot.inTree = n.Contains("(tree)");
        }
        else if (go.GetComponent<Interactable>() != null && GroundFinds.Contains(n))
        {
            var find = go.GetComponent<RandomFind>() ?? go.AddComponent<RandomFind>();
            find.inTree = false;
        }
        else if (go.GetComponent<Interactable>() != null && TreeFinds.Contains(n))
        {
            var find = go.GetComponent<RandomFind>() ?? go.AddComponent<RandomFind>();
            find.inTree = true;
            var cloth = GameObject.Find(n + " (cloth)");     // the little coloured scrap that hangs in the tree
            if (cloth != null)
            {
                find.attached = cloth.transform;
                find.attachedOffset = new Vector3(0.3f, 1.1f, 0f);
            }
        }
        EditorUtility.SetDirty(go);
    }
}
