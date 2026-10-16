using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>Targeted scene polish after removing the optional ecology prototype.</summary>
public static class GeneSpecSceneSetup
{
    [MenuItem("Tools/TapGJ/Apply Gene Document Route")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var scene=EditorSceneManager.OpenScene("Assets/Scene/GameScene.unity");
        var flow=Object.FindFirstObjectByType<CampaignFlow>();
        if(flow==null)throw new InvalidOperationException("The existing continuous world is required.");
        flow.level.guardianEntryX=112;
        flow.level.barriers=Array.Empty<GameObject>();
        // Put the first guardian ahead of the safe preparation terminal, not behind the player.
        var first=flow.level.guardians.FirstOrDefault(g=>g!=null&&g.name.Contains("菌壳"));
        if(first!=null)first.transform.position=new Vector3(116,.5f,0);
        foreach(var chest in Object.FindObjectsByType<GeneChest>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            var at=chest.transform.localPosition;
            if(Mathf.Abs(at.x-19)<.1f)at.x=5;
            at.y=0;chest.transform.localPosition=at;
        }
        foreach(var station in Object.FindObjectsByType<GeneEditStation>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(station.transform.position.x>96)station.transform.position=new Vector3(100,0,0);
        foreach(var sign in Object.FindObjectsByType<WorldSign>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(sign.name.StartsWith("基因路线 · "))Object.DestroyImmediate(sign.gameObject);
        Sign(flow.transform,"雨后泵站 · 打怪 / 开冷库获取基因",new Vector2(3,3.5f));
        Sign(flow.transform,"共生菌林 · 终端重组基因，继续探索",new Vector2(51,3.5f));
        Sign(flow.transform,"净化核心 · 击败三名守卫",new Vector2(99,3.5f));
        Sign(flow.transform,"终点回流阀 · 守卫清除后按 E",new Vector2(138,3.5f));
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene("Assets/Scene/BeginScene.unity");
        CompleteGameMenuBuilder.Apply();
        EditorSceneManager.OpenScene("Assets/Scene/GameScene.unity");
        AssetDatabase.SaveAssets();
    }
    private static void Sign(Transform parent,string text,Vector2 at)
    {
        var go=new GameObject("基因路线 · "+text,typeof(RectTransform),typeof(Canvas));
        go.transform.SetParent(parent);go.transform.position=at;
        PerformancePolishSetup.StyleSign(go.GetComponent<Canvas>(),text);
    }

    // This entry point is run only in the disposable QA project, never against the user's open editor.
    public static void RunIsolatedPlayChecks()
    {
        if(!Path.GetFullPath(Application.dataPath).Contains("GeneSpecQA"))throw new InvalidOperationException("Use the isolated QA project.");
        Apply();
        Directory.CreateDirectory("GeneSpecResults");
        if(File.Exists("GeneSpecResults/play-result.json"))File.Delete("GeneSpecResults/play-result.json");
        File.WriteAllText("GeneSpecResults/editor-ready.txt",DateTime.UtcNow.ToString("O"));
        SessionState.SetBool("GeneSpecQA.Run",true);
        EditorApplication.EnterPlaymode();
    }
}

[InitializeOnLoad]
public static class GeneSpecBatchCompletion
{
    static GeneSpecBatchCompletion(){EditorApplication.update+=Poll;}
    private static void Poll()
    {
        if(!Application.isBatchMode||!SessionState.GetBool("GeneSpecQA.Run",false))return;
        const string result="GeneSpecResults/play-result.json";
        if(!File.Exists(result))return;
        if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        SessionState.SetBool("GeneSpecQA.Run",false);
        EditorApplication.Exit(File.ReadAllText(result).Contains("\"success\": true")?0:1);
    }
}
