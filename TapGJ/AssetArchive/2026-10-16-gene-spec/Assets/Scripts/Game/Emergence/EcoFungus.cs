using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(EmergenceBody))]
public sealed class EcoFungus:MonoBehaviour
{
    public static readonly HashSet<EcoFungus> Active=new();
    public WaterReservoir2D reservoir;
    public GameObject bridge;
    public SpriteRenderer visual;
    public Text readout;
    public bool temporary;
    public float Growth { get; private set; }=.15f;
    public bool BridgeGrown=>Growth>=.6f;
    public bool Clogged=>Growth>1.2f;
    public bool Mature=>Growth>=.45f;
    private EmergenceBody material;
    private Vector3 baseScale;
    private float wetUntil,expires;
    private bool discovered;
    private float nextReadout;
    private void Awake(){material=GetComponent<EmergenceBody>();material.Received+=Receive;if(visual!=null)baseScale=visual.transform.localScale;expires=Time.time+22;}
    private void OnEnable()=>Active.Add(this);
    private void OnDisable()=>Active.Remove(this);
    private void OnDestroy(){if(material!=null)material.Received-=Receive;}
    public void SetGrowth(float value)=>Growth=float.IsNaN(value)?0:Mathf.Clamp(value,.08f,1.4f);
    public void Nourish(float amount){SetGrowth(Growth+amount);wetUntil=Time.time+10;}
    private void Receive(E_ElementType element)
    {
        if(element==E_ElementType.Water)Nourish(.055f);
        else if(element==E_ElementType.Fire){SetGrowth(Growth-.24f);wetUntil=0;}
        else if(element==E_ElementType.Poison){Nourish(.08f);reservoir?.Add(.18f,1);}
    }
    private void Update()
    {
        if(temporary&&Time.time>=expires){Destroy(gameObject);return;}
        if(reservoir!=null&&reservoir.Contains(transform.position))wetUntil=Time.time+1;
        if(Time.time<wetUntil)SetGrowth(Growth+Time.deltaTime*.045f);
        if(Mature&&reservoir!=null)reservoir.Purify(Time.deltaTime*.40f*Growth);
        if(bridge!=null)bridge.SetActive(BridgeGrown);
        if(visual!=null)visual.transform.localScale=baseScale*Mathf.Lerp(.65f,1.4f,Growth/1.4f);
        if(readout!=null&&Time.unscaledTime>=nextReadout)
        {
            nextReadout=Time.unscaledTime+.15f;
            readout.text="共生母菌 · 生长 "+(Growth*100).ToString("0")+"%\n"+(Clogged?"根系堵管 · 火种修剪":Mature?"正在净化 · 成熟可架桥":"水弹 / 放水 / 治疗光环促生长");
        }
        if(BridgeGrown&&!discovered){discovered=true;EmergenceReactions.Emit("ecology",transform.position,"水分 → 菌桥生长 → 净化水源 → 主泵可持续运行",1);}
    }
    public static EcoFungus CreatePatch(Vector2 position,Sprite sprite,WaterReservoir2D water)
    {
        var obj=new GameObject("共生基因菌簇");obj.transform.position=position;
        var face=new GameObject("菌簇图像");face.transform.SetParent(obj.transform,false);var sr=face.AddComponent<SpriteRenderer>();
        sr.sprite=sprite;sr.sortingOrder=5;face.transform.localScale=Vector3.one*.7f/sprite.bounds.size.x;
        var body=obj.AddComponent<EmergenceBody>();body.material=EmergenceMaterial.Organic;
        var fungus=obj.AddComponent<EcoFungus>();fungus.visual=sr;fungus.baseScale=face.transform.localScale;fungus.reservoir=water;fungus.temporary=true;fungus.Nourish(.4f);return fungus;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void ResetRegistry()=>Active.Clear();
}
