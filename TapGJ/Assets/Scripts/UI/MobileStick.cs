using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>The movement stick owns its pointer independently from world-tap shooting.</summary>
public sealed class MobileStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
{
    public RectTransform knob;
    private RectTransform rect;
    private readonly MobilePointerOwner owner = new();
    public bool Held => owner.Held;
    public Vector2 Value { get; private set; }
    private void Awake() => rect = (RectTransform)transform;
    public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = false;
    public void OnPointerDown(PointerEventData data)
    {
        if (!owner.Capture(data)) return;
        Apply(data);
    }
    public void OnDrag(PointerEventData data) { if (owner.Matches(data)) Apply(data); }
    public void OnPointerUp(PointerEventData data) { if (owner.Matches(data)) Release(); }
    private void LateUpdate() { if (owner.Held && !owner.IsPressed) Release(); }
    private void Apply(PointerEventData data)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out var local)) return;
        float radius = Mathf.Max(1, rect.rect.width * .34f);
        Value = Vector2.ClampMagnitude(local / radius, 1);
        if (Value.magnitude < .12f) Value = Vector2.zero;
        if (knob != null) knob.anchoredPosition = Value * radius;
        MobileGameInput.SetMove(Value);
    }
    public void Release()
    {
        owner.Clear(); Value = Vector2.zero;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
        MobileGameInput.SetMove(Vector2.zero);
    }
    private void OnDisable() => Release();
}
