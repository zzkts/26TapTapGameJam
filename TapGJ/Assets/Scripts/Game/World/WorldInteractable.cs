using System.Collections.Generic;
using UnityEngine;

public abstract class WorldInteractable : MonoBehaviour
{
    public static readonly HashSet<WorldInteractable> Active = new HashSet<WorldInteractable>();
    [Min(0.1f)] public float interactionRange = 1.6f;
    public abstract string Title { get; }
    public virtual string Detail => "";
    public virtual bool CanUse(PlayerController player) => player != null && !player.IsDead && isActiveAndEnabled &&
        Vector2.Distance(player.transform.position, transform.position) <= interactionRange;
    public abstract bool Interact(PlayerController player);
    protected virtual void OnEnable() => Active.Add(this);
    protected virtual void OnDisable() => Active.Remove(this);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => Active.Clear();
}
