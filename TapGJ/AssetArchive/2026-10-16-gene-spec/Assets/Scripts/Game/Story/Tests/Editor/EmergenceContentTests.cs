using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class EmergenceContentTests
{
    [Test]
    public void ContinuousWorldHasThreeDistrictsOneFinalTriggerAndNoProgressionWalls()
    {
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scene/GameScene.unity");
        try
        {
            var roots=scene.GetRootGameObjects();
            var flow=roots.SelectMany(r=>r.GetComponentsInChildren<CampaignFlow>(true)).Single();
            Assert.That(flow.unified,Is.True);Assert.That(flow.worldWidth,Is.EqualTo(144));
            Assert.That(flow.player.transform.position.x,Is.EqualTo(3));Assert.That(flow.level.spawn.x,Is.EqualTo(3));
            Assert.That(flow.reactors.Length,Is.EqualTo(9));
            foreach(int district in new[]{1,2,3})
            {
                var nodes=flow.reactors.Where(n=>n.district==district).ToArray();Assert.That(nodes.Length,Is.EqualTo(3));
                Assert.That(nodes.All(n=>!string.IsNullOrEmpty(n.hint)&&n.beacon!=null),Is.True);
                Assert.That(nodes.All(n=>CampaignFlow.DistrictAt(n.transform.position.x)==district),Is.True);
            }
            Assert.That(flow.exit.transform.position.x,Is.GreaterThan(135));
            Assert.That(flow.exit.GetComponentsInChildren<Collider2D>().All(c=>c.isTrigger),Is.True);
            Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<EmergenceGate>(true)).Count(),Is.Zero);
            Assert.That(flow.GetComponent<WatershedNetwork>().forestBridge,Is.Not.Null);
            Assert.That(flow.level.guardians.Length,Is.EqualTo(3));
            Assert.That(flow.ecosystem,Is.Not.Null);
            Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<GeneChest>(true)).Count(c=>c.offerChoice),Is.EqualTo(6));
            Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<EnemySupportAura>(true)).Count(),Is.GreaterThanOrEqualTo(3));
            var dependencies=AssetDatabase.GetDependencies("Assets/Scene/GameScene.unity",true);
            Assert.That(dependencies.Any(d=>d.EndsWith("SporeScene.unity")||d.EndsWith("CoreScene.unity")),Is.False);
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
    [Test]
    public void BuildContainsOnlyMenuAndContinuousGameplay()
    {Assert.That(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),Is.EqualTo(new[]{"Assets/Scene/BeginScene.unity","Assets/Scene/GameScene.unity"}));}
    [TestCase("SporeScene",65f)]
    [TestCase("CoreScene",113f)]
    public void OlderChapterCheckpointMigratesToContinuousWorldOnce(string oldScene,float x)
    {
        bool had=PlayerPrefs.HasKey(RunProgress.SaveKey);string original=PlayerPrefs.GetString(RunProgress.SaveKey);
        try
        {
            PlayerPrefs.SetString(RunProgress.SaveKey,"{\"version\":2,\"campaignLayout\":true,\"scene\":\""+oldScene+"\",\"checkpointX\":17,\"completedTriggers\":[\"campaign:"+oldScene+":生长节点\"]}");
            var data=RunProgress.Read();Assert.That(data.scene,Is.EqualTo("GameScene"));Assert.That(data.checkpointX,Is.EqualTo(x));
            Assert.That(data.completedTriggers[0],Does.StartWith("campaign:GameScene:区域"));
            RunProgress.Write(data);Assert.That(RunProgress.Read().checkpointX,Is.EqualTo(x));
        }
        finally{if(had)PlayerPrefs.SetString(RunProgress.SaveKey,original);else PlayerPrefs.DeleteKey(RunProgress.SaveKey);}
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
            RunSaveData data=RunProgress.Read();Assert.That(data.version,Is.EqualTo(3));Assert.That(data.checkpointX,Is.EqualTo(3));
            Assert.That(data.genes[0].instanceId,Is.EqualTo("keep-me"));
            RunProgress.Write(data);Assert.That(RunProgress.Read().checkpointX,Is.EqualTo(3),"Migration must not run twice");
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
