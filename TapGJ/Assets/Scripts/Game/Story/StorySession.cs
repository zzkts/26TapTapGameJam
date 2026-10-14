using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Owns the short chapter's dialogue, objectives, final decision and checkpoint lifecycle.</summary>
public sealed class StorySession : MonoBehaviour
{
    public static StorySession Current { get; private set; }
    public PlayerController player;
    public StoryDialogPanel dialogue;
    public StoryObjectivePanel objective;
    public CompleteLevelFlow level;
    public bool Ready { get; private set; }
    public bool IsModal { get; private set; }
    public bool ValidationMode { get; private set; }
    public bool GuardianCleared { get; private set; }
    public int StoryFlags => flags;
    public int Region => region;
    public float ElapsedSeconds => elapsed;
    public Vector2 Checkpoint => checkpoint;
    private int flags,region;
    private float elapsed,objectiveTimer,checkpointNoticeUntil;
    private Vector2 checkpoint = new Vector2(3f,1f);
    private bool completed,restoring;
    private readonly HashSet<string> completedTriggers = new HashSet<string>();
    private readonly Queue<DialogueRequest> pending = new Queue<DialogueRequest>();
    private sealed class DialogueRequest { public StoryLine[] lines; public Action complete; }

    private void Awake()
    {
        if(Current!=null&&Current!=this){enabled=false;return;}
        Current=this;
    }
    private IEnumerator Start()
    {
        yield return null;
        if(player==null)player=FindFirstObjectByType<PlayerController>();
        if(player==null){Debug.LogWarning("StorySession：当前场景没有玩家。");yield break;}
        if(level==null)level=FindFirstObjectByType<CompleteLevelFlow>();
        Canvas storyCanvas=StoryUIFactory.FindGameplayCanvas();
        Canvas dialogCanvas=dialogue!=null?dialogue.GetComponentInParent<Canvas>():null;
        if(dialogue==null||dialogCanvas==null||dialogCanvas.rootCanvas.renderMode==RenderMode.WorldSpace)
        {
            dialogue=StoryUIFactory.Ensure(storyCanvas);
        }
        storyCanvas=dialogue!=null?dialogue.GetComponentInParent<Canvas>():storyCanvas;
        if(storyCanvas!=null)objective=storyCanvas.GetComponentInChildren<StoryObjectivePanel>(true);
        restoring=true;
        RunSaveData saved=ValidationMode?null:RunProgress.EnterScene();
        if(saved!=null)
        {
            flags=saved.storyFlags;elapsed=saved.elapsedSeconds;
            GuardianCleared=saved.guardianCleared;checkpoint=new Vector2(saved.checkpointX,saved.checkpointY);
            foreach(string key in saved.completedTriggers)completedTriggers.Add(key);
            // A failed encounter always starts afresh from the last safe terminal.
            if(!GuardianCleared)flags&=~(1<<(int)StoryBeat.GuardianGate);
            RunProgress.RestorePlayer(player,saved);
            // Death and Continue return to a terminal, which may be in an earlier region.
            // Recompute location rather than showing the objective from where the run ended.
            region=level!=null?(checkpoint.x<level.firstRegionEnd?0:checkpoint.x<level.secondRegionEnd?1:2):saved.region;
            if(level!=null){level.SetCheckpoint(checkpoint);level.RestoreCoreCleared(GuardianCleared);}
            checkpointNoticeUntil=Time.unscaledTime+4f;
        }
        else
        {
            checkpoint=level!=null?level.spawn:(Vector2)player.transform.position;
            region=level!=null?Mathf.Max(0,level.Region):0;
        }
        restoring=false;Ready=true;
        if(player.Genes!=null){player.Genes.Acquired+=OnGeneAcquired;player.Genes.Changed+=OnInventoryChanged;}
        if(level!=null){level.CoreCleared+=NotifyGuardiansDefeated;level.ExitReached+=ShowCoreChoice;level.RegionEntered+=OnRegionEntered;}
        if(saved==null)Reach(StoryBeat.Entrance,player.transform.position);
        else if(region>0)OnRegionEntered(region);
        RefreshObjective();
    }

