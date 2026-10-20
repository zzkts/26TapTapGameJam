using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>Explicitly runs existing regression fixtures and keeps generated evidence in Temp.</summary>
[InitializeOnLoad]
public sealed class ReleaseRegressionRunner : ICallbacks
{
    private const string Folder="Temp/ReleasePerformance/";
    private static TestRunnerApi api;
    private static double nextPoll;
    private readonly List<Case> cases=new();
    private string save;private int endings;
    [Serializable] private sealed class Case {public string name,state,message;}
    [Serializable] private sealed class Report {public bool success,saveUnchanged;public int passed,failed,skipped;public Case[] cases;}
    static ReleaseRegressionRunner(){EditorApplication.update+=Poll;}
    private static void Poll()
    {
        if(EditorApplication.timeSinceStartup<nextPoll||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        nextPoll=EditorApplication.timeSinceStartup+.5;
        if(api!=null||!File.Exists(Folder+"edit-tests-request.txt"))return;
        File.Delete(Folder+"edit-tests-request.txt");
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
        {File.WriteAllText(Folder+"edit-tests-error.txt","Stop Play Mode and save current edits first.");return;}
        var callbacks=new ReleaseRegressionRunner{save=PlayerPrefs.GetString(RunProgress.SaveKey,""),endings=PlayerPrefs.GetInt(RunProgress.EndingKey,0)};
        api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(callbacks);
        api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,
            groupNames=new[]{"^(BattleSystemProjectileTests|BuffSystemTests|ElementProjectileTests|WaterFootprintTests|SplitGeneLaunchTests|EnemyCombatPolishTests|PlayerHealthTests|PlayerAnimationPolishTests|GameplayPolishAssetTests|SceneMusicAssetTests|WaterAudioAssetTests|PerformanceLayoutTests|RunProgressTests)(\\.|$)"}}));
    }
    public void RunStarted(ITestAdaptor test){}
    public void TestStarted(ITestAdaptor test){}
    public void TestFinished(ITestResultAdaptor result)
    {if(!result.Test.IsSuite)cases.Add(new Case{name=result.FullName,state=result.ResultState,message=result.Message});}
    public void RunFinished(ITestResultAdaptor result)
    {
        bool unchanged=PlayerPrefs.GetString(RunProgress.SaveKey,"")==save&&PlayerPrefs.GetInt(RunProgress.EndingKey,0)==endings;
        File.WriteAllText(Folder+"edit-tests.json",JsonUtility.ToJson(new Report{success=result.FailCount==0&&result.PassCount>0&&unchanged,
            saveUnchanged=unchanged,passed=result.PassCount,failed=result.FailCount,skipped=result.SkipCount,cases=cases.ToArray()},true));
        var completed=api;api=null;if(completed!=null)UnityEngine.Object.DestroyImmediate(completed);
    }
}
