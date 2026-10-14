using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class RunProgressTests
{
    private bool hadSave,hadEndings;
    private string previousSave;
    private int previousEndings;

    [SetUp]
    public void PreserveUserProgress()
    {
        hadSave=PlayerPrefs.HasKey(RunProgress.SaveKey);
        previousSave=PlayerPrefs.GetString(RunProgress.SaveKey);
        hadEndings=PlayerPrefs.HasKey(RunProgress.EndingKey);
        previousEndings=PlayerPrefs.GetInt(RunProgress.EndingKey);
        PlayerPrefs.DeleteKey(RunProgress.SaveKey);
        PlayerPrefs.DeleteKey(RunProgress.EndingKey);
    }

    [TearDown]
    public void RestoreUserProgress()
    {
        if(hadSave)PlayerPrefs.SetString(RunProgress.SaveKey,previousSave);else PlayerPrefs.DeleteKey(RunProgress.SaveKey);
        if(hadEndings)PlayerPrefs.SetInt(RunProgress.EndingKey,previousEndings);else PlayerPrefs.DeleteKey(RunProgress.EndingKey);
        PlayerPrefs.Save();
    }

    [Test]
    public void CheckpointPersistsLoadoutInstanceIdentityAndCompletedTriggers()
    {
        var data=new RunSaveData{checkpointX=41f,checkpointY=1f,elapsedSeconds=127.5f,region=1,dashUnlocked=true};
        data.genes.Add(new SavedGene{geneId=7,definitionName="Water",instanceId="evolved-water",level=2,copies=3});
        data.genes.Add(new SavedGene{geneId=8,definitionName="Fire",instanceId="fire",level=1,copies=2});
        data.equipped=new[]{1,-1,0,-1,-1,-1};
        data.completedTriggers.Add("Sample-Water-13.5");
        RunProgress.Write(data);
        RunSaveData saved=RunProgress.Read();
        Assert.That(RunProgress.CanContinue,Is.True);
        Assert.That(saved.checkpointX,Is.EqualTo(41f));
        Assert.That(saved.elapsedSeconds,Is.EqualTo(127.5f));
        Assert.That(saved.dashUnlocked,Is.True);
        Assert.That(saved.genes[0].instanceId,Is.EqualTo("evolved-water"));
        Assert.That(saved.genes[0].level,Is.EqualTo(2));
        Assert.That(saved.genes[1].copies,Is.EqualTo(2));
        CollectionAssert.AreEqual(data.equipped,saved.equipped);
        CollectionAssert.AreEqual(data.completedTriggers,saved.completedTriggers);
    }

    [TestCase(true,"水的归处",1)]
    [TestCase(false,"最后的回声",2)]
    public void FinishingEitherEndingDisablesContinueAndRecordsTheDecision(bool coexistence,string title,int bit)
    {
        RunProgress.Finish(new RunSaveData{guardianCleared=true},coexistence);
        RunSaveData saved=RunProgress.Read();
        Assert.That(saved.completed,Is.True);
        Assert.That(saved.ending,Is.EqualTo(title));
        Assert.That(RunProgress.CanContinue,Is.False);
        Assert.That(RunProgress.DiscoveredEndings,Is.EqualTo(bit));
    }

    [Test]
    public void DiscoveringAnotherEndingKeepsBothDiscoveries()
    {
        RunProgress.Finish(new RunSaveData(),true);
        RunProgress.Finish(new RunSaveData(),false);
        Assert.That(RunProgress.DiscoveredEndings,Is.EqualTo(3));
    }

    [TestCase("not json")]
    [TestCase("{\"version\":999,\"scene\":\"GameScene\"}")]
    [TestCase("{\"version\":1,\"scene\":\"BeginScene\"}")]
    public void InvalidOrUnsupportedSaveDoesNotEnableContinue(string json)
    {
        PlayerPrefs.SetString(RunProgress.SaveKey,json);
        Assert.That(RunProgress.Read(),Is.Null);
        Assert.That(RunProgress.CanContinue,Is.False);
    }

    [Test]
    public void OlderCheckpointWithInvalidClockAndRegionNormalizesForCurrentChapter()
    {
        RunProgress.Write(new RunSaveData{elapsedSeconds=-50f,region=9});
        RunSaveData saved=RunProgress.Read();
        Assert.That(saved.elapsedSeconds,Is.Zero);
        Assert.That(saved.region,Is.EqualTo(2));
        Assert.That(RunProgress.CanContinue,Is.True);
    }

    [Test]
    public void EveryNarrativeBeatAndBothEndingsHaveSpeakerAndReadableText()
    {
        foreach(StoryBeat beat in Enum.GetValues(typeof(StoryBeat)))
        {
            if(beat==StoryBeat.Checkpoint)continue;
            StoryLine[] lines=StoryContent.Lines(beat);
            Assert.That(lines,Is.Not.Empty,beat.ToString());
            Assert.That(lines.All(line=>line!=null&&!string.IsNullOrWhiteSpace(line.speaker)&&!string.IsNullOrWhiteSpace(line.text)),Is.True,beat.ToString());
        }
        Assert.That(StoryContent.Ending(true),Is.Not.Empty);
        Assert.That(StoryContent.Ending(false),Is.Not.Empty);
        Assert.That(StoryContent.Ending(true).Last().text,Is.Not.EqualTo(StoryContent.Ending(false).Last().text));
    }

    [Test]
    public void AStoryTriggerCanBeAuthoredWithItsRequiredConcreteCollider()
    {
        var obj=new GameObject("Story Trigger Test");
        try
        {
            StoryTrigger trigger=obj.AddComponent<StoryTrigger>();
            Assert.That(trigger,Is.Not.Null);
            Assert.That(obj.GetComponent<BoxCollider2D>(),Is.Not.Null);
        }
        finally{UnityEngine.Object.DestroyImmediate(obj);}
    }

    [Test]
    public void WorldSpaceSignCannotHostStoryDialogueWhenThePlayerHudExists()
    {
        Scene scene=EditorSceneManager.NewPreviewScene();
        try
        {
            Canvas sign=CanvasInScene(scene,"Wayfinding Sign",RenderMode.WorldSpace);
            Canvas hudCanvas=CanvasInScene(scene,"Gameplay Canvas",RenderMode.ScreenSpaceOverlay);
            GameObject hud=new GameObject("HUD",typeof(RectTransform));hud.transform.SetParent(hudCanvas.transform,false);hud.AddComponent<PlayerHudPanel>();
            StoryDialogPanel panel=StoryUIFactory.Ensure(sign);
            Assert.That(panel.GetComponentInParent<Canvas>(),Is.SameAs(hudCanvas));
            Assert.That(hudCanvas.GetComponentInChildren<StoryObjectivePanel>(true),Is.Not.Null);
            Assert.That(sign.GetComponentInChildren<StoryDialogPanel>(true),Is.Null);
            Assert.That(sign.renderMode,Is.EqualTo(RenderMode.WorldSpace));
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }

    [Test]
    public void ASceneWithOnlyWorldSpaceCanvasesGetsAScreenOverlayForItsStory()
    {
        Scene scene=EditorSceneManager.NewPreviewScene();
        try
        {
            Canvas enemyBar=CanvasInScene(scene,"Enemy Health Bar",RenderMode.WorldSpace);
            StoryDialogPanel panel=StoryUIFactory.Ensure(enemyBar);
            Canvas overlay=panel.GetComponentInParent<Canvas>();
            Assert.That(overlay,Is.Not.SameAs(enemyBar));
            Assert.That(overlay.renderMode,Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(overlay.gameObject.scene,Is.EqualTo(scene));
            Assert.That(overlay.GetComponent<UnityEngine.UI.GraphicRaycaster>(),Is.Not.Null);
            Assert.That(overlay.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution,Is.EqualTo(new Vector2(1600f,900f)));
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }

    private static Canvas CanvasInScene(Scene scene,string name,RenderMode mode)
    {
        GameObject obj=new GameObject(name,typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(obj,scene);
        Canvas canvas=obj.GetComponent<Canvas>();canvas.renderMode=mode;return canvas;
    }
}
