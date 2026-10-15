using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Player-authored materials use the same reactions as the world, not bespoke puzzle keys.</summary>
public sealed class PlayerSynthesis:MonoBehaviour
{
    public float Energy { get; private set; }=3f;
    public float MaxEnergy=>3f;
    public E_ElementType Selected { get; private set; }=E_ElementType.Water;
    public string SelectedName=>Selected==E_ElementType.Water?"水种":Selected==E_ElementType.Fire?"火种":"孢子种";
    public string Notice { get; private set; }="Q 切换 · R 播种 · T 回收 · 空中空格踏跃";
    public readonly List<ReactionSeed> Seeds=new();
    public int AirSteps { get; private set; }
    private PlayerController player;
    private bool airUsed;
    private float nextSow,nextReward;
    private void Awake(){player=GetComponent<PlayerController>();EmergenceReactions.Happened+=OnReaction;}
    private void Update()
    {
        if(player==null||player.IsDead||player.MenuOpen)return;
        Energy=Mathf.Min(MaxEnergy,Energy+Time.deltaTime*.4f);
        if(player.IsGrounded())airUsed=false;
        Keyboard k=Keyboard.current;if(k==null)return;
        if(k.qKey.wasPressedThisFrame)Cycle();
        if(k.rKey.wasPressedThisFrame)Sow(player.AimPosition);
        if(k.tKey.wasPressedThisFrame)Recall();
    }
    public void Cycle()
    {Selected=Selected==E_ElementType.Water?E_ElementType.Fire:Selected==E_ElementType.Fire?E_ElementType.Poison:E_ElementType.Water;Notice="已选择 "+SelectedName+" · R 投放到瞄准位置";}
    public bool Sow(Vector2 aim)
    {
        if(player==null||player.MenuOpen||player.IsDead||player.IsHurt||player.IsDashing||Time.time<nextSow)return false;
        if(Energy<1f){Notice="回流能量不足 · 稍候恢复，或 T 回收种子";return false;}
        Vector2 from=player.GetComponent<Collider2D>().bounds.center;
        Vector2 delta=aim-from;if(delta.magnitude<.1f)delta=Vector2.right*player.FaceDir;
        float distance=Mathf.Min(3.3f,delta.magnitude);Vector2 heading=delta.normalized;
        var filter=new ContactFilter2D();filter.useTriggers=false;filter.SetLayerMask(LayerMask.GetMask("Ground","Wall","OneWayPlatform"));
        var hits=new RaycastHit2D[24];int count=Physics2D.CircleCast(from,.34f,heading,filter,hits,distance);
        for(int i=0;i<count;i++)
        {
            var hit=hits[i];if(hit.collider==null||hit.collider.GetComponentInParent<ReactionSeed>()!=null||hit.collider.transform.IsChildOf(player.transform))continue;
            if(hit.collider.GetComponent<OneWayPlatform>()!=null&&from.y-.34f<hit.collider.bounds.max.y-.01f)continue;
            distance=Mathf.Min(distance,Mathf.Max(0f,hit.distance-.08f));
        }
        if(distance<.35f){Notice="前方没有放置空间 · 改变瞄准方向";return false;}
        Seeds.RemoveAll(s=>s==null);
        if(Seeds.Count>=2){Destroy(Seeds[0].gameObject);Seeds.RemoveAt(0);}
        ReactionSeed seed=ReactionSeed.Create(this,from+heading*distance,Selected);Seeds.Add(seed);
        Energy-=1f;nextSow=Time.time+.3f;Notice=SelectedName+"已播下 · 水火相变 / 火毒爆破 / 湿种导电";
        player.Presentation?.Shoot(heading);return true;
    }
    public bool Recall()
    {
        if(player==null||player.IsDead||player.MenuOpen)return false;
        Seeds.RemoveAll(s=>s==null);if(Seeds.Count==0){Notice="附近没有自己的种子";return false;}
        foreach(ReactionSeed seed in Seeds)if(seed!=null)Destroy(seed.gameObject);
        Energy=Mathf.Min(MaxEnergy,Energy+Seeds.Count*.5f);Seeds.Clear();Notice="种子已回收 · 能量返还";return true;
    }
    public bool TryAirStep()
    {
        if(player==null||airUsed||player.IsDead||player.IsHurt||player.MenuOpen||Energy<.4f)return false;
        airUsed=true;Energy-=.4f;AirSteps++;
        var rb=player.GetComponent<Rigidbody2D>();rb.linearVelocity=new Vector2(rb.linearVelocity.x,4.8f);
        TapGJ.WaterProjectiles.WaterVfx.SpawnSplash(player.transform.position,PlayerUIStyle.Mint,.7f,Vector2.down);
        Notice="泡泡踏跃 · 落地后恢复一次空中踏跃";return true;
    }
    private void OnReaction(EmergenceEvent e)
    {
        if(player==null||player.IsDead||Time.time<nextReward||Vector2.Distance(e.position,player.transform.position)>7f)return;
        nextReward=Time.time+2f;Energy=Mathf.Min(MaxEnergy,Energy+.35f);
    }
    private void OnDestroy(){EmergenceReactions.Happened-=OnReaction;foreach(var seed in Seeds)if(seed!=null)Destroy(seed.gameObject);}
}

