using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>Retains the actual touch identity, with a fallback when UGUI loses a release event.</summary>
public sealed class MobilePointerOwner
{
    private int? pointerId;
    private InputDevice device;
    private int touchId;
    public bool Held => pointerId.HasValue;
    public bool Capture(PointerEventData data)
    {
        if (Held) return false;
        pointerId = data.pointerId;
        if (data is ExtendedPointerEventData extended) { device = extended.device; touchId = extended.touchId; }
        return true;
    }
    public bool Matches(PointerEventData data) => pointerId == data.pointerId;
    public bool IsPressed
    {
        get
        {
            if (!Held || device != null && !device.added) return false;
            if (device is Touchscreen screen)
            {
                foreach (var touch in screen.touches)
                    if (touch.touchId.ReadValue() == touchId && touch.press.isPressed) return true;
                return false;
            }
            if (device is Mouse mouse) return mouse.leftButton.isPressed;
            return true;
        }
    }
    public void Clear() { pointerId = null; device = null; touchId = 0; }
}
