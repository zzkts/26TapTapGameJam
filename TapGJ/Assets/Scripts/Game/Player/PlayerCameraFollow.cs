using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector2 horizontalLimits = new Vector2(-7f, 176f);
    public float smoothing = 0.15f;
    private Vector3 velocity;
    private float baseY;
    private Vector3 followPosition;
    private float shakeUntil, shakeDuration, shakeStrength;
    private void Awake() { baseY = transform.position.y; followPosition = transform.position; }
    public void Shake(float strength, float duration)
    {
        shakeStrength = Mathf.Max(shakeStrength, strength);
        shakeDuration = Mathf.Max(.01f, duration);
        shakeUntil = Time.unscaledTime + shakeDuration;
    }
    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 goal = new Vector3(Mathf.Clamp(target.position.x + 1.2f, horizontalLimits.x, horizontalLimits.y),
            Mathf.Max(baseY, target.position.y + 0.5f), transform.position.z);
        // Keep the shake out of the follow solver so impacts never accumulate camera drift.
        followPosition = Vector3.SmoothDamp(followPosition, goal, ref velocity, smoothing);
        float remaining = Mathf.Clamp01((shakeUntil - Time.unscaledTime) / Mathf.Max(.01f, shakeDuration));
        PlayerController player = target.GetComponent<PlayerController>();
        float strength = player != null && player.MenuOpen ? 0f : shakeStrength * remaining;
        Vector3 offset = new Vector3(Mathf.Sin(Time.unscaledTime * 89f), Mathf.Cos(Time.unscaledTime * 71f), 0f) * strength;
        transform.position = followPosition + offset;
        if (remaining == 0f) shakeStrength = 0f;
    }
}
