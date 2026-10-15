using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum EmergenceMaterial { None=0, Conductive=1, Organic=2, Flammable=4, Fragile=8, Boiler=16, Movable=32 }

/// <summary>Material properties shared by scenery, puzzle machinery and movable objects.</summary>
public sealed class EmergenceBody : MonoBehaviour
{
    public static readonly HashSet<EmergenceBody> Active = new();
    public EmergenceMaterial material;
    public float durability=8f, wetDuration=12f, growthCooldown=18f;
    public EmergenceGate chargedGate;
    public string completionId;
    public TextMesh status;
    public bool Wet => wetUntil>Time.time;
    public bool Poisoned => poisonUntil>Time.time;
    public bool Charged => chargedUntil>Time.time;
    public bool Broken { get; private set; }
    public bool Bloomed { get; private set; }
    public int Pressure { get; private set; }
    public float LinkRadius => Wet ? 2.1f : .65f;
    private float wetUntil,poisonUntil,chargedUntil,burningUntil,nextBurn,nextGrowth,hp;
    private int lastFireFrame=-1;
    private SpriteRenderer visual;
    private Color original;
    private Vector3 scale;
    public bool Has(EmergenceMaterial flag)=>(material&flag)!=0;
    private void Awake()
    {
        hp=durability;visual=GetComponentInChildren<SpriteRenderer>();
        if(visual!=null){original=visual.color;scale=visual.transform.localScale;}
    }
    private void OnEnable()=>Active.Add(this);
    private void OnDisable()=>Active.Remove(this);
    public void ReceiveShot(float damage,E_ElementType element,Vector2 heading,float impulse)
    {
        if(Broken)return;
        Rigidbody2D body=GetComponent<Rigidbody2D>();
        if(Has(EmergenceMaterial.Movable)&&body!=null)
            body.AddForce(heading*Mathf.Clamp(impulse,.2f,4f),ForceMode2D.Impulse);
        Receive(element==E_ElementType.None?E_ElementType.Water:element);
        // A physical fallback remains available; reactions are substantially more efficient.
        if(Has(EmergenceMaterial.Fragile))Damage(Mathf.Max(0f,damage)*.25f);
    }
    public void Receive(E_ElementType incoming)
    {
        if(Broken)return;
        if(incoming==E_ElementType.Water)
        {
            wetUntil=Time.time+wetDuration;burningUntil=0f;
            if(Has(EmergenceMaterial.Boiler))Pressure=Mathf.Min(3,Pressure+1);
            if(Has(EmergenceMaterial.Organic)&&Time.time>=nextGrowth)
            {
                Bloomed=true;poisonUntil=Time.time+12f;nextGrowth=Time.time+growthCooldown;
                ElementField.Create(E_ElementType.Poison,transform.position+Vector3.up*.3f,new Vector2(5.5f,1.1f),7f,LayerMask.GetMask("Enemy","Player"));
                EmergenceReactions.Emit("growth",transform.position,"水分 → 菌落生长 → 毒雾",1);
            }
        }
        else if(incoming==E_ElementType.Poison)poisonUntil=Time.time+12f;
        else if(incoming==E_ElementType.Electric&&Has(EmergenceMaterial.Conductive))Energize();
        else if(incoming==E_ElementType.Fire)
        {
            // A projectile's direct hit and its splash describe one ignition in this frame.
            if(lastFireFrame==Time.frameCount)return;
            lastFireFrame=Time.frameCount;
            bool steam=Wet,explode=Poisoned;
            if(steam){wetUntil=0f;ReleaseSteam();}
            if(explode){poisonUntil=0f;EmergenceReactions.Explode(transform.position,3.2f);}
            if(!steam&&!explode&&Has(EmergenceMaterial.Flammable))burningUntil=Time.time+4f;
        }
    }
    public void Energize()
    {
        chargedUntil=Time.time+2.8f;
        if(chargedGate!=null)chargedGate.OpenPermanently();
        if(!string.IsNullOrEmpty(completionId))EmergenceProgress.Current?.Complete(completionId);
    }
    public bool ReleaseSteam()
    {
        Pressure=0;EmergenceSteamJet.Spawn(transform.position+Vector3.up*.55f,3.5f);
        EmergenceReactions.Emit("steam",transform.position,"水分 + 热量 → 蒸汽推力",1);return true;
    }
    public void Damage(float amount)
    {
        if(Broken||!Has(EmergenceMaterial.Fragile)||amount<=0f)return;
        hp-=amount;if(hp>0f)return;
        Broken=true;foreach(Collider2D collider in GetComponentsInChildren<Collider2D>())collider.enabled=false;
        if(visual!=null)visual.enabled=false;
        if(!string.IsNullOrEmpty(completionId))EmergenceProgress.Current?.Complete(completionId);
        if(!string.IsNullOrEmpty(completionId))StorySession.Current?.RegisterTriggerComplete("structure:"+completionId);
        EmergenceReactions.Emit("fracture",transform.position,"冲击 → 结构破坏 → 新通路",1);
    }
    private void Update()
    {
        if(Broken)return;
        if(burningUntil>Time.time&&Time.time>=nextBurn)
        {
            nextBurn=Time.time+.8f;Damage(2f);
            foreach(EmergenceBody other in new List<EmergenceBody>(Active))
                if(other!=null&&other!=this&&Vector2.Distance(other.transform.position,transform.position)<1.8f)
                    other.Receive(E_ElementType.Fire);
            ElementField.ReactFields(transform.position,1f,E_ElementType.Fire);
        }
        if(visual!=null)
        {
            Color tint=Charged?new Color(1f,.91f,.45f):Poisoned?new Color(.55f,1f,.42f):Wet?new Color(.42f,.94f,1f):burningUntil>Time.time?new Color(1f,.5f,.25f):Color.white;
            visual.color=original*tint;
            if(Has(EmergenceMaterial.Organic))visual.transform.localScale=scale*(Bloomed&&Poisoned?1.18f:1f);
        }
    }
    public void RestoreBroken()
    { if(!Broken){hp=0f;Damage(1f);} }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()=>Active.Clear();
}