    private void Update()
    {
        if(!Ready||player==null)return;
        if(!completed&&!player.IsDead&&!player.MenuOpen)elapsed+=Time.deltaTime;
        if(player.DashUnlocked&&!Has(StoryBeat.Dash))Reach(StoryBeat.Dash,player.transform.position);
        if(pending.Count>0&&!IsModal&&!player.MenuOpen&&!player.IsDead)PlayNext();
        objectiveTimer-=Time.unscaledDeltaTime;
        if(objectiveTimer<=0f){objectiveTimer=.25f;RefreshObjective();}
    }

    public bool Has(StoryBeat beat)=> (flags&(1<<(int)beat))!=0;
    public bool WasTriggerCompleted(string key)=>!string.IsNullOrEmpty(key)&&completedTriggers.Contains(key);
    public void RegisterTriggerComplete(string key)
    {
        if(string.IsNullOrEmpty(key))return;
        completedTriggers.Add(key);SaveCheckpointState();
    }
    public bool Reach(StoryBeat beat,Vector2 position,bool allowReplay=false)
    {
        if(!Ready||player==null||player.IsDead||completed)return false;
        if(beat==StoryBeat.Checkpoint){SaveCheckpoint(position);return true;}
        if(beat==StoryBeat.Core){ShowCoreChoice();return GuardianCleared;}
        if(beat==StoryBeat.GuardianGate&&GuardianCleared)return false;
        bool seen=Has(beat);
        if(seen&&!allowReplay)
        {
            if(beat==StoryBeat.GuardianGate){BeginGuardianBattle();return true;}
            return false;
        }
        flags|=1<<(int)beat;
        Action end=beat==StoryBeat.GuardianGate?(Action)BeginGuardianBattle:SaveCheckpointState;
        Enqueue(StoryContent.Lines(beat),end);RefreshObjective();return true;
    }
    private void OnGeneAcquired(Modifier gene)
    {
        if(restoring||gene==null||!gene.IsAttribute)return;
        StoryBeat beat=gene.Element==E_ElementType.Fire?StoryBeat.SampleFire:
            gene.Element==E_ElementType.Poison?StoryBeat.SamplePoison:StoryBeat.SampleWater;
        Reach(beat,player.transform.position);
    }
    private void OnInventoryChanged(){if(!restoring)SaveCheckpointState();RefreshObjective();}
    private void OnRegionEntered(int next)
    {
        region=Mathf.Clamp(next,0,2);
        if(region==1)Reach(StoryBeat.Ecology,player.transform.position);
        if(region==2)Reach(StoryBeat.CoreArea,player.transform.position);
        RefreshObjective();
    }
    public void BeginGuardianBattle()
    {
        if(GuardianCleared)return;
        level?.BeginGuardianBattle();SaveCheckpointState();RefreshObjective();
    }
    public void NotifyGuardiansDefeated()
    {
        if(GuardianCleared)return;
        GuardianCleared=true;flags|=1<<(int)StoryBeat.GuardianCleared;
        SaveCheckpointState();Enqueue(StoryContent.Lines(StoryBeat.GuardianCleared),SaveCheckpointState);RefreshObjective();
    }
    public void ShowCoreChoice()
    {
        if(!Ready||!GuardianCleared||completed||IsModal)return;
        flags|=1<<(int)StoryBeat.Core;
        if(ValidationMode)return;
        Enqueue(StoryContent.Lines(StoryBeat.Core),PresentChoice);
    }
    private void PresentChoice()
    {
        SaveCheckpointState();IsModal=true;player.SetMenuOpen(true);dialogue?.ShowChoices(ChooseEnding);
    }
    public void ChooseEnding(bool coexistence)
    {
        if(completed||!GuardianCleared)return;
        completed=true;RunSaveData data=Capture();
        if(!ValidationMode)RunProgress.Finish(data,coexistence);
        IsModal=false;player.SetMenuOpen(false);
        Enqueue(StoryContent.Ending(coexistence),()=>
        {
            IsModal=true;player.SetMenuOpen(true);
            dialogue?.ShowResult(coexistence,elapsed,player.Genes!=null?player.Genes.GenInventory.Count:0);
            RefreshObjective();
        });
    }

