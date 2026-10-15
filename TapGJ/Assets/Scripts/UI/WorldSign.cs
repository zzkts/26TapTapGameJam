using UnityEngine;

/// <summary>Short local signposts, culled without allocating text or rebuilding canvases every frame.</summary>
[RequireComponent(typeof(Canvas))]
public sealed class WorldSign : MonoBehaviour
{
    private Canvas canvas;
    private Transform player;
    private float nextCheck;
    private void Awake() { canvas = GetComponent<Canvas>(); }
    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + .15f;
        if (player == null && CampaignFlow.Current != null) player = CampaignFlow.Current.player.transform;
        canvas.enabled = player != null && (player.position - transform.position).sqrMagnitude < 40f;
    }
}
