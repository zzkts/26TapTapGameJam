using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Read-only asset dependency export, explicitly requested by a local marker file.</summary>
[InitializeOnLoad]
public static class AssetCleanupAudit
{
    private static double nextPoll;
    [Serializable] private sealed class Entry { public string path, guid; public string[] dependencies; }
    [Serializable] private sealed class Report
    {
        public string project, scene;
        public bool playing, sceneDirty, compiling;
        public string[] buildScenes;
        public Entry[] assets;
    }
    [Serializable] private sealed class ValidationReport
    {
        public bool success;
        public string[] checkedScenes, errors;
        public int prefabs, renderers;
    }
    static AssetCleanupAudit() { EditorApplication.update += CheckRequest; }
    private static void CheckRequest()
    {
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + .5;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool("AssetCleanup.SmokeFinished", false))
        {
            SessionState.SetBool("AssetCleanup.SmokeFinished", false);
            RunProgress.EndIsolatedValidation();
            SessionState.SetBool("WaterDrainUI.Review", false);
            SessionState.SetBool("GeneSpecQA.Run", false);
        }
        if (File.Exists("Temp/AssetCleanup/audit-request.txt")) Export();
        if (File.Exists("Temp/AssetCleanup/validate-request.txt"))
        { File.Delete("Temp/AssetCleanup/validate-request.txt"); Validate(); }
        if (File.Exists("Temp/AssetCleanup/smoke-request.txt"))
        {
            File.Delete("Temp/AssetCleanup/smoke-request.txt");
            if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) return;
            SessionState.SetString("AssetCleanup.OriginalScene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
            SessionState.SetString("AssetCleanup.OriginalSave", PlayerPrefs.GetString(RunProgress.SaveKey, ""));
            SessionState.SetInt("AssetCleanup.OriginalEndings", PlayerPrefs.GetInt(RunProgress.EndingKey, 0));
            SessionState.SetBool("WaterDrainUI.Review", true);
            SessionState.SetBool("GeneSpecQA.Run", false);
            SessionState.SetBool("AssetCleanup.Smoke", true);
            EditorApplication.isPlaying = true;
        }
    }
    [MenuItem("Tools/TapGJ/Project/Export Asset Dependency Audit")]
    public static void Export()
    {
        Directory.CreateDirectory("Temp/AssetCleanup");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p)).OrderBy(p => p).ToArray();
        var report = new Report {
            project = Application.dataPath, scene = scene.path,
            playing = EditorApplication.isPlaying, sceneDirty = scene.isDirty,
            compiling = EditorApplication.isCompiling,
            buildScenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            assets = paths.Select(p => new Entry { path = p, guid = AssetDatabase.AssetPathToGUID(p), dependencies = AssetDatabase.GetDependencies(p, false) }).ToArray()
        };
        File.WriteAllText("Temp/AssetCleanup/audit.json", JsonUtility.ToJson(report, true));
        if (File.Exists("Temp/AssetCleanup/audit-request.txt")) File.Delete("Temp/AssetCleanup/audit-request.txt");
        Debug.Log("Asset cleanup dependency audit exported: " + paths.Length + " assets; no scene changes.");
    }

    [MenuItem("Tools/TapGJ/Project/Validate Retained Scenes And Prefabs")]
    public static void Validate()
    {
        var errors = new List<string>();
        var report = new ValidationReport();
        report.checkedScenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        try
        {
            if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Validation requires stopped Play Mode and a clean current scene.");
            foreach (string path in report.checkedScenes)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (var root in scene.GetRootGameObjects()) Inspect(root, path, errors, report);
                    foreach (string dep in AssetDatabase.GetDependencies(path, true))
                        if (dep.StartsWith("Assets/") && !File.Exists(dep)) errors.Add(path + ": missing dependency " + dep);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) errors.Add("Cannot load prefab: " + path);
                else { report.prefabs++; Inspect(prefab, path, errors, report); }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { "Assets/ArtRes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader != null && ShaderUtil.ShaderHasError(shader)) errors.Add("Shader compile error: " + path);
            }
        }
        catch (Exception e) { errors.Add(e.ToString()); }
        report.errors = errors.ToArray(); report.success = errors.Count == 0;
        Directory.CreateDirectory("Temp/AssetCleanup");
        File.WriteAllText("Temp/AssetCleanup/validation.json", JsonUtility.ToJson(report, true));
        Debug.Log("Asset cleanup scene validation: " + (report.success ? "PASS" : "FAIL") + "; " + report.prefabs + " prefabs; " + errors.Count + " errors.");
    }
    private static void Inspect(GameObject root, string path, List<string> errors, ValidationReport report)
    {
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) > 0)
                errors.Add(path + ": missing script on " + node.name);
            foreach (var component in node.GetComponents<Component>())
            {
                if (component == null) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                        errors.Add(path + ": broken reference " + node.name + "." + property.propertyPath);
            }
        }
        foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            report.renderers++;
            if (sprite.enabled && sprite.sprite == null) errors.Add(path + ": missing sprite on " + sprite.name);
        }
    }
}
