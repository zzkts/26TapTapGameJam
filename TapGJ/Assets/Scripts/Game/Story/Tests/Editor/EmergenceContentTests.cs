using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class EmergenceContentTests
{
    [TestCase("GameScene",1)]
    [TestCase("SporeScene",2)]
    [TestCase("CoreScene",3)]
    public void CampaignChaptersHaveReachableNonBlockingExitsAndBranchGoals(string name,int chapter)
    {
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scene/"+name+".unity");
        try
        {
            var roots=scene.GetRootGameObjects();
            CompleteLevelFlow level=roots.SelectMany(r=>r.GetComponentsInChildren<CompleteLevelFlow>(true)).Single();
            var campaign=level.GetComponent<CampaignFlow>();Assert.That(campaign,Is.Not.Null);Assert.That(campaign.chapter,Is.EqualTo(chapter));
            Assert.That(campaign.reactors.Length,Is.EqualTo(3));Assert.That(campaign.reactors.All(n=>!string.IsNullOrEmpty(n.hint)&&n.beacon!=null),Is.True);
            Assert.That(campaign.exit.GetComponentsInChildren<Collider2D>().All(c=>c.isTrigger),Is.True,"Chapter exits must never be solid progression blockers");
            Assert.That(level.GetComponentsInChildren<EmergenceGate>().Length,Is.Zero);
            Assert.That(campaign.reactors.Max(n=>n.transform.position.y),Is.GreaterThanOrEqualTo(6));
            Assert.That(campaign.reactors.Min(n=>n.transform.position.x),Is.LessThan(8));
            Assert.That(campaign.reactors.Max(n=>n.transform.position.x),Is.GreaterThan(29));
            Assert.That(campaign.exit.transform.position.x,Is.InRange(17f,23f),"The exit is a central return point");
            Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<GeneChest>(true)).Count(c=>c.offerChoice),Is.EqualTo(2));
            if(chapter==3)Assert.That(level.guardians.Length,Is.EqualTo(3));
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
    [Test]
    public void BuildHintsDescribeActualEquippedCombinations()
    {
        Modifier fire=Resources.Load<Modifier>("SO/Genes/ember"),poison=Resources.Load<Modifier>("SO/Genes/sewage"),water=Resources.Load<Modifier>("SO/Genes/water");
        Assert.That(GeneBuildAnalysis.Describe(new[]{fire,poison}),Does.Contain("连锁"));
        Assert.That(GeneBuildAnalysis.Describe(new[]{fire,water}),Does.Contain("蒸汽"));
        Assert.That(GeneBuildAnalysis.Describe(new Modifier[0]),Does.Contain("箱子"));
    }
    [Test]
    public void OriginalCheckpointMovesWithTheExpandedCoreAndKeepsItsGenes()
    {
        bool had=PlayerPrefs.HasKey(RunProgress.SaveKey);string original=PlayerPrefs.GetString(RunProgress.SaveKey);
        try
        {
            PlayerPrefs.SetString(RunProgress.SaveKey,"{\"version\":1,\"scene\":\"GameScene\",\"checkpointX\":78,\"checkpointY\":1,\"genes\":[{\"geneId\":8,\"definitionName\":\"water\",\"instanceId\":\"keep-me\",\"level\":2,\"copies\":3}]}");
            RunSaveData data=RunProgress.Read();Assert.That(data.version,Is.EqualTo(2));Assert.That(data.checkpointX,Is.EqualTo(228));
            Assert.That(data.genes[0].instanceId,Is.EqualTo("keep-me"));
            RunProgress.Write(data);Assert.That(RunProgress.Read().checkpointX,Is.EqualTo(228),"Migration must not run twice");
        }
        finally{if(had)PlayerPrefs.SetString(RunProgress.SaveKey,original);else PlayerPrefs.DeleteKey(RunProgress.SaveKey);}
    }
    [Test]
    public void TrialSettingsSurviveSerialization()
    {
        bool had=PlayerPrefs.HasKey(RunProgress.SaveKey);string original=PlayerPrefs.GetString(RunProgress.SaveKey);
        try
        {
            RunProgress.Write(new RunSaveData{experimentMode=true,runSeed=7342,checkpointX=139});
            RunSaveData saved=RunProgress.Read();Assert.That(saved.experimentMode,Is.True);Assert.That(saved.runSeed,Is.EqualTo(7342));
        }
        finally{if(had)PlayerPrefs.SetString(RunProgress.SaveKey,original);else PlayerPrefs.DeleteKey(RunProgress.SaveKey);}
    }
}
