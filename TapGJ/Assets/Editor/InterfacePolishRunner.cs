using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Explicit local requests for the interface migration and isolated visual verification.</summary>
[InitializeOnLoad]
public static class InterfacePolishRunner
{
    private static double nextPoll;
    [Serializable] private sealed class Report { public bool success; public string message; public string[] scenes; }
    static InterfacePolishRunner() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if(EditorApplication.timeSinceStartup<nextPoll||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        nextPoll=EditorApplication.timeSinceStartup+.5;
        if(SessionState.GetBool("UIAlign.Finished",false))
        {
            if(EditorApplication.isPlaying) { EditorApplication.ExitPlaymode(); return; }
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            SessionState.SetBool("UIAlign.Finished",false);RunProgress.EndIsolatedValidation();
            SessionState.SetBool("WaterDrainUI.Review",false);SessionState.SetBool("UIAlign.Run",false);
            File.WriteAllText("Temp/UIAlign/stopped.json","{\"playing\":false,\"isolated\":false}");
        }
        if(File.Exists("Temp/UIAlign/apply-request.txt"))
        {
            File.Delete("Temp/UIAlign/apply-request.txt");
            try { ApplyBoth(); Write(true,"Both scene interfaces saved."); }
            catch(Exception e) { Write(false,e.ToString()); }
        }
        if(File.Exists("Temp/UIAlign/test-request.txt"))
        {
            File.Delete("Temp/UIAlign/test-request.txt");
            if(EditorApplication.isPlaying||EditorSceneManager.GetSceneManagerSetup().Any(s=>UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty))
            {Write(false,"Stop Play Mode and save current edits before UI validation.");return;}
            SessionState.SetString("UIAlign.Save",PlayerPrefs.GetString(RunProgress.SaveKey,""));
            SessionState.SetInt("UIAlign.Endings",PlayerPrefs.GetInt(RunProgress.EndingKey,0));
            SessionState.SetFloat("UIAlign.Music",PlayerPrefs.GetFloat("Audio.BackgroundVolume",1));
            SessionState.SetFloat("UIAlign.Effects",PlayerPrefs.GetFloat("Audio.GameVolume",1));
            SessionState.SetBool("WaterDrainUI.Review",true);SessionState.SetBool("GeneSpecQA.Run",false);
            SessionState.SetBool("UIAlign.Run",true);EditorApplication.EnterPlaymode();
        }
    }
    [MenuItem("Tools/TapGJ/UI/Align Both Scene Interfaces")]
    public static void ApplyBoth()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        var original=EditorSceneManager.GetSceneManagerSetup();
        foreach(var s in original)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty)throw new InvalidOperationException("Unsaved scene edits: "+s.path);
        try
        {
            foreach(string path in new[]{"Assets/Scene/BeginScene.unity","Assets/Scene/GameScene.unity"})
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                string before=WorldSignature(scene);
                WaterDrainUISetup.ApplyInterfacePass();
                if(before!=WorldSignature(scene))throw new InvalidOperationException("Non-UI world changed: "+path);
            }
        }
        finally {EditorSceneManager.RestoreSceneManagerSetup(original);}
    }
    private static string WorldSignature(UnityEngine.SceneManagement.Scene scene)
    {
        return string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .Where(t=>t.GetComponentInParent<Canvas>()==null)
            .Select(t=>t.name+"|"+t.position.ToString("F5")+"|"+t.rotation.ToString("F5")+"|"+t.lossyScale.ToString("F5")+"|"+t.GetComponents<Collider2D>().Length)
            .OrderBy(s=>s));
    }
    private static void Write(bool success,string message)
    {
        Directory.CreateDirectory("Temp/UIAlign");
        File.WriteAllText("Temp/UIAlign/migration.json",JsonUtility.ToJson(new Report{success=success,message=message,
            scenes=new[]{"Assets/Scene/BeginScene.unity","Assets/Scene/GameScene.unity"}},true));
    }
}
