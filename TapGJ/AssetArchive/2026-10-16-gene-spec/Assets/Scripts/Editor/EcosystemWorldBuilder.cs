using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Layers a continuous water-and-ecology loop onto the existing first chapter's art and traversal.</summary>
public static class EcosystemWorldBuilder
{
    private static Transform root;
    private static Font font;
    public static void Apply(CampaignFlow flow)
    {
        root=new GameObject("活体水网 · 供水净化动力").transform;root.SetParent(flow.transform);
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/ArtRes/Fonts/PlayerUI/NotoSansCJKsc-Medium.otf");
        EnsureGenes();
        var system=root.gameObject.AddComponent<PumpLoopSystem>();flow.ecosystem=system;
        var boilerNode=flow.reactors.First(n=>n.rule==CampaignReactor.Rule.Pressure);
        var weightNode=flow.reactors.First(n=>n.rule==CampaignReactor.Rule.Weight);
        var circuitNode=flow.reactors.First(n=>n.rule==CampaignReactor.Rule.Conduction);
        system.boiler=boilerNode.material;system.bypass=weightNode.plate;system.relay=circuitNode.material;
        system.cistern=Tank("高处蓄水池",new Vector2(6.5f,4),4,1.5f,10,8,.62f);
        system.pool=Tank("下层净化池",new Vector2(27.5f,-1.7f),7,2.4f,10,4,.78f);
        system.pool.readout.transform.parent.position=new Vector2(27.5f,3.25f);
        var plantObj=new GameObject("共生母菌");plantObj.transform.SetParent(root);plantObj.transform.position=new Vector2(30.2f,-.9f);plantObj.layer=3;
        var sr=Visual("母菌图像","mushroom",new Vector2(30.2f,-.2f),new Vector2(1.4f,1.7f),5,plantObj.transform);
        var body=plantObj.AddComponent<EmergenceBody>();body.material=EmergenceMaterial.Organic;
        var trigger=plantObj.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=new Vector2(1.2f,1.7f);trigger.offset=Vector2.up*.7f;
        system.fungus=plantObj.AddComponent<EcoFungus>();system.fungus.reservoir=system.pool;system.fungus.visual=sr;
        system.fungus.readout=Label("母菌状态",new Vector2(32.6f,3.25f));
        var bridge=new GameObject("生长菌桥");bridge.transform.SetParent(root);
        for(int i=0;i<5;i++)
        {
            var cap=new GameObject("菌桥落脚点 "+i);cap.transform.SetParent(bridge.transform);cap.transform.position=new Vector2(24.5f+i*1.45f,.15f);cap.layer=9;
            var box=cap.AddComponent<BoxCollider2D>();box.size=new Vector2(1.6f,.2f);box.usedByEffector=true;
            var eff=cap.AddComponent<PlatformEffector2D>();eff.useOneWay=true;eff.surfaceArc=170;cap.AddComponent<OneWayPlatform>();
            Visual("菌伞","mushroom",cap.transform.position-Vector3.up*.35f,new Vector2(1.75f,1),4,cap.transform);
        }
        system.fungus.bridge=bridge;bridge.SetActive(false);
        Platform(new Vector2(6.5f,4),5);Platform(new Vector2(24.5f,-.7f),1.2f);Platform(new Vector2(30.5f,-.7f),1.2f);
        // Visible lower floor prevents an empty-looking or lethal pit; water, floats and fungus alter the crossing.
        Visual("净化池底","rock_fill",new Vector2(27.5f,-3.1f),new Vector2(7.1f,2.8f),0);
        var floor=new GameObject("净化池底碰撞");floor.transform.SetParent(root);floor.layer=3;floor.transform.position=new Vector2(27.5f,-2.1f);floor.AddComponent<BoxCollider2D>().size=new Vector2(7,.8f);
        var cargo=flow.GetComponentsInChildren<EmergenceBody>().First(b=>b.Has(EmergenceMaterial.Movable));cargo.transform.position=new Vector2(26,-1.15f);
        cargo.GetComponent<Rigidbody2D>().mass=.8f;
        system.pipe=Visual("从高池到低池的输水管","pipe_horizontal",new Vector2(17,3.65f),new Vector2(22,.32f),-4);
        Visual("上层引水管","pipe_elbow",new Vector2(7.8f,4.5f),new Vector2(.8f,1.3f),-3);
        Visual("下层落水口","pipe_elbow",new Vector2(27.5f,1.8f),new Vector2(.85f,1.5f),-3);
        system.outflow=Visual("主泵清水出口","pipe_horizontal",new Vector2(21.5f,.35f),new Vector2(2.5f,.25f),6);system.outflow.enabled=false;
        system.readout=Label("主泵状态",new Vector2(20,2.3f));
        Label("水网提示",new Vector2(8,1.7f)).text="高池补水 → 下池净化 → 主泵出水\n重量阀加速输水 · 水火蒸汽也可供能";
        var monitored=new[]{boilerNode,weightNode,circuitNode};
        var names=new[]{"下层供水","菌群净化","主泵动力"};
        var hints=new[]{"S+F 搬水，F 换水；重量阀加速高池来水","浇水让母菌生长；净化同时长出菌桥","高处湿润电路，或左侧水火蒸汽供能"};
        for(int i=0;i<3;i++)
        {
            var node=monitored[i];node.ecosystem=system;node.channel=i;node.displayName=names[i];node.hint=hints[i];
            node.transform.position=i==0?system.pool.transform.position:i==1?system.fungus.transform.position:system.relay.transform.position;
            node.beacon.transform.position=node.transform.position+Vector3.up*1.8f;
        }
        foreach(var c in flow.GetComponentsInChildren<Canvas>())
            if(c.name.StartsWith("路标 · 蓄压节点")||c.name.StartsWith("路标 · 重量节点")||c.name.StartsWith("路标 · 高处导电节点")||c.name.StartsWith("路标 · 中央出口"))Object.DestroyImmediate(c.gameObject);
        flow.reactors=monitored;
        Camera.main.transform.position=new Vector3(18,2.2f,-10);
        var controller=flow.player.GetComponent<PlayerReservoir>();if(controller==null)flow.player.gameObject.AddComponent<PlayerReservoir>();
    }
    private static WaterReservoir2D Tank(string name,Vector2 bottom,float width,float height,float capacity,float volume,float pollution)
    {
        var obj=new GameObject(name);obj.transform.SetParent(root);obj.transform.position=bottom;obj.layer=3;
        var trigger=obj.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=new Vector2(width,height);trigger.offset=Vector2.up*height*.5f;
        obj.AddComponent<EmergenceBody>().material=EmergenceMaterial.Conductive;
        var tank=obj.AddComponent<WaterReservoir2D>();tank.reservoirId=name;tank.width=width;tank.maxHeight=height;tank.capacity=capacity;tank.initialVolume=volume;tank.initialPollution=pollution;
        var face=new GameObject("可见水位");face.transform.SetParent(obj.transform,false);var sr=face.AddComponent<SpriteRenderer>();sr.sprite=Resources.Load<Sprite>("Prototype/White");
        sr.sharedMaterial=EnsureWaterMaterial();sr.sortingOrder=6;tank.water=sr;
        tank.readout=Label(name+"读数",bottom+new Vector2(0,height+1));return tank;
    }
    private static Material EnsureWaterMaterial()
    {
        const string path="Assets/Resources/WaterProjectiles/Reservoir.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("TapGJ/Reservoir 2D"));AssetDatabase.CreateAsset(mat,path);}return mat;
    }
    private static void EnsureGenes()
    {
        var keys=new[]{"body_cling","body_jet","body_symbiosis"};var names=new[]{"黏附体液","喷射体液","共生体液"};
        var desc=new[]{"F 释放的水迹持续 18 秒，可附在墙上搭建湿润电路。","瞄准下方按 F 放水，反冲让自己腾空；需要身体内有储水。","F 放水时留下临时菌簇，菌簇吸水生长并帮助净化附近水池。"};
        for(int i=0;i<3;i++)
        {
            string path="Assets/Resources/SO/Genes/"+keys[i]+".asset";var gene=AssetDatabase.LoadAssetAtPath<Modifier>(path);
            if(gene==null){gene=ScriptableObject.CreateInstance<Modifier>();AssetDatabase.CreateAsset(gene,path);}
            var so=new SerializedObject(gene);so.FindProperty("geneId").longValue=101+i;so.FindProperty("geneName").stringValue=names[i];so.FindProperty("desc").stringValue=desc[i];
            so.FindProperty("modifierType").intValue=(int)E_ModifierType.BodyTrait;so.FindProperty("modifierValue").floatValue=i+1;so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    private static SpriteRenderer Visual(string name,string key,Vector2 at,Vector2 size,int order,Transform parent=null)
    {
        var obj=new GameObject(name);obj.transform.SetParent(parent!=null?parent:root);obj.transform.position=at;
        var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/WorldPolished/"+key+".png");sr.sortingOrder=order;
        obj.transform.localScale=new Vector3(size.x/sr.sprite.bounds.size.x,size.y/sr.sprite.bounds.size.y,1);return sr;
    }
    private static Text Label(string name,Vector2 at)
    {
        var obj=new GameObject(name,typeof(RectTransform),typeof(Canvas));obj.transform.SetParent(root);obj.transform.position=at;obj.transform.localScale=Vector3.one*.012f;
        var canvas=obj.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=18;
        obj.GetComponent<RectTransform>().sizeDelta=new Vector2(370,80);
        var t=GameplayUIFactory.Label("Text",obj.transform,Vector2.zero,Vector2.one,font,17,"",TextAnchor.MiddleCenter);t.color=PlayerUIStyle.Cream;t.raycastTarget=false;return t;
    }
    private static void Platform(Vector2 at,float width)
    {
        var obj=new GameObject("水网踏台");obj.transform.SetParent(root);obj.transform.position=at-Vector2.up*.1f;obj.layer=9;
        var box=obj.AddComponent<BoxCollider2D>();box.size=new Vector2(width,.2f);box.usedByEffector=true;
        var eff=obj.AddComponent<PlatformEffector2D>();eff.useOneWay=true;eff.surfaceArc=170;obj.AddComponent<OneWayPlatform>();
        Visual("踏台","metal_deck",at-Vector2.up*.1f,new Vector2(width,.25f),4,obj.transform);
    }
}
