using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct EmergenceEvent
{
    public readonly string kind,trace;public readonly Vector2 position;public readonly int count;
    public EmergenceEvent(string kind,Vector2 position,string trace,int count)
    {this.kind=kind;this.position=position;this.trace=trace;this.count=count;}
}
public static class EmergenceReactions
{
    public static event Action<EmergenceEvent> Happened;
    private static bool exploding;
    private static readonly Queue<Vector2> explosions=new();
    private static readonly HashSet<EmergenceBody> consumed=new();
    public static void Emit(string kind,Vector2 at,string trace,int count)=>Happened?.Invoke(new EmergenceEvent(kind,at,trace,count));
    public static void ApplyAround(Vector2 at,float radius,E_ElementType incoming)
    {
        foreach(EmergenceBody body in new List<EmergenceBody>(EmergenceBody.Active))
            if(body!=null&&!body.Broken&&Vector2.Distance(body.transform.position,at)<=radius+.3f)body.Receive(incoming);
    }
    public static void Explode(Vector2 at,float radius)
    {
        explosions.Enqueue(at);if(exploding)return;
        exploding=true;consumed.Clear();int links=0;
        try
        {
            // Each body can contribute once; neighbouring spore pods form a bounded cascade.
            while(explosions.Count>0&&links<32)
            {
                Vector2 center=explosions.Dequeue();links++;
                HashSet<IHealthActor> hit=new();
                foreach(Collider2D collider in Physics2D.OverlapCircleAll(center,radius,LayerMask.GetMask("Enemy","Player","PlayerInvulnerable")))
                {
                    IHealthActor actor=collider.GetComponentInParent<IHealthActor>();
                    if(actor!=null&&!actor.IsDead&&hit.Add(actor))actor.TakeDamage(actor is PlayerController?1f:actor.MaxHp*.3f);
                }
                foreach(EmergenceBody body in new List<EmergenceBody>(EmergenceBody.Active))
                {
                    if(body==null||body.Broken||!consumed.Add(body))continue;
                    if(Vector2.Distance(body.transform.position,center)>radius){consumed.Remove(body);continue;}
                    body.Damage(8f);body.Receive(E_ElementType.Fire);
                }
                ElementField.ReactFields(center,radius,E_ElementType.Fire);
                TapGJ.WaterProjectiles.WaterVfx.SpawnSplash(center,new Color(1f,.58f,.2f),1f,Vector2.up);
            }
            Emit("explosion",at,"毒雾 + 火焰 → 连锁爆破 → 破坏结构",links);
        }
        finally{explosions.Clear();consumed.Clear();exploding=false;}
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){Happened=null;explosions.Clear();consumed.Clear();exploding=false;}
}
