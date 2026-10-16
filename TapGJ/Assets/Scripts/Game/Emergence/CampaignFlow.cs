using System.Linq;
using UnityEngine;

/// <summary>Three visual districts in one scene. Progress depends on combat, never ecology puzzles.</summary>
public sealed class CampaignFlow : MonoBehaviour
{
    public static CampaignFlow Current { get; private set; }
    public int chapter=1;
    public bool unified=true;
    public float worldWidth=144;
    public CampaignExit exit;
    public CompleteLevelFlow level;
    public PlayerController player;
    public bool Ready { get; private set; }
    public string ChapterName=>Name(chapter);
    public static string Name(int n)=>n==1?"第一章 · 雨后泵站":n==2?"第二章 · 共生菌林":"第三章 · 净化核心";
    public bool NavigatingGuardians=>chapter==3&&level.BattleStarted&&!level.CoreDefeated;
    public string Goal=>chapter<3?"打怪 / 开冷库获取基因 → 终端装配 → 向右探索":
        level.CoreDefeated?"核心守卫已击败 · 到最右端回流阀按 E 选择结局":
        level.BattleStarted?"击败核心守卫 · 剩余 "+level.guardians.Count(g=>g!=null&&!g.IsDead)+" / 3":
        "在基因终端准备构筑 · 向右挑战三名核心守卫";
    public Vector2 NavigationPosition=>chapter<3?new Vector2(chapter*48+2,1):
        NavigatingGuardians?level.guardians.Where(g=>g!=null&&!g.IsDead).OrderBy(g=>Vector2.Distance(g.transform.position,player.transform.position))
            .Select(g=>(Vector2)g.transform.position).DefaultIfEmpty(exit.transform.position).First():(Vector2)exit.transform.position;
    private void Awake(){Current=this;}
    public void Begin(RunSaveData saved)
    {
        if(player==null)player=StorySession.Current.player;
        player.UnlockDash();
        chapter=DistrictAt(player.transform.position.x);Ready=true;CampaignUI.Ensure(this);Announce();
    }
    public static int DistrictAt(float x)=>x<48?1:x<96?2:3;
    private void Update()
    {
        if(!Ready||player==null||player.IsDead||player.MenuOpen)return;
        int next=DistrictAt(player.transform.position.x);
        if(next!=chapter){chapter=next;Announce();}
        if(player.transform.position.x>=level.guardianEntryX&&!level.CoreDefeated&&!level.BattleStarted)
        {level.BeginGuardianBattle();CampaignUI.Current?.Toast("核心守卫出现 · 击败三名守卫后，到最右端按 E 选择结局");}
    }
    private void Announce()
    {
        ChapterArrivalPanel.Show(ChapterName,chapter==1?"击败敌人或开启冷库收集基因 · 在终端按 E 装配":
            chapter==2?"继续收集与重组基因 · 改造持续生效，右键依序启动属性":
            "准备构筑，击败三名核心守卫 · 最右端回流阀是终点");
        string key="gene-route:arrival:"+chapter;
        if(StorySession.Current.WasTriggerCompleted(key))return;
        StorySession.Current.RegisterTriggerComplete(key);
        if(chapter>1)StorySession.Current.SaveCheckpoint(new Vector2((chapter-1)*48+2,1));
    }
    public void Advance(){CampaignUI.Current?.Toast("沿主路向右探索 · 到达下一区域会自动提示");}
    public StoryLine[] Introduction()=>new[]{new StoryLine("地下实验记录","从敌人与冷库收集基因，在终端重组。改造持续生效，右键按槽位顺序依次启动属性。")};
    private void OnDestroy(){if(Current==this)Current=null;}
}
