using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Stores discoveries and route changes in the existing checkpoint transaction.</summary>
public sealed class EmergenceProgress:MonoBehaviour
{
    public static EmergenceProgress Current { get; private set; }
    public EmergenceGate coreGate;
    public float startX=78f,endX=228f;
    public int CompletionCount=>(IsComplete("duct")?1:0)+(IsComplete("spores")?1:0)+(IsComplete("factory")?1:0);
    public readonly HashSet<string> Completed=new();
    public readonly Dictionary<string,string> Discoveries=new();
    public string LastTrace { get; private set; }="试着让水流改变这里";
    public string DistrictName=>PlayerX<125f?"相变与管网":PlayerX<180f?"会呼吸的菌落":"惯性与回流";
    private float PlayerX=>StorySession.Current!=null&&StorySession.Current.player!=null?StorySession.Current.player.transform.position.x:0f;
    public bool InExperimentDistrict=>PlayerX>=startX&&PlayerX<endX;
    public bool IsComplete(string key)=>!string.IsNullOrEmpty(key)&&Completed.Contains(key);
    private void Awake(){Current=this;EmergenceReactions.Happened+=OnReaction;}
    private IEnumerator Start()
    {
        while(StorySession.Current==null||!StorySession.Current.Ready)yield return null;
        foreach(string key in new[]{"duct","spores","factory","gate-wire","gate-core","gate-factory"})
            if(StorySession.Current.WasTriggerCompleted("emergence:"+key))Completed.Add(key);
        foreach(string kind in new[]{"conduction","steam","lift","growth","explosion","fracture","inertia"})
            if(StorySession.Current.WasTriggerCompleted("discovery:"+kind))Discoveries[kind]=Description(kind);
        foreach(EmergenceGate gate in GetComponentsInChildren<EmergenceGate>(true))
            if(IsComplete(gate.persistentId))gate.OpenPermanently();
        foreach(EmergenceBody body in GetComponentsInChildren<EmergenceBody>(true))
            if(body.Has(EmergenceMaterial.Fragile)&&StorySession.Current.WasTriggerCompleted("structure:"+body.completionId))body.RestoreBroken();
        if(CompletionCount>=2||StorySession.Current.GuardianCleared)coreGate?.OpenPermanently();
        EmergenceUI.Ensure(this);
        if(RunProgress.ExperimentMode)ApplyVariation();
    }
    public void Complete(string key)
    {
        if(string.IsNullOrEmpty(key)||!Completed.Add(key))return;
        StorySession.Current?.RegisterTriggerComplete("emergence:"+key);
        if(CompletionCount>=2)coreGate?.OpenPermanently();
        LastTrace="通路发生变化 · 回流锁 "+CompletionCount+" / 2";
    }
    private void OnReaction(EmergenceEvent reaction)
    {
        if(reaction.kind=="conduction"&&reaction.count<2)return;
        if(!Discoveries.ContainsKey(reaction.kind))
        {
            Discoveries[reaction.kind]=Description(reaction.kind);
            StorySession.Current?.RegisterTriggerComplete("discovery:"+reaction.kind);
        }
        LastTrace=reaction.trace+(reaction.count>1?" · "+reaction.count+" 次连锁":"");
    }
    public static string Description(string kind)=>kind switch
    {
        "conduction"=>"导电：水渍延长导体之间的连接，电源也会伤到湿润的生物。",
        "steam"=>"相变：水分与热量产生蒸汽。蓄水压力也可以通过阀门释放。",
        "lift"=>"推力：同一股蒸汽能抬起人物，也能抬起箱子。",
        "growth"=>"生长：菌落遇水释放孢子，改变附近的环境。",
        "explosion"=>"连锁：火焰遇毒雾会爆炸，相邻菌落能将反应传递下去。",
        "fracture"=>"破坏：爆炸、燃烧和持续冲击可以破坏脆弱结构。",
        "inertia"=>"惯性：水球推动箱子；旋涡、人物和敌人也能改变机关上的重量。",
        _=>"新的变化正在发生。"
    };
    private void ApplyVariation()
    {
        int variation=RunProgress.RunSeed%3;
        foreach(EnvironmentElementSource source in GetComponentsInChildren<EnvironmentElementSource>())
            source.interval=variation==0?1.1f:2.4f;
        foreach(EmergenceBody body in GetComponentsInChildren<EmergenceBody>())
            if(body.Has(EmergenceMaterial.Organic))body.growthCooldown=variation==1?9f:18f;
        foreach(EnemyAIController enemy in GetComponentsInChildren<EnemyAIController>(true))
        {
            // Scene-owned enemy instances, never their shared prefab assets.
            if(enemy.transform.position.x>=startX&&enemy.transform.position.x<endX)
                enemy.gameObject.AddComponent<EmergenceTrialEnemy>().Configure(variation);
        }
        string path=variation==0?"Prefab/Enemy/Ranged/Monster7":variation==1?"Prefab/Enemy/Support/Monster4":"Prefab/Enemy/Melee/Monster3";
        GameObject prefab=Resources.Load<GameObject>(path);
        if(prefab!=null)for(int i=0;i<3;i++)
        {
            GameObject enemy=Instantiate(prefab,new Vector3(112f+i*50f,variation==0?2.3f:.5f,0),Quaternion.identity,transform);enemy.name="试验变体遭遇 "+i;
            enemy.AddComponent<EmergenceTrialEnemy>().Configure(variation);
        }
        LastTrace="回流试验 #"+RunProgress.RunSeed+" · "+TrialName(variation);
    }
    public static string TrialName(int variation)=>variation==0?"高频电路":variation==1?"旺盛菌落":"追猎回流";
    private void OnDestroy(){EmergenceReactions.Happened-=OnReaction;if(Current==this)Current=null;}
}
