using UnityEngine;
public class EnvironmentElementSource : MonoBehaviour
{
    public E_ElementType element = E_ElementType.Electric;
    [Min(0.1f)] public float radius = 1f, interval = 1f;
    private float nextPulse;
    private void Update()
    {
        if (Time.time < nextPulse) return;
        nextPulse = Time.time + interval;
        if (element == E_ElementType.Electric) ActorElementEffects.Conduct(transform.position, radius);
        else ElementField.ApplyAround(transform.position, radius, element, LayerMask.GetMask("Player", "Enemy"));
    }
}