public sealed class ReactionSeed:MonoBehaviour
{
    public E_ElementType Element { get; private set; }
    public EmergenceBody Body { get; private set; }
    private float expires,nextPulse;
    private SpriteRenderer face;
    public static ReactionSeed Create(PlayerSynthesis owner,Vector2 at,E_ElementType element)
    {
        var obj=new GameObject("玩家播种 · "+element);obj.transform.position=at;obj.layer=LayerMask.NameToLayer("Ground");
        var collider=obj.AddComponent<CircleCollider2D>();collider.radius=.32f;
        var rb=obj.AddComponent<Rigidbody2D>();rb.mass=1f;rb.gravityScale=.65f;rb.linearDamping=.8f;rb.freezeRotation=true;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
        var visual=new GameObject("种子光核");visual.transform.SetParent(obj.transform,false);
        var sr=visual.AddComponent<SpriteRenderer>();sr.sprite=PlayerUIStyle.Circle;sr.sortingOrder=12;
        float width=sr.sprite.bounds.size.x;visual.transform.localScale=Vector3.one*(.65f/width);
        sr.color=element==E_ElementType.Fire?new Color(1f,.55f,.22f):element==E_ElementType.Poison?new Color(.65f,1f,.32f):PlayerUIStyle.Mint;
        EmergenceBody body=obj.AddComponent<EmergenceBody>();body.material=EmergenceMaterial.Movable|EmergenceMaterial.Conductive;
        if(element==E_ElementType.Water)body.material|=EmergenceMaterial.Boiler;
        if(element==E_ElementType.Poison)body.material|=EmergenceMaterial.Organic;
        var seed=obj.AddComponent<ReactionSeed>();seed.Element=element;seed.Body=body;seed.face=sr;seed.expires=Time.time+25f;
        body.Receive(element);
        Collider2D playerCollider=owner.GetComponent<Collider2D>();if(playerCollider!=null)Physics2D.IgnoreCollision(collider,playerCollider,true);
        Physics2D.SyncTransforms();return seed;
    }
    private void Update()
    {
        if(Time.time>=expires){Destroy(gameObject);return;}
        if(face!=null)face.transform.localScale=Vector3.one*(.65f/face.sprite.bounds.size.x)*(1f+Mathf.Sin(Time.time*4f)*.06f);
        if(Time.time<nextPulse)return;nextPulse=Time.time+1.5f;
        Vector2 at=transform.position;
        if(Element==E_ElementType.Fire)ElementField.ApplyAround(at,1.45f,Element,LayerMask.GetMask("Enemy"));
        else ElementField.Create(Element,at,new Vector2(2.6f,.8f),2f,LayerMask.GetMask("Enemy"));
    }
}
