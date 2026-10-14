using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class EmergenceContentTests
{
    [Test]
    public void ExpandedChapterHasMultipleRoutesChoicesAndPersistentCircuit()
    {
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scene/GameScene.unity");
        try
        {
            var roots=scene.GetRootGameObjects();
            CompleteLevelFlow level=roots.SelectMany(r=>r.GetComponentsInChildren<CompleteLevelFlow>(true)).Single();
            EmergenceProgress progress=level.GetComponent<EmergenceProgress>();
            Assert.That(progress,Is.Not.Null);Assert.That(progress.coreGate.barrier,Is.Not.Null);
            Assert.That(level.guardianEntryX,Is.GreaterThan(230));Assert.That(level.checkpoints.Length,Is.EqualTo(6));
            Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<GeneChest>(true)).Count(c=>c.offerChoice),Is.EqualTo(7));
            var bodies=level.GetComponentsInChildren<EmergenceBody>(true);
            Assert.That(bodies.Count(b=>b.Has(EmergenceMaterial.Conductive)),Is.EqualTo(5));
            Assert.That(bodies.Count(b=>b.Has(EmergenceMaterial.Organic)),Is.EqualTo(4));
            Assert.That(bodies.Count(b=>b.Has(EmergenceMaterial.Movable)),Is.EqualTo(2));
            Assert.That(level.GetComponentsInChildren<EmergenceConsole>().Where(c=>c.completionId=="duct").Count(),Is.EqualTo(1));
            Assert.That(bodies.Any(b=>b.completionId=="duct"&&b.chargedGate!=null),Is.True);
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
