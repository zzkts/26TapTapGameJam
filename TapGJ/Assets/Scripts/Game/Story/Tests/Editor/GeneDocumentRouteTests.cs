using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public sealed class GeneDocumentRouteTests
{
    [Test]
    public void ElevatedEncounterDropsLandOnTheAccessibleMainFloor()
    {
        const float x=11000;
        var floor=new GameObject("Isolated drop floor");floor.layer=LayerMask.NameToLayer("Ground");
        floor.transform.position=new Vector2(x,-.5f);floor.AddComponent<BoxCollider2D>().size=new Vector2(8,1);
        try
        {
            Physics2D.SyncTransforms();
            DropService.Spawn(new[]{Resources.Load<Modifier>("SO/Genes/water")},2,new Vector2(x,8),1.6f);
            var drops=Object.FindObjectsByType<GenePickup>(FindObjectsSortMode.None).Where(p=>Mathf.Abs(p.transform.position.x-x)<2).ToArray();
            Assert.That(drops.Length,Is.EqualTo(2));
            Assert.That(drops.All(p=>Mathf.Abs(p.transform.position.y-.25f)<.01f),Is.True);
        }
        finally
        {
            foreach(var drop in Object.FindObjectsByType<GenePickup>(FindObjectsSortMode.None).Where(p=>Mathf.Abs(p.transform.position.x-x)<2))Object.DestroyImmediate(drop.gameObject);
            Object.DestroyImmediate(floor);
        }
    }

    [Test]
    public void ContinuousWorldHasNoPrototypeMechanicsOrBlockingPuzzleBodies()
    {
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scene/GameScene.unity");
        try
        {
            var roots=scene.GetRootGameObjects();
            var components=roots.SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null).ToArray();
            string[] removed={"PlayerReservoir","PlayerSynthesis","PlayerBackflowPulse","CampaignReactor","PumpLoopSystem","WaterReservoir2D","EcoFungus","EmergenceBody","EmergenceGate","EmergenceProgress","WatershedNetwork"};
            Assert.That(components.Any(c=>removed.Contains(c.GetType().Name)),Is.False);
            var flow=components.OfType<CampaignFlow>().Single();
            Assert.That(flow.worldWidth,Is.EqualTo(144));
            Assert.That(flow.level.guardianEntryX,Is.EqualTo(112));
            Assert.That(flow.level.barriers,Is.Empty);
            Assert.That(flow.level.guardians.Length,Is.EqualTo(3));
            Assert.That(components.OfType<EnemySupportAura>().Count(),Is.GreaterThanOrEqualTo(3));
            var chests=components.OfType<GeneChest>().ToArray();
            Assert.That(chests.Length,Is.EqualTo(6));
            Assert.That(chests.All(c=>Mathf.Abs(c.transform.position.y)<.01f),Is.True,"No reward requires removed air-step/jet skills");
            Assert.That(components.OfType<GeneEditStation>().Count(),Is.EqualTo(3));
            Assert.That(components.OfType<GeneEditStation>().Any(t=>Mathf.Abs(t.transform.position.x-100)<.01f),Is.True);
            Assert.That(components.OfType<CampaignExit>().Single().GetComponent<Collider2D>().isTrigger,Is.True);
            Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)).Count(),Is.GreaterThan(300),"Existing environment art is preserved");
            foreach(var t in roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject),Is.Zero,t.name);
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }

    [Test]
    public void MainMenuHasOnlyStoryContinueSettingsAndQuit()
    {
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scene/BeginScene.unity");
        try
        {
            var menu=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BeginPanel>(true)).Single();
            Assert.That(menu.transform.Find("Backflow Trial"),Is.Null);
            var text=string.Join("\n",menu.GetComponentsInChildren<UnityEngine.UI.Text>(true).Select(t=>t.text));
            foreach(string old in new[]{"吸水","放水","播种","踏跃","水网目标"})Assert.That(text,Does.Not.Contain(old));
            Assert.That(text,Does.Contain("右键"));
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }

    [Test]
    public void OldEcologySaveStillParsesWithoutChangingItsGeneReferencesOrCheckpoint()
    {
        string json="{\"version\":3,\"scene\":\"GameScene\",\"campaignLayout\":true,\"checkpointX\":98,\"checkpointY\":1,\"carriedWater\":5,\"ecosystem\":{\"growth\":1},\"genes\":[{\"definitionName\":\"body_jet\",\"instanceId\":\"old\"},{\"definitionName\":\"water\",\"instanceId\":\"kept\",\"level\":2,\"copies\":3}],\"equipped\":[1,0,-1]}";
        var data=RunProgress.ParseSnapshot(json);
        Assert.That(data,Is.Not.Null);Assert.That(data.checkpointX,Is.EqualTo(98));
        Assert.That(data.genes[1].instanceId,Is.EqualTo("kept"));Assert.That(data.genes[1].level,Is.EqualTo(2));
        Assert.That(data.equipped,Is.EqualTo(new[]{1,0,-1}));
    }
}
