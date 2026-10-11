using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerInteraction : MonoBehaviour
{
    private PlayerController player;
    public WorldInteractable Nearby { get; private set; }
    private void Awake() => player = GetComponent<PlayerController>();
    private void Update()
    {
        if (player.IsDead || player.MenuOpen) { Nearby = null; return; }
        Nearby = null;
        float distance = float.PositiveInfinity;
        foreach (WorldInteractable item in WorldInteractable.Active)
        {
            if (item == null || !item.CanUse(player)) continue;
            float candidate = ((Vector2)item.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (candidate < distance) { distance = candidate; Nearby = item; }
        }
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && Nearby != null) Nearby.Interact(player);
    }
}
