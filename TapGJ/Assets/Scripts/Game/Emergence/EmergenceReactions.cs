using System;
using UnityEngine;

// Combat feedback only. Reactions do not award energy or unlock environmental objectives.
public readonly struct EmergenceEvent
{
    public readonly string kind,trace;public readonly Vector2 position;public readonly int count;
    public EmergenceEvent(string kind,Vector2 position,string trace,int count)
    {this.kind=kind;this.position=position;this.trace=trace;this.count=count;}
}
public static class EmergenceReactions
{
    public static event Action<EmergenceEvent> Happened;
    public static void Emit(string kind,Vector2 at,string trace,int count)=>Happened?.Invoke(new EmergenceEvent(kind,at,trace,count));
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){Happened=null;}
}
