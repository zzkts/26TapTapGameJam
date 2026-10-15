using System.Linq;
using UnityEngine;

/// <summary>A single watershed: district changes never reload the world or discard reactions.</summary>
public sealed class CampaignFlow:MonoBehaviour
{
    public static CampaignFlow Current { get; private set; }
    public int chapter=1;
    public bool unified;
    public float worldWidth=144;
    public CampaignReactor[] reactors;
    public CampaignExit exit;
    public CompleteLevelFlow level;
    public PlayerController player;
    public PumpLoopSystem ecosystem;
    public bool Ready { get; private set; }
    public CampaignReactor[] DistrictNodes=>reactors.Where(n=>n.district==chapter).ToArray();
    public int Powered=>DistrictNodes.Count(r=>r.Complete);
    public bool DistrictSolved(int district)=>district==1?ecosystem!=null&&ecosystem.Completed:reactors.Count(n=>n.district==district&&n.Complete)>=2;
    public bool ChapterSolved=>DistrictSolved(chapter);
    public bool WatershedSolved=>DistrictSolved(1)&&DistrictSolved(2)&&DistrictSolved(3);
    public string ChapterName=>Name(chapter);
    public static string Name(int n)=>n==1?"第一章 · 雨后泵站":n==2?"第二章 · 共生菌林":"第三章 · 净化核心";
    public string Goal=>chapter==1?(ChapterSolved?"清水已送往下游 · 向右进入共生菌林":ecosystem.Goal):chapter==2?(ChapterSolved?"菌林回流已接通 · 向右前往净化核心":"浇醒母菌 / 连锁破壳 / 湿种导电，任选两项 · "+Powered+" / 2"):
        !WatershedSolved?"水网进度：泵站 "+(DistrictSolved(1)?"●":"○")+" 菌林 "+(DistrictSolved(2)?"●":"○")+" 核心 "+(DistrictSolved(3)?"●":"○")+" · M 定位未完成目标":
        level.CoreDefeated?"胜利条件已达成 · 最右侧回流阀按 E 选择结局":level.BattleStarted?"击败三名核心守卫 · 剩余 "+level.guardians.Count(g=>g!=null&&!g.IsDead):"水网已恢复 · 前往核心守卫";
    public CampaignReactor NavigationTarget
    {
        get
        {
            int district=chapter;
            if(chapter==3&&!WatershedSolved)district=!DistrictSolved(1)?1:!DistrictSolved(2)?2:3;
            if(DistrictSolved(district))return null;
            return reactors.Where(n=>n.district==district&&!n.Complete).OrderBy(n=>Vector2.Distance(n.transform.position,player.transform.position)).FirstOrDefault()??(district==1?reactors.First(n=>n.district==1&&n.channel==2):null);
        }
    }
    public bool NavigatingGuardians=>chapter==3&&WatershedSolved&&level.BattleStarted&&!level.CoreDefeated;
    public Vector2 NavigationPosition=>NavigationTarget!=null?(Vector2)NavigationTarget.transform.position:chapter<3?new Vector2(chapter*48+2,1):NavigatingGuardians?(Vector2)level.guardians.Where(g=>g!=null&&!g.IsDead).OrderBy(g=>Vector2.Distance(g.transform.position,player.transform.position)).Select(g=>g.transform.position).DefaultIfEmpty(exit.transform.position).First():(Vector2)exit.transform.position;
    private void Awake(){Current=this;}
    public void Begin(RunSaveData saved)
    {
        if(player==null)player=StorySession.Current.player;
        if(player.GetComponent<PlayerSynthesis>()==null)player.gameObject.AddComponent<PlayerSynthesis>();
        var reservoir=player.GetComponent<PlayerReservoir>();if(reservoir==null)reservoir=player.gameObject.AddComponent<PlayerReservoir>();
        if(saved!=null)reservoir.Restore(saved.carriedWater,saved.carriedPollution);
        player.UnlockDash();
        if(player.Genes.GenInventory.Count==0)
            foreach(string gene in new[]{"water","ember","sewage"})player.Genes.Acquire(Resources.Load<Modifier>("SO/Genes/"+gene));
        foreach(var node in reactors)node.Restore(StorySession.Current.WasTriggerCompleted(node.SaveId));
        ecosystem.Restore(saved?.ecosystem);
        foreach(string key in new[]{"body_cling","body_jet","body_symbiosis"})
            if(!player.Genes.GenInventory.Any(g=>g!=null&&g.Definition.name==key))player.Genes.Acquire(Resources.Load<Modifier>("SO/Genes/"+key));
        chapter=DistrictAt(player.transform.position.x);Ready=true;CampaignUI.Ensure(this);Announce();
    }
    public static int DistrictAt(float x)=>x<48?1:x<96?2:3;
    private void Update()
    {
        if(!Ready||player==null||player.IsDead)return;
        int next=DistrictAt(player.transform.position.x);
        if(next!=chapter){chapter=next;Announce();}
        if(chapter==3&&WatershedSolved&&!level.CoreDefeated&&!level.BattleStarted)
        {level.BeginGuardianBattle();CampaignUI.Current?.Toast("整条水网已连通 · 三名守卫苏醒 · 击败后到最右端按 E");}
    }
    private void Announce()
    {
        CampaignUI.Current?.SelectDistrict();
        string key="watershed:arrival:"+chapter;
        ChapterArrivalPanel.Show(ChapterName,chapter==1?"从最左侧出发 · 供水 + 净化 + 动力，输出清水 20 秒":"第 "+chapter+" 区域已到达 · "+(chapter==2?"任选两项反应接通菌林，清水会唤醒菌冠捷径":"接通任意两座核心节点，恢复水网后击败守卫"));
        if(StorySession.Current.WasTriggerCompleted(key))return;
        StorySession.Current.RegisterTriggerComplete(key);
        if(chapter>1)StorySession.Current.SaveCheckpoint(new Vector2((chapter-1)*48+2,1));
    }
    public void CompleteReactor(CampaignReactor node)
    {
        if(!Ready||node.Complete)return;
        node.Restore(true);StorySession.Current.RegisterTriggerComplete(node.SaveId);GameAudioPresentation.PlayCue("node",.85f);
        Modifier reward=Resources.Load<Modifier>("SO/Genes/"+node.reward);
        if(reward!=null)player.Genes.Acquire(reward);player.Heal(1);
        int count=reactors.Count(n=>n.district==node.district&&n.Complete);
        CampaignUI.Current?.Toast(node.displayName+"已接通 · "+count+" / 2"+(count>=2?" · 向右继续探索":" · 再接通任意一项"));
        StorySession.Current.SaveCheckpointState();
    }
    // Kept for old UI callers: travel is always continuous within GameScene.
    public void Advance(){CampaignUI.Current?.Close();CampaignUI.Current?.Toast("无需切换场景 · 沿主路向右前进即可进入下一区域");}
    public StoryLine[] Introduction()=>new[]{new StoryLine("地下水网","把同一份水用于供水、菌群、蒸汽和导电。改变一个地方，观察变化如何传到下游。")};
    private void OnDestroy(){if(Current==this)Current=null;}
}
