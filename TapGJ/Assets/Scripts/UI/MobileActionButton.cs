using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public MobileGameInput.Command command;
    public Image face;
    private readonly MobilePointerOwner owner = new();
    private Color normal;
    private bool available = true;
    public bool Available
    {
        get => available;
        set { available = value; RefreshFace(); }
    }
    private void Awake() { face = GetComponent<Image>(); normal = face.color; }
    public void OnPointerDown(PointerEventData data)
    {
        if (!Available || !owner.Capture(data)) return;
        MobileGameInput.Press(command);
        RefreshFace();
        GameAudioPresentation.PlayCue("ui", .35f);
    }
    public void OnPointerUp(PointerEventData data) { if (owner.Matches(data)) Release(); }
    private void LateUpdate() { if (owner.Held && !owner.IsPressed) Release(); }
    public void Release()
    {
        owner.Clear(); MobileGameInput.Release(command);
        RefreshFace();
    }
    private void RefreshFace()
    {
        if (face == null) return;
        face.color = owner.Held ? Color.Lerp(normal, Color.white, .3f) : available ? normal : new Color(normal.r * .65f, normal.g * .65f, normal.b * .65f, .55f);
    }
    private void OnDisable() => Release();
}
