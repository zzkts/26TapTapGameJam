using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector2 horizontalLimits = new Vector2(-7f, 176f);
    public float smoothing = 0.15f;
    private Vector3 velocity;
    private float baseY;
    private void Awake() { baseY = transform.position.y; }
    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 goal = new Vector3(Mathf.Clamp(target.position.x + 1.2f, horizontalLimits.x, horizontalLimits.y),
            Mathf.Max(baseY, target.position.y + 0.5f), transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothing);
    }
}
