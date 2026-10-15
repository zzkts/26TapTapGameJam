using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A bounded volume of water with conserved mixing, visible water level and physical buoyancy.</summary>
[RequireComponent(typeof(EmergenceBody))]
public sealed class WaterReservoir2D:MonoBehaviour
{
    public static readonly HashSet<WaterReservoir2D> Active=new();
    public string reservoirId;
    public float width=7,maxHeight=2.4f,capacity=10,initialVolume=4,initialPollution=.78f;
    public SpriteRenderer water;
    public Text readout;
    public float Volume { get; private set; }
    public float Pollution { get; private set; }
    public float SurfaceY=>transform.position.y+maxHeight*Volume/capacity;
    public float FlowAdded { get; private set; }
    private EmergenceBody material;
    private MaterialPropertyBlock properties;
    private BoxCollider2D waterContact;
    private float nextWet;
    private float nextReadout;
    private readonly Collider2D[] overlaps=new Collider2D[48];
    private readonly HashSet<Rigidbody2D> moved=new();
    private void Awake()
    {material=GetComponent<EmergenceBody>();waterContact=GetComponent<BoxCollider2D>();properties=new MaterialPropertyBlock();SetState(initialVolume,initialPollution);material.Received+=Receive;}
    private void OnEnable()=>Active.Add(this);
    private void OnDisable()=>Active.Remove(this);
    private void OnDestroy(){if(material!=null)material.Received-=Receive;}
    public void SetState(float volume,float pollution)
    {Volume=ClampFinite(volume,0,capacity);Pollution=Volume>.001f?ClampFinite(pollution,0,1):0;}
    public float Add(float amount,float pollution)
    {
        float received=Mathf.Clamp(amount,0,capacity-Volume);
        if(received<=0)return 0;
        Pollution=(Pollution*Volume+Mathf.Clamp01(pollution)*received)/(Volume+received);
        Volume+=received;FlowAdded+=received;return received;
    }
    public float Take(float amount,out float pollution)
    {pollution=Pollution;float taken=Mathf.Clamp(amount,0,Volume);Volume-=taken;if(Volume<.001f)Pollution=0;return taken;}
    public void Purify(float pollutantMass)
    {if(Volume>.01f)Pollution=Mathf.Max(0,Pollution-Mathf.Max(0,pollutantMass)/Volume);}
    public void Evaporate(float amount)
    {float mass=Volume*Pollution;Volume=Mathf.Max(0,Volume-amount);Pollution=Volume>.01f?Mathf.Clamp01(mass/Volume):0;}
    public bool Contains(Vector2 point)=>Volume>.05f&&Mathf.Abs(point.x-transform.position.x)<width*.5f&&point.y>=transform.position.y-.1f&&point.y<=SurfaceY+.15f;
    public Vector2 NearestPoint(Vector2 at)=>new Vector2(Mathf.Clamp(at.x,transform.position.x-width*.5f,transform.position.x+width*.5f),Mathf.Clamp(at.y,transform.position.y,SurfaceY));
    private void Receive(E_ElementType element)
    {
        if(element==E_ElementType.Water)Add(.22f,0);
        else if(element==E_ElementType.Poison)Add(.25f,1);
        else if(element==E_ElementType.Fire&&Volume>.2f){Evaporate(.3f);EmergenceSteamJet.Spawn(new Vector2(transform.position.x,SurfaceY),1.4f);}
    }
    private void Update()
    {
        if(waterContact!=null)
        {float height=Mathf.Max(.015f,SurfaceY-transform.position.y);waterContact.enabled=Volume>.02f;waterContact.size=new Vector2(width,height);waterContact.offset=Vector2.up*height*.5f;}
        if(water!=null)
        {
            water.enabled=Volume>.02f;
            float height=Mathf.Max(.01f,SurfaceY-transform.position.y);
            water.transform.position=new Vector3(transform.position.x,transform.position.y+height*.5f,0);
            water.transform.localScale=new Vector3(width,height,1);
            properties.SetFloat("_Pollution",Pollution);properties.SetFloat("_Charged",material.Charged?1:0);
            water.SetPropertyBlock(properties);
        }
        if(readout!=null&&Time.unscaledTime>=nextReadout)
        {
            nextReadout=Time.unscaledTime+.15f;
            readout.text=reservoirId+" · 水量 "+Volume.ToString("0.0")+" / "+capacity+"\n污染 "+(Pollution*100).ToString("0")+"%"+(material.Charged?" · 带电":"");
        }
    }
    private void FixedUpdate()
    {
        if(Volume<.05f)return;
        float height=SurfaceY-transform.position.y;
        int count=Physics2D.OverlapBox(new Vector2(transform.position.x,transform.position.y+height*.5f),new Vector2(width,height),0,new ContactFilter2D{useTriggers=true},overlaps);
        moved.Clear();bool wet=Time.time>=nextWet;if(wet)nextWet=Time.time+.7f;
        for(int i=0;i<count;i++)
        {
            var hit=overlaps[i];if(hit==null)continue;
            var body=hit.attachedRigidbody;if(body==null||!moved.Add(body)||body.bodyType!=RigidbodyType2D.Dynamic)continue;
            var actor=body.GetComponent<PlayerController>();var cargo=body.GetComponent<EmergenceBody>();
            if(actor==null&&(cargo==null||!cargo.Has(EmergenceMaterial.Movable)))continue;
            if(wet){if(actor!=null)actor.AddBuff(E_ElementType.Water);if(cargo!=null)cargo.Receive(E_ElementType.Water);}
            float target=SurfaceY+(actor!=null?-.35f:hit.bounds.extents.y*.5f);
            float lift=Mathf.Clamp((target-body.worldCenterOfMass.y)*5f-body.linearVelocity.y*1.8f,-3f,8f);
            body.AddForce(Vector2.up*(lift+Mathf.Abs(Physics2D.gravity.y)*body.gravityScale)*body.mass);
        }
    }
    private static float ClampFinite(float value,float min,float max)=>float.IsNaN(value)||float.IsInfinity(value)?min:Mathf.Clamp(value,min,max);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void ResetRegistry()=>Active.Clear();
}