    private void Enqueue(StoryLine[] lines,Action complete)
    {
        if(ValidationMode||dialogue==null){complete?.Invoke();return;}
        pending.Enqueue(new DialogueRequest{lines=lines,complete=complete});
        if(!IsModal&&!player.MenuOpen)PlayNext();
    }
    private void PlayNext()
    {
        if(pending.Count==0||IsModal||player.IsDead)return;
        DialogueRequest request=pending.Dequeue();IsModal=true;player.SetMenuOpen(true);
        dialogue.Open(request.lines,()=>
        {
            IsModal=false;player.SetMenuOpen(false);request.complete?.Invoke();
            GameplayUIFactory.ModalInputFrame=Time.frameCount;
            if(pending.Count>0&&!IsModal&&!player.MenuOpen)PlayNext();
        });
    }

    public void SaveCheckpoint(Vector2 position)
    {
        if(!Ready||player==null||player.IsDead)return;
        checkpoint=position;level?.SetCheckpoint(position);player.Heal(player.maxHp);
        checkpointNoticeUntil=Time.unscaledTime+3.5f;SaveCheckpointState();RefreshObjective();
    }
    public RunSaveData Capture()=>RunProgress.Capture(player,checkpoint,flags,region,GuardianCleared,elapsed,completedTriggers);
    public void SaveCheckpointState()
    {
        if(!Ready||ValidationMode||restoring||completed||player==null||player.IsDead)return;
        RunProgress.Write(Capture());
    }
    public void SetValidationMode(bool value)
    {
        ValidationMode=value;
        if(value)
        {
            pending.Clear();dialogue?.CloseForValidation();IsModal=false;
            if(player==null)player=FindFirstObjectByType<PlayerController>();
            if(player!=null)player.SetMenuOpen(false);
        }
        RefreshObjective();
    }
    private void RefreshObjective()
    {
        if(objective==null)return;
        objective.gameObject.SetActive(!IsModal);
        string chapter=region==0?"第一章 · 异常回流":region==1?"第一章 · 它们的声音":"第一章 · 水落口";
        string target;
        if(completed)target="探索完成 · 水流仍在继续";
        else if(GuardianCleared)target="前往回流阀 · E 作出最后选择";
        else if(level!=null&&level.BattleStarted)target="击败核心守卫 · 关闭净化锁";
        else if(region==2)target="在终端确认构筑 · 前往核心";
        else if(!Has(StoryBeat.LabRecord))target="阅读维修日志 · 寻找 NODE-03";
        else if(!Has(StoryBeat.SampleWater)||!Has(StoryBeat.SampleFire)||!Has(StoryBeat.SamplePoison))target="收集三类基因 · 探索实验区";
        else if(!Has(StoryBeat.Dash))target="寻找回流模块 · 解锁冲刺";
        else if(!Has(StoryBeat.ExperimentRecord))target="进入生态区 · 阅读实验记录";
        else target="前往净化核心 · 使用终端构筑";
        string progress=Time.unscaledTime<checkpointNoticeUntil?"检查点已记录 · 生命已恢复":
            "水 "+(Has(StoryBeat.SampleWater)?"●":"○")+"   火 "+(Has(StoryBeat.SampleFire)?"●":"○")+"   毒 "+(Has(StoryBeat.SamplePoison)?"●":"○")+"   冲刺 "+(player!=null&&player.DashUnlocked?"●":"○");
        objective.Set(chapter,target,progress);
    }
    private void OnDestroy()
    {
        if(player!=null&&player.Genes!=null){player.Genes.Acquired-=OnGeneAcquired;player.Genes.Changed-=OnInventoryChanged;}
        if(level!=null){level.CoreCleared-=NotifyGuardiansDefeated;level.ExitReached-=ShowCoreChoice;level.RegionEntered-=OnRegionEntered;}
        if(Current==this){Current=null;Time.timeScale=1f;}
    }
}
