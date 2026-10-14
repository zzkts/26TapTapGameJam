using UnityEngine;

public sealed class CompleteWorldParallax : MonoBehaviour
{
    [Range(0f, 1f)] public float followFraction = .12f;
    private Transform view;
    private Vector3 origin;
    private float cameraStart;
    private void Start()
    {
        view = Camera.main != null ? Camera.main.transform : null;
        origin = transform.position; cameraStart = view != null ? view.position.x : 0f;
    }
    private void LateUpdate()
    {
        if (view != null) transform.position = origin + Vector3.right * ((view.position.x - cameraStart) * followFraction);
    }
}
