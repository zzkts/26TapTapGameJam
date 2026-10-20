using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>Opt-in import/build evidence. Does not build a player or change scenes.</summary>
[InitializeOnLoad]
public static class ReleasePerformanceAudit
{
    private const string Folder = "Temp/ReleasePerformance/";
    private static double nextPoll;
    [Serializable] private sealed class TextureEntry
    {
        public string path, format;
        public int width, height, maxSize;
        public bool readable, mipmaps;
        public long bytes;
    }
    [Serializable] private sealed class PackedEntry { public string path; public ulong bytes; }
    [Serializable] private sealed class Report
    {
        public string project, scene, utc, buildPath, buildOptions, error, compressionOverride;
        public int textureSizeOverride;
        public bool playing, sceneDirty, compiling;
        public ulong lastBuildBytes;
        public TextureEntry[] loadedTextures;
        public PackedEntry[] largestBuildAssets;
        public string[] buildDependencies;
    }
    static ReleasePerformanceAudit() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if(EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextPoll = EditorApplication.timeSinceStartup + .5;
        if(SessionState.GetBool("ReleasePerf.Finished",false))
        {
            if(EditorApplication.isPlaying) { EditorApplication.isPlaying=false; return; }
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool("ReleasePerf.Finished",false); RunProgress.EndIsolatedValidation();
            File.WriteAllText(Folder+"stopped.json","{\"playing\":false,\"isolated\":false}");
        }
        if(File.Exists(Folder+"run-request.txt"))
        {
            string runLabel=File.ReadAllText(Folder+"run-request.txt").Trim(); File.Delete(Folder+"run-request.txt");
            if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty ||
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="BeginScene")
            {File.WriteAllText(Folder+"run-error.txt","Open clean BeginScene in stopped editor first.");return;}
            SessionState.SetString("ReleasePerf.Label",runLabel=="after"?"after":"before");
            SessionState.SetString("ReleasePerf.Save",PlayerPrefs.GetString(RunProgress.SaveKey,""));
            SessionState.SetInt("ReleasePerf.Endings",PlayerPrefs.GetInt(RunProgress.EndingKey,0));
            SessionState.SetBool("ReleasePerf.Run",true); EditorApplication.EnterPlaymode(); return;
        }
        if(!File.Exists(Folder + "audit-request.txt")) return;
        string label = File.ReadAllText(Folder + "audit-request.txt").Trim();
        File.Delete(Folder + "audit-request.txt");
        Export(label == "after" ? "after" : "before");
    }
    public static void Export(string label)
    {
        Directory.CreateDirectory(Folder);
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var report = new Report {project = Application.dataPath, scene = scene.path, utc = DateTime.UtcNow.ToString("O"),
            playing = EditorApplication.isPlaying, sceneDirty = scene.isDirty, compiling = EditorApplication.isCompiling,
            compressionOverride=EditorUserBuildSettings.overrideTextureCompression.ToString(),
            textureSizeOverride=EditorUserBuildSettings.overrideMaxTextureSize};
        try
        {
            var roots = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path)
                .Concat(AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && p.Contains("/Resources/") && !AssetDatabase.IsValidFolder(p))).ToArray();
            report.buildDependencies = AssetDatabase.GetDependencies(roots, true);
            report.loadedTextures = Resources.FindObjectsOfTypeAll<Texture2D>()
                .Select(t => new {texture = t, path = AssetDatabase.GetAssetPath(t)})
                .Where(t => t.path.StartsWith("Assets/")).Select(t =>
                {
                    var importer = AssetImporter.GetAtPath(t.path) as TextureImporter;
                    return new TextureEntry {path=t.path, width=t.texture.width, height=t.texture.height,
                        bytes=Profiler.GetRuntimeMemorySizeLong(t.texture), format=t.texture.format.ToString(),
                        maxSize=importer != null ? importer.maxTextureSize : 0, readable=t.texture.isReadable,
                        mipmaps=t.texture.mipmapCount > 1};
                }).OrderByDescending(t => t.bytes).ToArray();
            var build = BuildReport.GetLatestReport();
            if(build != null)
            {
                report.lastBuildBytes=build.summary.totalSize; report.buildPath=build.summary.outputPath;
                report.buildOptions=build.summary.options.ToString();
                report.largestBuildAssets=build.packedAssets.SelectMany(p => p.contents)
                    .GroupBy(p => p.sourceAssetPath).Select(g => new PackedEntry {path=g.Key,bytes=g.Aggregate(0UL,(n,p)=>n+p.packedSize)})
                    .OrderByDescending(p => p.bytes).Take(40).ToArray();
            }
        }
        catch(Exception e) { report.error=e.ToString(); }
        File.WriteAllText(Folder + label + "-imports.json", JsonUtility.ToJson(report,true));
    }
}
