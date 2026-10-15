using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>Compact chapters with a central return point and connected vertical/side loops.</summary>
public static class CampaignWorldBuilder
{
    public static bool Enabled=>true;
    private static Transform root;
    private static Font font;
    private static CampaignFlow flow;
    private static readonly Dictionary<string,Sprite> art=new();
    [MenuItem("Tools/TapGJ/Rebuild Three Chapter Campaign")]
    public static void ApplyAll()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
        EditorSceneManager.OpenScene("Assets/Scene/GameScene.unity");Build(1);Save("Assets/Scene/GameScene.unity");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scene/SporeScene.unity",true);
        EditorSceneManager.OpenScene("Assets/Scene/SporeScene.unity");Build(2);Save("Assets/Scene/SporeScene.unity");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scene/CoreScene.unity",true);
        EditorSceneManager.OpenScene("Assets/Scene/CoreScene.unity");Build(3);Save("Assets/Scene/CoreScene.unity");
        EditorBuildSettings.scenes=new[]{"BeginScene","GameScene","SporeScene","CoreScene"}.Select(s=>new EditorBuildSettingsScene("Assets/Scene/"+s+".unity",true)).ToArray();
        EditorSceneManager.OpenScene("Assets/Scene/BeginScene.unity");CompleteGameMenuBuilder.Apply();
        EditorSceneManager.OpenScene("Assets/Scene/GameScene.unity");AssetDatabase.SaveAssets();
    }
    private static void Save(string path)
    {EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),path);}
    private static void Build(int chapter)
    {
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/ArtRes/Fonts/PlayerUI/NotoSansCJKsc-Medium.otf");
        art.Clear();foreach(string key in new[]{"CavernBackdrop","rock_fill","rock_top_moss","metal_deck","pipe_horizontal","pipe_elbow","crystal","mushroom","gene_terminal","supply_crate","core_gate"})
            art[key]=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/WorldPolished/"+key+".png");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var player=Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        var camera=Camera.main;var story=Object.FindFirstObjectByType<StorySession>();
        player.transform.SetParent(null);camera.transform.SetParent(null);
        foreach(var obj in scene.GetRootGameObjects())
        {
            bool keep=obj==player.gameObject||obj==camera.gameObject||obj.GetComponent<StorySession>()!=null||obj.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true)!=null||
                obj.GetComponentsInChildren<Canvas>(true).Any(c=>c.renderMode!=RenderMode.WorldSpace);
            if(!keep)Object.DestroyImmediate(obj);
        }
        root=new GameObject("Campaign Chapter "+chapter).transform;
        flow=root.gameObject.AddComponent<CampaignFlow>();flow.chapter=chapter;flow.player=player;
        flow.level=root.gameObject.AddComponent<CompleteLevelFlow>();flow.level.player=player;flow.level.spawn=new Vector2(17,1);
        flow.level.firstRegionEnd=1000;flow.level.secondRegionEnd=1001;flow.level.checkpoints=new[]{new Vector2(17,1)};flow.level.guardianEntryX=1000;
        var progress=root.gameObject.AddComponent<EmergenceProgress>();progress.startX=0;progress.endX=40;
        story.player=player;story.level=flow.level;
        player.transform.position=new Vector3(17,1,0);player.gameObject.SetActive(true);
        if(player.GetComponent<PlayerSynthesis>()==null)player.gameObject.AddComponent<PlayerSynthesis>();
        var inventory=new SerializedObject(player.GetComponent<GeneInventory>());inventory.FindProperty("genInventory").arraySize=0;inventory.FindProperty("genChain").arraySize=0;inventory.ApplyModifiedPropertiesWithoutUndo();
        camera.transform.position=new Vector3(18,3.2f,-10);camera.orthographicSize=5.4f;
        var follow=camera.GetComponent<PlayerCameraFollow>();follow.target=player.transform;follow.horizontalLimits=new Vector2(9.5f,30.5f);
        var light=new GameObject("Chapter Light").AddComponent<Light>();light.transform.SetParent(root);light.type=LightType.Directional;light.intensity=.8f;
        Color tint=chapter==1?new Color(.85f,.86f,1f):chapter==2?new Color(.66f,1f,.73f):new Color(.70f,.75f,1f);
        for(int i=0;i<3;i++)Visual("环境幕布 "+i,"CavernBackdrop",new Vector2(i*20+0,7),new Vector2(20.1f,24),-40).color=tint;
        Ground(0,40);Wall(-.5f);Wall(40.5f);
        for(int i=0;i<12;i++)
        {
            float x=1.5f+i*3.3f;
            Visual("环境群落 "+i,chapter==2||i%3==0?"mushroom":"crystal",new Vector2(x,.9f),new Vector2(1.3f,1.8f),-7).color=tint;
            if(i%4==0)Visual("管网 "+i,"pipe_horizontal",new Vector2(x,10.5f),new Vector2(9,.5f),-6).color=tint;
        }
        var exitObj=Item("中央章节出口","core_gate",new Vector2(20,0),new Vector2(1.4f,2.2f));
        flow.exit=exitObj.AddComponent<CampaignExit>();flow.exit.interactionRange=2;
        var exitCollider=exitObj.AddComponent<BoxCollider2D>();exitCollider.isTrigger=true;exitCollider.size=new Vector2(1.2f,2);exitCollider.offset=Vector2.up;
        Label("中央出口 · E",new Vector2(20,2.6f));
        var terminal=Item("中央基因终端","gene_terminal",new Vector2(14,0),new Vector2(1.3f,1.7f));terminal.AddComponent<GeneEditStation>().interactionRange=2;
        var checkpoint=new GameObject("中央安全点");checkpoint.transform.SetParent(root);checkpoint.transform.position=new Vector2(17,1);
        var cp=checkpoint.AddComponent<StoryTrigger>();cp.beat=StoryBeat.Checkpoint;cp.triggerId="campaign-checkpoint-"+chapter;
        cp.GetComponent<BoxCollider2D>().isTrigger=true;cp.GetComponent<BoxCollider2D>().size=new Vector2(2,3);
        Label("Q 切换 / R 播种 · M 地图与目标",new Vector2(17,3.1f));
        List<CampaignReactor> nodes=new();
        if(chapter==1)
        {
            var boiler=Body("左侧蓄压罐","pipe_elbow",new Vector2(6,0),new Vector2(1.2f,1.2f),EmergenceMaterial.Boiler);
            nodes.Add(Node("蓄压节点","水种蓄水三次；水 + 火也能驱动",CampaignReactor.Rule.Pressure,boiler,null,"ember"));
            var plate=Plate(31.5f);Crate(27);
            nodes.Add(Node("重量节点","人物、箱体或种子压住阀板",CampaignReactor.Rule.Weight,null,plate,"split"));
            nodes.Add(Circuit(18,6,"高处导电节点"));
            Stair(12,1);Stair(14.3f,2);Stair(17,3);Stair(20,4);Stair(17,5);Platform(17,6,5.5f);
            Stair(4,1);Stair(6.5f,2);Platform(7,3,4);Stair(9.5f,4);Stair(12,5);Platform(14,6,4);
            Stair(23,3);Stair(26,2);Stair(29,1);
            Enemy(3,8,0,5);Enemy(6,30,2.4f,5);Enemy(4,32,1.5f,4);Enemy(5,20,4,6);
        }
        else if(chapter==2)
        {
            var living=Body("左侧母菌","mushroom",new Vector2(5,0),new Vector2(1.7f,2.2f),EmergenceMaterial.Organic);
            nodes.Add(Node("生长节点","水种 / 水球让母菌生长",CampaignReactor.Rule.Growth,living,null,"sewage"));
            for(int i=0;i<3;i++)Body("右侧菌落 "+i,"mushroom",new Vector2(25+i*2.3f,0),new Vector2(1.2f,1.8f),EmergenceMaterial.Organic);
            var shell=Body("连锁菌壳","crystal",new Vector2(32,0),new Vector2(1.5f,2),EmergenceMaterial.Fragile|EmergenceMaterial.Flammable);
            nodes.Add(Node("连锁节点","孢子种 + 火种爆破，或击碎菌壳",CampaignReactor.Rule.Fracture,shell,null,"bounce"));
            nodes.Add(Circuit(20,7,"菌冠导电节点"));
            Stair(3,1);Stair(6,2);Stair(8.5f,3);Stair(11,4);Stair(14,5);Stair(17,6);Platform(19,7,5.5f);
            Stair(23,6);Stair(26,5);Stair(29,4);Stair(32,3);Stair(35,2);Stair(37,1);
            Body("可借力的菌林水罐","pipe_elbow",new Vector2(11,0),new Vector2(1.2f,1.2f),EmergenceMaterial.Boiler);
            Platform(11,3,4);Enemy(3,7,0,7);Enemy(5,10,0,8);Enemy(4,12,1.5f,5);Enemy(6,29,3,6);Enemy(4,30,1.5f,5);Enemy(3,33,0,7);
        }
        else
        {
            var boiler=Body("左侧核心热泵","pipe_elbow",new Vector2(5,0),new Vector2(1.4f,1.3f),EmergenceMaterial.Boiler);
            nodes.Add(Node("相变核心","水种蓄压 / 水火相变产生推力",CampaignReactor.Rule.Pressure,boiler,null,"size"));
            var plate=Plate(37.5f);Crate(31);
            nodes.Add(Node("惯性核心","湿润箱体或种子改变机关重量",CampaignReactor.Rule.Weight,null,plate,"pierce"));
            nodes.Add(Circuit(20,8,"高处回流核心"));
            for(int i=0;i<8;i++)Stair(i%2==0?12:15,i+1);
            Platform(19,8,7);for(int i=0;i<7;i++)Stair(23+i*2,7-i);
            Platform(6,3,4);Stair(8.5f,4);Stair(11,5);
            Enemy(3,6,0,7);Enemy(6,34,2.5f,7);Enemy(4,33,1.2f,5);
            var a=Enemy(5,10,0,20);var b=Enemy(6,27,3,14);var c=Enemy(3,29,0,14);
            a.name="核心守卫 · 菌壳";b.name="核心守卫 · 传导";c.name="核心守卫 · 火流";
            flow.level.guardians=new[]{a,b,c};foreach(var g in flow.level.guardians)g.gameObject.SetActive(false);
        }
        flow.reactors=nodes.ToArray();
        if(chapter>1)Platform(34,3,2);
        Chest(chapter,34,chapter==1?0:3);Chest(chapter,19,chapter==1?6:chapter==2?7:8);
        var supply=Item("中央恢复补给","supply_crate",new Vector2(22.8f,0),new Vector2(.8f,.8f));supply.AddComponent<PlayerSupply>().healAmount=6;
        supply.AddComponent<CompleteRewardState>().rewardId="campaign-health-"+chapter;
        GeneEditorLayoutPolish.Rebuild(Object.FindFirstObjectByType<GeneEditPanel>(FindObjectsInactive.Include));
        EditorUtility.SetDirty(story);EditorUtility.SetDirty(flow);EditorUtility.SetDirty(flow.level);Physics2D.SyncTransforms();
    }
    private static CampaignReactor Circuit(float x,float y,string name)
    {
        var receiver=Body("导电接收器","crystal",new Vector2(x,y),new Vector2(.7f,1),EmergenceMaterial.Conductive);
        var emitter=new GameObject("间歇电源");emitter.transform.SetParent(root);emitter.transform.position=new Vector2(x-2.3f,y+.4f);
        var source=emitter.AddComponent<EnvironmentElementSource>();source.element=E_ElementType.Electric;source.radius=1;source.interval=2.4f;
        Visual("电源指示","crystal",emitter.transform.position,new Vector2(.6f,.7f),5).color=PlayerUIStyle.Gold;
        return Node(name,"湿润接收器，用水种架起电路",CampaignReactor.Rule.Conduction,receiver,null,"vortex");
    }
    private static CampaignReactor Node(string name,string hint,CampaignReactor.Rule rule,EmergenceBody body,EmergencePlate plate,string reward)
    {
        var obj=new GameObject(name);obj.transform.SetParent(root);obj.transform.position=body!=null?body.transform.position:plate.transform.position;
        var node=obj.AddComponent<CampaignReactor>();node.displayName=name;node.hint=hint;node.rule=rule;node.material=body;node.plate=plate;node.reward=reward;
        node.beacon=Visual(name+"状态","core_gate",obj.transform.position+Vector3.up*2,new Vector2(.5f,.65f),6);node.beacon.color=PlayerUIStyle.Violet;
        Label(name+" · "+hint,(Vector2)obj.transform.position+Vector2.up*2.7f);return node;
    }
    private static EmergencePlate Plate(float x)
    {var obj=new GameObject("重量阀板");obj.transform.SetParent(root);obj.transform.position=new Vector2(x,0);var p=obj.AddComponent<EmergencePlate>();p.visual=Visual("压板","metal_deck",new Vector2(x,.08f),new Vector2(1.8f,.16f),4);return p;}
    private static void Crate(float x)
    {var body=Body("可推动箱体","supply_crate",new Vector2(x,.08f),Vector2.one*.8f,EmergenceMaterial.Movable|EmergenceMaterial.Flammable,true);var rb=body.gameObject.AddComponent<Rigidbody2D>();rb.mass=1.2f;rb.gravityScale=.65f;rb.freezeRotation=true;rb.linearDamping=.8f;}
    private static EmergenceBody Body(string name,string key,Vector2 bottom,Vector2 size,EmergenceMaterial flags,bool solid=false)
    {var obj=Item(name,key,bottom,size);obj.layer=LayerMask.NameToLayer("Ground");var box=obj.AddComponent<BoxCollider2D>();box.isTrigger=!solid;box.size=size*.8f;box.offset=Vector2.up*size.y*.4f;var b=obj.AddComponent<EmergenceBody>();b.material=flags;return b;}
    private static GameObject Item(string name,string key,Vector2 bottom,Vector2 size)
    {var obj=new GameObject(name);obj.transform.SetParent(root);obj.transform.position=bottom;Visual(name+"图像",key,bottom+Vector2.up*size.y*.5f,size,3,obj.transform);return obj;}
    private static SpriteRenderer Visual(string name,string key,Vector2 at,Vector2 size,int order,Transform parent=null)
    {var obj=new GameObject(name);obj.transform.SetParent(parent!=null?parent:root);obj.transform.position=at;var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=art[key];sr.sortingOrder=order;obj.transform.localScale=new Vector3(size.x/sr.sprite.bounds.size.x,size.y/sr.sprite.bounds.size.y,1);return sr;}
    private static void Ground(float left,float right)
    {for(float x=left;x<right;x+=4){Visual("地层","rock_fill",new Vector2(x+2,-1.6f),new Vector2(4.02f,3.2f),0);Visual("苔沿","rock_top_moss",new Vector2(x+2,-.05f),new Vector2(4.02f,.25f),2);}var obj=new GameObject("地面碰撞");obj.transform.SetParent(root);obj.layer=LayerMask.NameToLayer("Ground");obj.transform.position=new Vector2((left+right)/2,-1.5f);obj.AddComponent<BoxCollider2D>().size=new Vector2(right-left,3);}
    private static void Wall(float x)
    {var obj=new GameObject("场景边界");obj.transform.SetParent(root);obj.layer=LayerMask.NameToLayer("Wall");obj.transform.position=new Vector2(x,6);obj.AddComponent<BoxCollider2D>().size=new Vector2(1,18);for(int i=0;i<5;i++)Visual("洞壁边缘","rock_fill",new Vector2(x,-1+i*4),new Vector2(1.1f,4.1f),2);}
    private static void Stair(float x,float top)=>Platform(x,top,3.2f);
    private static void Platform(float x,float top,float width)
    {var obj=new GameObject("单向支路平台");obj.transform.SetParent(root);obj.layer=LayerMask.NameToLayer("OneWayPlatform");obj.transform.position=new Vector2(x,top-.12f);var box=obj.AddComponent<BoxCollider2D>();box.size=new Vector2(width,.24f);box.usedByEffector=true;var effect=obj.AddComponent<PlatformEffector2D>();effect.useOneWay=true;effect.surfaceArc=170;obj.AddComponent<OneWayPlatform>();Visual("平台","metal_deck",new Vector2(x,top-.12f),new Vector2(width,.3f),4);}
    private static void Label(string text,Vector2 at)
    {var obj=new GameObject("路标 · "+text,typeof(RectTransform),typeof(Canvas));obj.transform.SetParent(root);obj.transform.position=at;obj.transform.localScale=Vector3.one*.015f;var canvas=obj.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=15;var rect=obj.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(500,45);var t=GameplayUIFactory.Label("Text",obj.transform,Vector2.zero,Vector2.one,font,20,text,TextAnchor.MiddleCenter);t.color=PlayerUIStyle.Mint;t.raycastTarget=false;}
    private static void Chest(int chapter,float x,float top)
    {var obj=Item("支路冷库 "+x,"supply_crate",new Vector2(x,top),new Vector2(.9f,1));var chest=obj.AddComponent<GeneChest>();chest.offerChoice=true;var so=new SerializedObject(chest);var pool=so.FindProperty("genePool");pool.arraySize=3;var keys=new[]{"split","bounce","vortex"};for(int i=0;i<3;i++)pool.GetArrayElementAtIndex(i).objectReferenceValue=Resources.Load<Modifier>("SO/Genes/"+keys[i]);so.ApplyModifiedPropertiesWithoutUndo();obj.AddComponent<CompleteRewardState>().rewardId="campaign-chest-"+chapter+"-"+x;}
    private static EnemyAIController Enemy(int kind,float x,float y,float hp)
    {
        string dir=kind==4?"Support":kind>=6?"Ranged":"Melee";
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefab/Enemy/"+dir+"/Monster"+kind+".prefab"),root);
        obj.transform.position=new Vector2(x,y+.5f);obj.name="章节遭遇 Monster"+kind;
        var enemy=obj.GetComponent<EnemyAIController>();EnemyCombatPolishSetup.ConfigureSceneEnemy(enemy);
        var so=new SerializedObject(enemy);so.FindProperty("maxHp").floatValue=hp;so.FindProperty("meleeDamage").floatValue=1;so.FindProperty("bulletDamage").floatValue=1;so.FindProperty("detectRange").floatValue=5;so.FindProperty("patrolRange").floatValue=1;so.FindProperty("chaseSpeed").floatValue=1.8f;so.ApplyModifiedPropertiesWithoutUndo();
        if(kind==4){var aura=new SerializedObject(obj.GetComponent<EnemySupportAura>());aura.FindProperty("healRadius").floatValue=6;aura.ApplyModifiedPropertiesWithoutUndo();}
        return enemy;
    }
}
