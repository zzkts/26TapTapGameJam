using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>World taps share the desktop screen-to-world aim, beneath every interactive UI control.</summary>
public sealed class MobileScreenAim : MonoBehaviour, IPointerDownHandler, IDragHandler,
    IPointerUpHandler, IInitializePotentialDragHandler
{
    private readonly MobilePointerOwner owner = new();
    private readonly List<RaycastResult> hits = new();
    public bool Held => owner.Held;

    public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = false;
    public void OnPointerDown(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || !owner.Capture(data)) return;
        MobileGameInput.BeginScreenAim(data.position);
    }
    public void OnDrag(PointerEventData data)
    {
        if (!owner.Matches(data)) return;
        // Dragging over a button must not fire behind that button.
        hits.Clear();
        EventSystem.current.RaycastAll(data, hits);
        MobileGameInput.SetScreenAim(data.position, hits.Count > 0 && hits[0].gameObject == gameObject);
    }
    public void OnPointerUp(PointerEventData data) { if (owner.Matches(data)) Release(); }
    private void LateUpdate() { if (owner.Held && !owner.IsPressed) Release(); }
    public void Release() { owner.Clear(); MobileGameInput.EndScreenAim(); }
    private void OnDisable() => Release();
}
