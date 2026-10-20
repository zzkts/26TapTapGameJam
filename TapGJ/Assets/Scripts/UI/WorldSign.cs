using UnityEngine;

/// <summary>Short local signposts, culled without allocating text or rebuilding canvases every frame.</summary>
[RequireComponent(typeof(Canvas))]
public sealed class WorldSign : MonoBehaviour
{
    private Canvas canvas;
    private Transform player;
    private float nextCheck;
    private void Awake() { canvas = GetComponent<Canvas>(); }
    private void Start()
    {
        if(!MobileGameInput.Enabled)return;
        foreach(var label in GetComponentsInChildren<UnityEngine.UI.Text>(true))
            label.text=MobileGameInput.AdaptHint(label.text).Replace("LeftShift","冲刺键").Replace("SHIFT","冲刺键").Replace("Space","跳跃键");
    }
    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + .15f;
        if (player == null && CampaignFlow.Current != null) player = CampaignFlow.Current.player.transform;
        canvas.enabled = player != null && (player.position - transform.position).sqrMagnitude < 40f;
    }
}
