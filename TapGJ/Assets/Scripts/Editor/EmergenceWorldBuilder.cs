using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Adds systemic playgrounds between the original ecology route and the final core.</summary>
public static class EmergenceWorldBuilder
{
    private static Transform root;
    private static EmergenceProgress progress;
    [MenuItem("Tools/TapGJ/Emergence/Rebuild Expanded Chapter")]
    public static void Apply()=>CompleteGameWorldBuilder.Apply();
    public static void Expand(CompleteLevelFlow level,Camera camera)
    {
        root=level.transform;
        // Shift the authored ending as a unit, retaining its references and guardian setup.
        foreach(Transform group in root)
        {
            if(group.name.StartsWith("00"))continue;
            foreach(Transform item in group)if(item.position.x>=78f)item.position+=Vector3.right*150f;
        }
        level.secondRegionEnd=228f;
        level.guardianEntryX=234f;
        level.checkpoints=new[]{new Vector2(16,1),new Vector2(41,1),new Vector2(92,1),new Vector2(139,1),new Vector2(187,1),new Vector2(228,1)};
        progress=root.gameObject.AddComponent<EmergenceProgress>();
        CompleteGameWorldBuilder.AddGround("相变实验区",78,125,0);
        CompleteGameWorldBuilder.AddGround("菌落实验区",125,180,0);
        CompleteGameWorldBuilder.AddGround("回流实验区西",180,215,0);
        CompleteGameWorldBuilder.AddGround("回流裂缝东",217.4f,228,0);
        for(int i=6;i<12;i++)V("远方洞穴 "+i,"CavernBackdrop",new Vector2(i*24+12,3.2f),new Vector2(24.1f,13.5f),-40);
        for(int i=0;i<30;i++)
        {
            float x=80+i*5;
            V("实验区植物 "+i,i%3==0?"mushroom":"crystal",new Vector2(x,i%3==0?1.15f:.7f),new Vector2(1.2f,i%3==0?2.3f:1.4f),-8);
            if(i%3==0)V("循环管线 "+i,"pipe_horizontal",new Vector2(x,6.6f),new Vector2(15.2f,.55f),-6);
        }
        foreach(float x in new[]{92f,139f,187f})
        {
            GameObject terminal=CompleteGameWorldBuilder.AddItem("实验构筑台 "+x,"gene_terminal",new Vector2(x,0),new Vector2(1.4f,1.8f));
            terminal.AddComponent<GeneEditStation>().interactionRange=2;
            var trigger=new GameObject("实验区检查点 "+x);trigger.transform.SetParent(root);trigger.transform.position=new Vector2(x,1);
            var story=trigger.AddComponent<StoryTrigger>();story.beat=StoryBeat.Checkpoint;story.triggerId="experiment-checkpoint-"+x;
            var collider=trigger.GetComponent<BoxCollider2D>();collider.isTrigger=true;collider.size=new Vector2(2.6f,3);
        }
        DuctDistrict();SporeDistrict();InertiaDistrict();Rewards();Encounters();
        progress.coreGate=Gate("核心回流锁",222,0,5.5f,"gate-core");
        Sign("解开任意两条回流通路，再进入核心",new Vector2(220,4));
        foreach(GeneChest chest in root.GetComponentsInChildren<GeneChest>())chest.offerChoice=true;
        PlayerCameraFollow follow=camera.GetComponent<PlayerCameraFollow>();if(follow!=null)follow.horizontalLimits=new Vector2(9.6f,253);
    }
    private static void DuctDistrict()
    {
        Sign("03 / 相变与管网",new Vector2(83,4.8f));
        Sign("水流能接通断开的地方，也能改变压力",new Vector2(88,3.4f));
        var emitter=new GameObject("裸露电源");emitter.transform.SetParent(root);emitter.transform.position=new Vector2(84.2f,.5f);
        var source=emitter.AddComponent<EnvironmentElementSource>();source.element=E_ElementType.Electric;source.radius=1;source.interval=2.4f;
        V("电源指示","crystal",new Vector2(84.2f,1),new Vector2(.6f,1.2f),4).color=PlayerUIStyle.Gold;
        EmergenceGate sideGate=Gate("管网侧门",96,2.25f,2.2f,"gate-wire");
        for(int i=0;i<5;i++)
        {
            var body=Body("导体 "+i,"crystal",new Vector2(85+i*1.6f,.2f),new Vector2(.6f,.8f),EmergenceMaterial.Conductive,false);
            if(i==4){body.completionId="duct";body.chargedGate=sideGate;}
            if(i<4)V("断裂导线 "+i,"pipe_horizontal",new Vector2(85.8f+i*1.6f,.4f),new Vector2(.5f,.12f),3);
        }
        P("管网第一层",92.5f,.75f,2.8f);P("管网第二层",95,1.5f,2.8f);P("管网冷库平台",98,2.25f,4);
        EmergenceBody boiler=Body("蓄压罐","pipe_elbow",new Vector2(103,0),new Vector2(1.3f,1.2f),EmergenceMaterial.Boiler,true);
        var release=boiler.gameObject.AddComponent<EmergenceConsole>();release.kind=EmergenceConsole.ConsoleKind.PressureRelease;release.body=boiler;
        P("蒸汽观测平台",105.5f,4,4);
        GameObject calibration=CompleteGameWorldBuilder.AddItem("高处回流节点","core_gate",new Vector2(106,4),new Vector2(.9f,1));
        var node=calibration.AddComponent<EmergenceConsole>();node.kind=EmergenceConsole.ConsoleKind.Calibration;node.completionId="duct";node.gate=sideGate;
        Sign("上面还有一条路",new Vector2(106,5.7f));
        P("相变区支线",116,.75f,3);P("相变区补给台",119,1.5f,3);
    }
    private static void SporeDistrict()
    {
        Sign("04 / 会呼吸的菌落",new Vector2(130,4.8f));
        Sign("它们喝下水，也会改变身边的东西",new Vector2(145,3.8f));
        for(int i=0;i<4;i++)Body("连锁菌落 "+i,"mushroom",new Vector2(142+i*2.7f,0),new Vector2(1.1f,1.8f),EmergenceMaterial.Organic,false);
        EmergenceBody barrier=Body("脆弱隔板","metal_deck",new Vector2(153,0),new Vector2(.6f,2.1f),EmergenceMaterial.Fragile|EmergenceMaterial.Flammable,true);
        barrier.durability=8;barrier.completionId="spores";
        P("菌落观察一层",136,.75f,3);P("菌落观察二层",139.5f,1.5f,3);P("菌落观察廊",143.5f,2.25f,4);
        P("燃烧隔板绕行一层",157,.75f,3);P("菌落侧藏",160.5f,1.5f,3.2f);
        var alternative=CompleteGameWorldBuilder.AddItem("菌落标本台","gene_terminal",new Vector2(161,1.5f),new Vector2(.8f,1));
        var console=alternative.AddComponent<EmergenceConsole>();console.kind=EmergenceConsole.ConsoleKind.Calibration;console.completionId="spores";
        Sign("通路的变化，也是一种答案",new Vector2(163,3.3f));
        P("孢子高台",170,.75f,3);P("孢子冷库",173,1.5f,3.5f);
    }
    private static void InertiaDistrict()
    {
        Sign("05 / 惯性与回流",new Vector2(184,4.8f));
        Sign("重量来自哪里，机关并不在意",new Vector2(198,3.7f));
        for(int i=0;i<2;i++)
        {
            EmergenceBody crate=Body("可推动箱体 "+i,"supply_crate",new Vector2(192+i*1.4f,.05f),new Vector2(.8f,.8f),EmergenceMaterial.Movable|EmergenceMaterial.Flammable,true);
            Rigidbody2D rb=crate.gameObject.AddComponent<Rigidbody2D>();rb.mass=1.2f;rb.gravityScale=.65f;rb.freezeRotation=true;rb.linearDamping=1.5f;
        }
        var plateObject=new GameObject("重量阀板");plateObject.transform.SetParent(root);plateObject.transform.position=new Vector2(198,0);
        var plate=plateObject.AddComponent<EmergencePlate>();plate.visual=V("压板","metal_deck",new Vector2(198,.055f),new Vector2(1.8f,.11f),4);
        plate.gate=Gate("重量回路门",202,0,2.1f,"gate-factory");
        var leverObject=CompleteGameWorldBuilder.AddItem("止回控制台","core_gate",new Vector2(208,0),new Vector2(.9f,1.2f));
        var lever=leverObject.AddComponent<EmergenceConsole>();lever.kind=EmergenceConsole.ConsoleKind.BackflowSwitch;lever.completionId="factory";lever.gate=plate.gate;
        P("惯性绕行一层",197,.75f,2.8f);P("惯性绕行二层",199.8f,1.5f,2.8f);P("惯性绕行廊",203,2.25f,3.8f);
        P("回流箱体观察",211,.75f,3);P("回流奖励台",214,1.5f,3);
    }
    private static void Rewards()
    {
        CompleteGameWorldBuilder.AddChest("第一次构筑选择",new Vector2(19.8f,0),"ember","split","water");
        CompleteGameWorldBuilder.AddChest("管网样本三选一",new Vector2(98,2.25f),"bounce","pierce","speed");
        CompleteGameWorldBuilder.AddChest("孢子样本三选一",new Vector2(173,1.5f),"water","ember","sewage");
        CompleteGameWorldBuilder.AddChest("惯性样本三选一",new Vector2(214,1.5f),"orbit","size","vortex");
        foreach(float x in new[]{100f,160f,205f})
        {
            GameObject obj=CompleteGameWorldBuilder.AddItem("实验恢复箱 "+x,"supply_crate",new Vector2(x,0),new Vector2(.8f,.8f));
            var heal=obj.AddComponent<PlayerSupply>();heal.kind=PlayerSupply.SupplyKind.Health;heal.healAmount=5;
            obj.AddComponent<CompleteRewardState>().rewardId="experiment-health-"+x;
        }
        GameObject backup=CompleteGameWorldBuilder.AddItem("火种补充样本","supply_crate",new Vector2(80,.15f),new Vector2(.6f,.75f));
        backup.AddComponent<CompleteGeneSample>().gene=Resources.Load<Modifier>("SO/Genes/ember");
        backup.AddComponent<CompleteRewardState>().rewardId="experiment-fire-backup";
    }
    private static void Encounters()
    {
        foreach(float x in new[]{111f,124f,133f,165f,176f,191f,211f})CompleteGameWorldBuilder.AddEnemy(3,x,.5f,7,2);
        foreach(float x in new[]{119f,155f,184f,219f})CompleteGameWorldBuilder.AddEnemy(6,x,2.7f,6,1.2f);
        CompleteGameWorldBuilder.AddEnemy(5,147,.5f,9,2);
        CompleteGameWorldBuilder.AddEnemy(4,167,.5f,8,1);
    }
    private static EmergenceBody Body(string name,string key,Vector2 position,Vector2 size,EmergenceMaterial flags,bool solid)
    {
        var obj=CompleteGameWorldBuilder.AddItem(name,key,position,size);obj.layer=LayerMask.NameToLayer("Ground");
        var box=obj.AddComponent<BoxCollider2D>();box.size=new Vector2(size.x*.8f,size.y*.8f);box.offset=new Vector2(0,size.y*.4f);box.isTrigger=!solid;
        var body=obj.AddComponent<EmergenceBody>();body.material=flags;return body;
    }
    private static EmergenceGate Gate(string name,float x,float bottom,float height,string id)
    {
        var obj=new GameObject(name);obj.transform.SetParent(root);obj.transform.position=new Vector2(x,bottom);obj.layer=LayerMask.NameToLayer("Wall");
        var gate=obj.AddComponent<EmergenceGate>();gate.persistentId=id;
        var collider=obj.AddComponent<BoxCollider2D>();collider.size=new Vector2(.55f,height);collider.offset=Vector2.up*height*.5f;gate.barrier=collider;
        gate.artwork=CompleteGameWorldBuilder.AddVisual(name+"面板",obj.transform,"pipe_horizontal",new Vector2(x,bottom+height*.5f),new Vector2(.6f,height),4);return gate;
    }
    private static void P(string name,float x,float y,float width)=>CompleteGameWorldBuilder.AddPlatform(name,x,y,width);
    private static void Sign(string text,Vector2 at)=>CompleteGameWorldBuilder.AddSign(text,at);
    private static SpriteRenderer V(string name,string key,Vector2 at,Vector2 size,int order)=>CompleteGameWorldBuilder.AddVisual(name,root,key,at,size,order);
}
