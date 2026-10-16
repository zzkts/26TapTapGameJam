using System.Linq;
using UnityEngine;

/// <summary>The hero carries an actual limited volume; body genes change what release does.</summary>
public sealed class PlayerReservoir:MonoBehaviour
{
    public float Stored { get; private set; }=2;
    public float Pollution { get; private set; }
    public float Capacity=>6;
    public string Notice { get; private set; }="S+F 吸水 · F 放水 · 在终端装配身体基因";
    public string Traits=>string.Join(" / ",new[]{HasTrait(1)?"黏附":null,HasTrait(2)?"喷射":null,HasTrait(3)?"共生":null}.Where(x=>x!=null));
    private PlayerController player;
    private void Start(){player=GetComponent<PlayerController>();player.Backflow.Pulsed+=OnPulse;}
    public bool HasTrait(int id)=>player!=null&&player.Genes.GenChain.Any(g=>g!=null&&g.ModifierType==E_ModifierType.BodyTrait&&Mathf.RoundToInt(g.ModifierValue)==id);
    public void Restore(float amount,float pollution){Stored=float.IsNaN(amount)?2:Mathf.Clamp(amount,0,Capacity);Pollution=float.IsNaN(pollution)?0:Mathf.Clamp01(pollution);}
    private void OnPulse(BackflowPulseReport pulse)
    {if(pulse.pulling)Absorb();else Release(player.AimPosition);}
    public bool Absorb()
    {
        if(player==null||player.IsDead||player.MenuOpen)return false;
        Vector2 origin=player.GetComponent<Collider2D>().bounds.center;
        var tank=WaterReservoir2D.Active.Where(t=>t!=null&&t.Volume>.01f&&Vector2.Distance(t.NearestPoint(origin),origin)<3.2f&&player.Backflow.CanReach(origin,t.NearestPoint(origin),t.transform)).OrderBy(t=>Vector2.Distance(t.NearestPoint(origin),origin)).FirstOrDefault();
        if(tank==null)
        {
            var field=ElementField.Active.Where(f=>f!=null&&f.element==E_ElementType.Water&&Vector2.Distance(f.transform.position,origin)<3.2f&&player.Backflow.CanReach(origin,f.transform.position,f.transform)).FirstOrDefault();
            float gained=Mathf.Min(1,Capacity-Stored);
            if(field!=null&&gained>.01f){Pollution=Pollution*Stored/(Stored+gained);Stored+=gained;field.Consume();Notice="水迹已吸入身体 · 可带到其它位置释放";GameAudioPresentation.PlayCue("absorb");return true;}
            Notice="靠近蓄水池，用 S+F 吸取；也可吸收附近水迹";return false;
        }
        float pollution;float taken=tank.Take(Mathf.Min(2,Capacity-Stored),out pollution);
        if(taken<=0){Notice="身体已装满 · F 放水后再吸取";return false;}
        Pollution=(Stored*Pollution+taken*pollution)/(Stored+taken);Stored+=taken;
        Notice="吸入 "+taken.ToString("0.0")+" 水量 · 污染 "+(Pollution*100).ToString("0")+"%";GameAudioPresentation.PlayCue("absorb");return true;
    }
    public bool Release(Vector2 aim)
    {
        if(player==null||player.IsDead||player.MenuOpen||Stored<.1f){Notice="没有储水 · S+F 从水池吸取";return false;}
        Vector2 origin=player.GetComponent<Collider2D>().bounds.center;
        Vector2 heading=(aim-origin).normalized;if(heading.sqrMagnitude<.01f)heading=Vector2.right*player.FaceDir;
        Vector2 at=origin+heading*Mathf.Min(3.1f,Vector2.Distance(origin,aim));
        var hits=Physics2D.RaycastAll(origin,heading,Vector2.Distance(origin,at),LayerMask.GetMask("Ground","Wall"));
        RaycastHit2D wall=default;
        foreach(var hit in hits.OrderBy(h=>h.distance))
            if(!hit.collider.isTrigger&&!hit.collider.transform.IsChildOf(transform)&&hit.collider.GetComponent<EmergenceBody>()==null)
            {wall=hit;at=hit.point+hit.normal*.08f;break;}
        var tank=WaterReservoir2D.Active.Where(t=>t!=null&&Mathf.Abs(at.x-t.transform.position.x)<=t.width*.5f&&at.y>=t.transform.position.y-.2f&&at.y<=t.transform.position.y+t.maxHeight+.8f).OrderBy(t=>Vector2.Distance(t.transform.position,at)).FirstOrDefault();
        float amount=Mathf.Min(2,Stored);float delivered=tank!=null?tank.Add(amount,Pollution):amount;
        if(delivered<.01f){Notice="水池已满 · 先吸走脏水，或改向其它位置放水";return false;}
        Stored-=delivered;
        Vector2 fieldSize=HasTrait(1)&&wall.collider!=null&&Mathf.Abs(wall.normal.x)>.5f?new Vector2(.5f,1.8f):new Vector2(1.8f,.5f);
        if(tank==null)ElementField.Create(E_ElementType.Water,at,fieldSize,HasTrait(1)?18:5,LayerMask.GetMask("Enemy"));
        foreach(var material in EmergenceBody.Active.ToArray())
            if(material!=null&&material.GetComponent<WaterReservoir2D>()==null&&Vector2.Distance(material.transform.position,at)<1.6f)
            {material.Receive(E_ElementType.Water);if(Pollution>.4f)material.Receive(E_ElementType.Poison);}
        if(HasTrait(2)&&heading.y<-.35f)player.LaunchFromEnvironment(6);
        if(HasTrait(3)&&CampaignFlow.Current?.ecosystem!=null)
        {var eco=CampaignFlow.Current.ecosystem;var nearby=tank??WaterReservoir2D.Active.Where(t=>Vector2.Distance(t.NearestPoint(at),at)<2.8f).OrderBy(t=>Vector2.Distance(t.NearestPoint(at),at)).FirstOrDefault();EcoFungus.CreatePatch(at,eco.fungus.visual.sprite,nearby);}
        TapGJ.WaterProjectiles.WaterVfx.SpawnLiquidImpact(at,-heading,.65f,5,onSurface:wall.collider!=null);
        Notice="释放 "+delivered.ToString("0.0")+" 水量"+(Traits.Length>0?" · "+Traits:"");
        if(Stored<.01f)Pollution=0;GameAudioPresentation.PlayCue("release");return true;
    }
    private void OnDestroy(){if(player!=null&&player.Backflow!=null)player.Backflow.Pulsed-=OnPulse;}
}
