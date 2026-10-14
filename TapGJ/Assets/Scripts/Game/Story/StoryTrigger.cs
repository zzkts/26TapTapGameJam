using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class StoryTrigger : MonoBehaviour
{
    public StoryBeat beat;
    public string triggerId;
    private bool activated;
    private string Key=>string.IsNullOrEmpty(triggerId)?gameObject.name+"@"+transform.position.x.ToString("F1",System.Globalization.CultureInfo.InvariantCulture):triggerId;
    private void Reset(){Collider2D collider=GetComponent<Collider2D>();if(collider!=null)collider.isTrigger=true;}
    private void OnTriggerEnter2D(Collider2D other)=>TryActivate(other);
    private void OnTriggerStay2D(Collider2D other)=>TryActivate(other);
    private void TryActivate(Collider2D other)
    {
        if(activated)return;
        PlayerController player=other.GetComponentInParent<PlayerController>();
        if(player!=null)Activate(player);
    }
    public bool Activate(PlayerController player)
    {
        StorySession session=StorySession.Current;
        if(activated||player==null||player.IsDead||session==null||!session.Ready||player.MenuOpen)return false;
        if(beat==StoryBeat.Checkpoint&&session.WasTriggerCompleted(Key)){activated=true;return false;}
        activated=session.Reach(beat,transform.position);
        if(activated&&beat==StoryBeat.Checkpoint)session.RegisterTriggerComplete(Key);
        return activated;
    }
}
