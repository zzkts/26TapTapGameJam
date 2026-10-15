using UnityEngine;
public sealed class CampaignReactor:MonoBehaviour
{
    public enum Rule { Pressure,Weight,Growth,Conduction,Fracture }
    public Rule rule;
    public string displayName,hint,reward="split";
    public EmergenceBody material;
    public EmergencePlate plate;
    public SpriteRenderer beacon;
    public bool Complete { get; private set; }
    public string StateText=>Complete?"● 已接通":rule==Rule.Pressure?"蓄水 "+material.Pressure+" / 3":rule==Rule.Weight?(plate.Pressed?"阀板已压下":"阀板尚未受压"):rule==Rule.Growth?"母菌等待水分":rule==Rule.Conduction?"接收器等待电流":"菌壳尚未击破";
    public string SaveId=>"campaign:"+gameObject.scene.name+":"+gameObject.name;
    private float weighted;
    public void Restore(bool done){Complete=done;if(beacon!=null)beacon.color=done?PlayerUIStyle.Mint:PlayerUIStyle.Violet;if(done&&rule==Rule.Fracture&&material!=null)material.RestoreBroken();}
    private void Update()
    {
        if(Complete||CampaignFlow.Current==null||!CampaignFlow.Current.Ready)return;
        if(plate!=null)weighted=plate.Pressed?weighted+Time.deltaTime:0;
        bool done=rule==Rule.Weight?weighted>=.5f:material!=null&&(rule==Rule.Pressure?material.Pressure>=3||material.SteamCount>0:
            rule==Rule.Growth?material.Bloomed:rule==Rule.Conduction?material.Charged:material.Broken);
        if(done)CampaignFlow.Current.CompleteReactor(this);
    }
}

