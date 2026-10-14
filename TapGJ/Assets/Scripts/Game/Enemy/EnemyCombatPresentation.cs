using UnityEngine;

/// <summary>Readable attack anticipation and recoil, applied only to the artwork child.</summary>
[DisallowMultipleComponent]
public sealed class EnemyCombatPresentation : MonoBehaviour
{
    private EnemyAIController owner;
    private SpriteRenderer artwork;
    private Transform visual;
    private Collider2D body;
    private Vector3 basePosition, baseScale, feet;
    private Quaternion baseRotation;
    private LineRenderer warningBar, warningDot, groundWarning, strikeArc;
    private Material effectMaterial;
    private float strikeMotionAt = float.NegativeInfinity, strikeAt = float.NegativeInfinity;
    private bool ready, canDeform;
    public int WindupCount { get; private set; }
    public int StrikeCount { get; private set; }
    public int CancelCount { get; private set; }
    public bool WarningVisible => warningBar != null && warningBar.enabled;

    public void Initialize(EnemyAIController enemy, SpriteRenderer renderer, Collider2D collider)
    {
        if (ready || renderer == null) return;
        owner = enemy; artwork = renderer; visual = renderer.transform; body = collider;
        basePosition = visual.localPosition; baseScale = visual.localScale; baseRotation = visual.localRotation;
        canDeform = visual != enemy.transform && visual.parent != null;
        if (canDeform) feet = visual.parent.InverseTransformPoint(new Vector3(body.bounds.center.x, body.bounds.min.y, visual.position.z));
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) effectMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
        warningBar = Line("Attack Warning", .065f, 2);
        warningDot = Line("Warning Dot", .07f, 2);
        groundWarning = Line("Attack Reach Warning", .035f, 24);
        strikeArc = Line("Attack Impact Arc", .07f, 12);
        ready = true;
    }

    private LineRenderer Line(string name, float width, int points)
    {
        GameObject child = new GameObject(name) { hideFlags = HideFlags.DontSave };
        child.transform.SetParent(transform, false);
        LineRenderer line = child.AddComponent<LineRenderer>();
        line.useWorldSpace = true; line.positionCount = points;
        line.startWidth = width; line.endWidth = width;
        line.sharedMaterial = effectMaterial; line.sortingLayerID = artwork.sortingLayerID;
        line.sortingOrder = artwork.sortingOrder + 3; line.numCapVertices = 4;
        line.enabled = false; return line;
    }

    public void BeginWindup()
    {
        WindupCount++; strikeAt = strikeMotionAt = float.NegativeInfinity;
    }
    public void BeginStrikeMotion() => strikeMotionAt = Time.time;
    public void Strike() { strikeAt = Time.time; StrikeCount++; }
    public void CancelAttack()
    {
        CancelCount++; strikeAt = strikeMotionAt = float.NegativeInfinity;
        HideEffects();
        RestoreVisual();
    }

    private void LateUpdate()
    {
        if (!ready || owner == null || !owner.enabled || owner.IsDead) { HideEffects(); RestoreVisual(); return; }
        bool warning = owner.IsWindingUp;
        float progress = owner.AttackWindupNormalized;
        warningBar.enabled = warningDot.enabled = groundWarning.enabled = warning;
        if (warning)
        {
            Color color = Color.Lerp(new Color(1f, .85f, .32f, .75f), new Color(1f, .36f, .22f, 1f), progress);
            SetColor(warningBar, color); SetColor(warningDot, color); SetColor(groundWarning, color);
            float bounce = Mathf.Sin(Time.time * 25f) * .025f;
            Vector3 head = new Vector3(body.bounds.center.x, artwork.bounds.max.y + .19f + bounce, transform.position.z);
            warningBar.SetPosition(0, head + Vector3.up * .06f); warningBar.SetPosition(1, head + Vector3.up * .24f);
            warningDot.SetPosition(0, head - Vector3.up * .025f); warningDot.SetPosition(1, head - Vector3.up * .01f);
            for (int i = 0; i < groundWarning.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / (groundWarning.positionCount - 1);
                groundWarning.SetPosition(i, new Vector3(body.bounds.center.x + Mathf.Cos(angle) * (.66f + progress * .08f),
                    body.bounds.min.y + .025f + Mathf.Sin(angle) * .055f, transform.position.z));
            }
        }
        float strikeAge = Time.time - strikeAt;
        strikeArc.enabled = strikeAge >= 0f && strikeAge < .16f && owner.State == EnemyAIController.EnemyState.Attack;
        if (strikeArc.enabled)
        {
            Color color = new Color(1f, .95f, .58f, 1f - strikeAge / .16f); SetColor(strikeArc, color);
            for (int i = 0; i < strikeArc.positionCount; i++)
            {
                float angle = Mathf.Lerp(-1.15f, 1.15f, i / (float)(strikeArc.positionCount - 1));
                strikeArc.SetPosition(i, new Vector3(body.bounds.center.x + owner.AttackFacingDirection * Mathf.Cos(angle) * (.7f + strikeAge),
                    body.bounds.center.y + Mathf.Sin(angle) * .55f, transform.position.z));
            }
        }
        if (!canDeform) return;
        Vector2 scale = warning ? new Vector2(1f + progress * .13f, 1f - progress * .13f) : Vector2.one;
        Vector3 offset = warning ? Vector3.left * owner.AttackFacingDirection * progress * .075f : Vector3.zero;
        float motionAge = Time.time - strikeMotionAt;
        if (owner.State == EnemyAIController.EnemyState.Attack && motionAge >= 0f && motionAge < .35f)
        {
            float lunge = Mathf.Sin(Mathf.Clamp01(motionAge / .35f) * Mathf.PI);
            scale += new Vector2(lunge * .15f, -lunge * .08f);
            offset.x += owner.AttackFacingDirection * lunge * .18f;
        }
        Vector3 multiplier = new Vector3(scale.x, scale.y, 1f);
        visual.localScale = Vector3.Scale(baseScale, multiplier);
        visual.localRotation = baseRotation;
        visual.localPosition = feet + Vector3.Scale(basePosition - feet, multiplier) + offset;
    }

    private static void SetColor(LineRenderer line, Color color) { line.startColor = line.endColor = color; }
    private void HideEffects()
    {
        if (warningBar != null) warningBar.enabled = false;
        if (warningDot != null) warningDot.enabled = false;
        if (groundWarning != null) groundWarning.enabled = false;
        if (strikeArc != null) strikeArc.enabled = false;
    }
    private void OnDisable()
    {
        HideEffects();
        RestoreVisual();
    }
    private void RestoreVisual()
    {
        if (canDeform && visual != null)
        { visual.localPosition = basePosition; visual.localScale = baseScale; visual.localRotation = baseRotation; }
    }
    private void OnDestroy() { if (effectMaterial != null) Destroy(effectMaterial); }
}
