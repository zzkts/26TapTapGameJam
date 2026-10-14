using System.Collections.Generic;
using UnityEngine;

public sealed class EmergenceSteamJet:MonoBehaviour
{
    private float expires;
    private readonly HashSet<Rigidbody2D> lifted=new();
    private LineRenderer ribbon;
    public static void Spawn(Vector2 position,float seconds)
    {
        GameObject obj=new("相变蒸汽");obj.transform.position=position;
        var jet=obj.AddComponent<EmergenceSteamJet>();jet.expires=Time.time+seconds;
        var trigger=obj.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=new Vector2(2f,1.3f);
        jet.ribbon=obj.AddComponent<LineRenderer>();jet.ribbon.sharedMaterial=new Material(Shader.Find("Sprites/Default"));
        jet.ribbon.useWorldSpace=false;jet.ribbon.positionCount=9;jet.ribbon.sortingOrder=7;
        jet.ribbon.startWidth=1.1f;jet.ribbon.endWidth=.25f;
        jet.ribbon.startColor=new Color(.8f,1f,1f,.6f);jet.ribbon.endColor=new Color(.8f,1f,1f,0f);
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if(Time.timeScale<=0f)return;
        Rigidbody2D body=other.attachedRigidbody;if(body==null||lifted.Contains(body))return;
        PlayerController player=body.GetComponent<PlayerController>();
        EmergenceBody material=body.GetComponent<EmergenceBody>();
        if(player!=null&&!player.MenuOpen&&!player.IsDead)player.LaunchFromEnvironment(7.6f);
        else if(material!=null&&material.Has(EmergenceMaterial.Movable))body.linearVelocity=new Vector2(body.linearVelocity.x,6.5f);
        else return;
        lifted.Add(body);EmergenceReactions.Emit("lift",transform.position,"蒸汽推力 → 人物 / 箱体升空",1);
    }
    private void Update()
    {
        if(Time.time>=expires){Destroy(gameObject);return;}
        for(int i=0;i<9;i++)ribbon.SetPosition(i,new Vector3(Mathf.Sin(Time.time*5f+i)*.18f,i*.45f,0));
    }
    private void OnDestroy(){if(ribbon!=null&&ribbon.sharedMaterial!=null)Destroy(ribbon.sharedMaterial);}
}
