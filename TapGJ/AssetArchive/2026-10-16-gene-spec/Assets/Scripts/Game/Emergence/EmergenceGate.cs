using UnityEngine;

public sealed class EmergenceGate:MonoBehaviour
{
    public string persistentId;
    public Collider2D barrier;
    public SpriteRenderer artwork;
    public bool IsOpen { get; private set; }
    private bool permanent;
    public void OpenPermanently()
    {
        permanent=true;SetPowered(true);
        if(!string.IsNullOrEmpty(persistentId))EmergenceProgress.Current?.Complete(persistentId);
    }
    public void SetPowered(bool value)
    {
        if(permanent)value=true;
        if(!value&&barrier!=null)
        {
            // Do not close a moving gate through a person or a crate.
            BoxCollider2D box=barrier as BoxCollider2D;
            Vector2 center=box!=null?box.transform.TransformPoint(box.offset):barrier.transform.position;
            Vector2 size=box!=null?Vector2.Scale(box.size,box.transform.lossyScale):Vector2.one;
            Collider2D[] nearby=Physics2D.OverlapBoxAll(center,size,0);
            foreach(Collider2D collider in nearby)if(collider!=barrier&&collider.attachedRigidbody!=null)return;
        }
        IsOpen=value;if(barrier!=null)barrier.enabled=!value;
        if(artwork!=null)artwork.color=value?new Color(.4f,1f,.8f,.17f):new Color(.65f,.51f,.78f);
    }
}
