using System.Collections.Generic;
using UnityEngine;

/// <summary>Upstream output changes downstream habitat; nearby varied reactions feed the player's energy.</summary>
public sealed class WatershedNetwork:MonoBehaviour
{
    public CampaignFlow flow;
    public EmergenceBody forest;
    public GameObject forestBridge;
    public bool DownstreamFlow { get; private set; }
    public int ResonanceBursts { get; private set; }
    public int Variety=>variety.Count;
    public string Status=>"下游清水 "+(DownstreamFlow?"●":"○")+" · 反应接力 "+variety.Count+" / 3 · 12 秒内三种反应返还能量与生命";
    private readonly HashSet<string> variety=new();
    private float nextWater,windowEnd;
    private void OnEnable(){EmergenceReactions.Happened+=OnReaction;}
    private void OnDisable(){EmergenceReactions.Happened-=OnReaction;}
    private void Update()
    {
        if(flow==null||!flow.Ready||Time.deltaTime<=0)return;
        if(variety.Count>0&&Time.time>windowEnd)variety.Clear();
        var pump=flow.ecosystem;
        DownstreamFlow=pump.Supply&&pump.Clean&&pump.Power;
        if(DownstreamFlow&&Time.time>=nextWater)
        {
            nextWater=Time.time+8;
            // A running upstream pump irrigates the real downstream material. No second puzzle flag.
            if(forest!=null)forest.Receive(E_ElementType.Water);
        }
        bool bridge=forest!=null&&forest.Bloomed;
        if(forestBridge!=null&&forestBridge.activeSelf!=bridge)forestBridge.SetActive(bridge);
    }
    private void OnReaction(EmergenceEvent reaction)
    {
        if(flow==null||!flow.Ready||flow.player.IsDead||flow.player.MenuOpen||Time.timeScale<=0||Vector2.Distance(flow.player.transform.position,reaction.position)>7)return;
        if(reaction.kind=="ecology")return;
        if(Time.time>windowEnd)variety.Clear();
        if(variety.Count==0)windowEnd=Time.time+12;
        variety.Add(reaction.kind);
        if(variety.Count<3)return;
        variety.Clear();ResonanceBursts++;
        flow.player.GetComponent<PlayerSynthesis>().Recharge(1.5f);flow.player.Heal(1);
        CampaignUI.Current?.Toast("涌现接力 · 三种反应相遇！回流能量 +1.5，生命 +1");
        TapGJ.WaterProjectiles.WaterVfx.SpawnSplash(flow.player.transform.position,PlayerUIStyle.Mint,1.2f,Vector2.up);
    }
}
