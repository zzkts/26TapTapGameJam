using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]public sealed class ReservoirState{public string id;public float volume,pollution;}
[Serializable]public sealed class EcosystemState
{public List<ReservoirState> tanks=new();public float growth,stableSeconds,steamSeconds;public bool complete;}

/// <summary>Continuous supply, filtration and renewable power meet at the main pump.</summary>
public sealed class PumpLoopSystem:MonoBehaviour
{
    public WaterReservoir2D cistern,pool;
    public EcoFungus fungus;
    public EmergenceBody relay,boiler;
    public EmergencePlate bypass;
    public Text readout;
    public SpriteRenderer pipe,outflow;
    public float requiredStableSeconds=20;
    public bool Completed { get; private set; }
    public float StableSeconds { get; private set; }
    public float SteamSeconds { get; private set; }
    public float PipeRate { get; private set; }
    public bool Supply=>pool.Volume>=3;
    public bool Clean=>pool.Pollution<=.28f;
    public bool Power=>relay.Charged||SteamSeconds>0;
    public string PowerName=>relay.Charged?"水路导电":SteamSeconds>0?"相变蒸汽 · 剩余 "+SteamSeconds.ToString("0")+" 秒":"等待电流 / 蒸汽";
    public string Goal=>Completed?"主泵已恢复 · 清水持续送往右侧菌林":"主泵稳定输出清水 · "+StableSeconds.ToString("0")+" / 20 秒";
    private int steamCount;
    private float unstable,saveTimer;
    public void Restore(EcosystemState data)
    {
        if(data==null)return;
        foreach(var saved in data.tanks??new List<ReservoirState>())
        {
            var tank=saved.id==cistern.reservoirId?cistern:saved.id==pool.reservoirId?pool:null;
            if(tank!=null)tank.SetState(saved.volume,saved.pollution);
        }
        fungus.SetGrowth(data.growth);StableSeconds=Mathf.Clamp(data.stableSeconds,0,requiredStableSeconds);
        SteamSeconds=Mathf.Clamp(data.steamSeconds,0,30);Completed=data.complete;
    }
    public EcosystemState Capture()=>new(){tanks=new[]{cistern,pool}.Select(t=>new ReservoirState{id=t.reservoirId,volume=t.Volume,pollution=t.Pollution}).ToList(),growth=fungus.Growth,stableSeconds=StableSeconds,steamSeconds=SteamSeconds,complete=Completed};
    public bool Signal(int channel)=>channel==0?Supply:channel==1?Clean:Power;
    public string Status(int channel)=>channel==0?"水量 "+pool.Volume.ToString("0.0")+" / 至少 3":channel==1?"污染 "+(pool.Pollution*100).ToString("0")+"% / 不高于 28%":PowerName;
    private void Update()
    {
        if(CampaignFlow.Current==null||!CampaignFlow.Current.Ready)return;
        float dt=Time.deltaTime;if(dt<=0)return;
        cistern.Add(dt*.35f,.62f);
        PipeRate=(bypass.Pressed?.65f:.32f)*(fungus.Clogged?.4f:1);
        float pollution;float taken=cistern.Take(Mathf.Min(PipeRate*dt,pool.capacity-pool.Volume),out pollution);pool.Add(taken,pollution);
        if(boiler.SteamCount!=steamCount){SteamSeconds=Mathf.Min(30,SteamSeconds+24*(boiler.SteamCount-steamCount));steamCount=boiler.SteamCount;}
        SteamSeconds=Mathf.Max(0,SteamSeconds-dt);
        bool running=Supply&&Power;
        if(running){float ignored;pool.Take(dt*.18f,out ignored);}
        if(!Completed)
        {
            if(running&&Clean){unstable=0;StableSeconds+=dt;}
            else {unstable+=dt;if(unstable>1)StableSeconds=0;}
            if(StableSeconds>=requiredStableSeconds)
            {
                StableSeconds=requiredStableSeconds;Completed=true;
                CampaignUI.Current?.Toast("清水持续流出 · 向右探索菌林，观察清水如何改变下游");
                CampaignFlow.Current.player.Genes.Acquire(Resources.Load<Modifier>("SO/Genes/body_symbiosis"));
                EmergenceReactions.Emit("ecology",pool.transform.position,"供水 + 菌群净化 + 可持续动力 → 地下循环重新运行",3);
                StorySession.Current.SaveCheckpointState();
            }
        }
        if(readout!=null)readout.text="主泵 · "+(Completed?"生态循环已恢复":StableSeconds.ToString("0")+" / 20 秒")+"\n供水 "+(Supply?"●":"○")+" · 净水 "+(Clean?"●":"○")+" · 动力 "+(Power?"●":"○");
        if(pipe!=null)pipe.color=Color.Lerp(PlayerUIStyle.Mint,PlayerUIStyle.Violet,1-Mathf.Clamp01(PipeRate/.65f));
        if(outflow!=null){outflow.enabled=running;outflow.color=Color.Lerp(PlayerUIStyle.Mint,new Color(.64f,.87f,.32f),pool.Pollution);}
        saveTimer+=dt;if(saveTimer>=5){saveTimer=0;StorySession.Current.SaveCheckpointState();}
    }
}
