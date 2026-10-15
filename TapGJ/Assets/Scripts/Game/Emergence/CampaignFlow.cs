using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>Three compact hub-and-branch chapters, with explicit chapter exits and final victory.</summary>
public sealed class CampaignFlow:MonoBehaviour
{
    public static CampaignFlow Current { get; private set; }
    public int chapter=1;
    public CampaignReactor[] reactors;
    public CampaignExit exit;
    public CompleteLevelFlow level;
    public PlayerController player;
    public bool Ready { get; private set; }
    public int Powered=>reactors.Count(r=>r.Complete);
    public bool ChapterSolved=>Powered>=2;
    public string ChapterName=>chapter==1?"第一章 · 雨后泵站":chapter==2?"第二章 · 共生菌林":"第三章 · 净化核心";
    public string Goal=>chapter<3?(ChapterSolved?"目标完成 · 回到中央出口，E 进入下一章":"用反应接通任意两座节点 · "+Powered+" / 2"):
        level.CoreDefeated?"胜利条件已达成 · 回中央阀门，E 选择水流的归处":level.BattleStarted?"击败三名核心守卫 · 剩余 "+level.guardians.Count(g=>g!=null&&!g.IsDead):"接通任意两座核心节点，唤醒守卫 · "+Powered+" / 2";
    private void Awake(){Current=this;}
    public void Begin(RunSaveData saved)
    {
        if(player==null)player=StorySession.Current.player;
        if(player.GetComponent<PlayerSynthesis>()==null)player.gameObject.AddComponent<PlayerSynthesis>();
        player.UnlockDash();
        if(player.Genes.GenInventory.Count==0)
            foreach(string gene in new[]{"water","ember","sewage"})player.Genes.Acquire(Resources.Load<Modifier>("SO/Genes/"+gene));
        foreach(var node in reactors)node.Restore(StorySession.Current.WasTriggerCompleted(node.SaveId));
        Ready=true;CampaignUI.Ensure(this);
        string entered="campaign:entered:"+chapter;
        if(!StorySession.Current.WasTriggerCompleted(entered))
        {
            StorySession.Current.RegisterTriggerComplete(entered);
            StorySession.Current.Tell(Introduction());
        }
        if(chapter==3&&ChapterSolved&&!level.CoreDefeated)level.BeginGuardianBattle();
    }
    public void CompleteReactor(CampaignReactor node)
    {
        if(!Ready||node.Complete)return;
        node.Restore(true);StorySession.Current.RegisterTriggerComplete(node.SaveId);
        Modifier reward=Resources.Load<Modifier>("SO/Genes/"+node.reward);
        if(reward!=null)player.Genes.Acquire(reward);player.Heal(1);
        CampaignUI.Current?.Toast(node.displayName+"已接通 · "+Powered+" / 3"+(ChapterSolved?(chapter==3?" · 守卫已唤醒，击败后回阀门":" · 中央出口可用"):" · 接通两座即可前进"));
        if(chapter==3&&ChapterSolved&&!level.BattleStarted)level.BeginGuardianBattle();
        StorySession.Current.SaveCheckpointState();
    }
    public void Advance()
    {
        if(chapter>=3||!ChapterSolved)return;
        RunSaveData data=StorySession.Current.Capture();data.scene=chapter==1?"SporeScene":"CoreScene";
        data.checkpointX=17;data.checkpointY=1;data.guardianCleared=false;data.region=0;
        RunProgress.Write(data);player.SetMenuOpen(false);RunProgress.ContinueRun();
    }
    public StoryLine[] Introduction()
    {
        if(chapter==1)return new[]{new StoryLine("雨后的泵站","水路断成了几段。这里没有唯一的开关：你可以给泵站加热，也可以让重量和水团接入回路。"),new StoryLine("本章胜利条件","左侧蓄压罐、右侧重量阀、高处导电节点，任选两个接通。然后回到中央发光出口，按 E 进入菌林。按 M 查看地图与各节点方法。"),new StoryLine("地漏的新能力","Q 切换水 / 火 / 孢子种；R 播种，T 回收；F 推流，S+F 吸回湿润种子与箱体。空中再按空格可踏跃一次，落地恢复。",true)};
        if(chapter==2)return new[]{new StoryLine("共生菌林","水让菌落醒来，菌落又把变化传给邻居。你带来的小水团，可以成为这条连锁的一部分。"),new StoryLine("本章胜利条件","让左侧菌落生长、右侧连锁击破隔板，或接通高处导体，任选两项。完成后回中央出口进入核心；支路顺序由你决定。"),new StoryLine("组合试验","孢子种 + 火种可以爆破；水种 + 火种可以产生蒸汽登高。治疗怪会救助同伴，优先打断它，或用连锁同时清场。")};
        return new[]{new StoryLine("净化核心","核心在等待两个回流节点重新接通。你学到的反应，将决定这里的水路如何重组。"),new StoryLine("最终胜利条件","先接通任意两座节点，再击败出现的三名守卫。最后回到中央阀门按 E，选择一个结局，才完成整个游戏。"),new StoryLine("地漏","不是谁给了我一把钥匙。是这些相遇，让原本走不通的地方有了新的路。",true)};
    }
    private void OnDestroy(){if(Current==this)Current=null;}
}

