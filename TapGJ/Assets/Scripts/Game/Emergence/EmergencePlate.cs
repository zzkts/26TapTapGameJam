using System.Collections.Generic;
using UnityEngine;

public sealed class EmergencePlate:MonoBehaviour
{
    public EmergenceGate gate;
    public float requiredMass=.8f;
    public bool Pressed { get; private set; }
    public SpriteRenderer visual;
    private void FixedUpdate()
    {
        HashSet<Rigidbody2D> seen=new();float weight=0;
        foreach(Collider2D collider in Physics2D.OverlapBoxAll((Vector2)transform.position+Vector2.up*.3f,new Vector2(1.8f,.65f),0))
        {
            Rigidbody2D body=collider.attachedRigidbody;
            if(body!=null&&body.bodyType==RigidbodyType2D.Dynamic&&seen.Add(body))weight+=body.mass;
        }
        bool previouslyPressed=Pressed;
        Pressed=weight>=requiredMass;gate?.SetPowered(Pressed);
        if(Pressed&&!previouslyPressed)
            EmergenceReactions.Emit("inertia",transform.position,"重量变化 → 阀板下压 → 回路开启",1);
        if(visual!=null)visual.color=Pressed?new Color(.5f,1f,.7f):new Color(.6f,.5f,.8f);
    }
}
