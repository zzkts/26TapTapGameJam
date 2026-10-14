using UnityEngine;

/// <summary>Secondary motion lives on the artwork child and never changes the player's collider.</summary>
[DisallowMultipleComponent]
public sealed class PlayerPresentation : MonoBehaviour
{
    private const int EchoCount = 4;
    private PlayerController player;
    private SpriteRenderer artworkRenderer;
    private Rigidbody2D body;
    private Transform visual;
    private Vector3 basePosition, baseScale, footAnchor;
    private Quaternion baseRotation;
    private Animator animator;
    private AnimatorUpdateMode originalUpdateMode;
    private float jumpAt = float.NegativeInfinity, landAt = float.NegativeInfinity;
    private float hurtAt = float.NegativeInfinity, shotAt = float.NegativeInfinity;
    private float deathAt = float.NegativeInfinity, landingStrength, hurtDirection;
    private Vector2 shotDirection;
    private float nextEcho;
    private bool ready;
    private int echoIndex;
    private readonly SpriteRenderer[] echoes = new SpriteRenderer[EchoCount];
    private readonly float[] echoBorn = new float[EchoCount];
    public int JumpCount { get; private set; }
    public int LandingCount { get; private set; }
    public int ShotCount { get; private set; }
    public int HurtCount { get; private set; }

    public void Initialize(PlayerController owner, SpriteRenderer artwork, Rigidbody2D rigidbody, Collider2D collider)
    {
        if (ready || artwork == null || artwork.transform == owner.transform) return;
        player = owner; artworkRenderer = artwork; body = rigidbody; visual = artwork.transform;
        basePosition = visual.localPosition; baseScale = visual.localScale; baseRotation = visual.localRotation;
        Vector3 feet = collider != null
            ? new Vector3(collider.bounds.center.x, collider.bounds.min.y, visual.position.z)
            : owner.transform.position + Vector3.down * .9f;
        footAnchor = visual.parent.InverseTransformPoint(feet);
        animator = visual.GetComponent<Animator>();
        if (animator != null) originalUpdateMode = animator.updateMode;
        ready = true;
    }

    public void Jump() { jumpAt = Time.time; JumpCount++; }
    public void Land(float speed)
    {
        // Ignore spawn settling and very small ground contact jitter.
        if (speed < 1f) return;
        landAt = Time.time; landingStrength = Mathf.InverseLerp(1f, 8f, speed); LandingCount++;
    }
    public void Shoot(Vector2 direction) { shotAt = Time.time; shotDirection = direction; ShotCount++; }
    public void Dash() { nextEcho = 0f; }
    public void Hurt(Vector2 source)
    {
        hurtAt = Time.time;
        hurtDirection = Mathf.Sign(transform.position.x - source.x);
        if (hurtDirection == 0f && player != null) hurtDirection = -player.FaceDir;
        HurtCount++;
        Camera camera = Camera.main;
        if (camera != null) camera.GetComponent<PlayerCameraFollow>()?.Shake(.07f, .2f);
    }
    public void Die()
    {
        deathAt = Time.unscaledTime;
        // The death menu pauses gameplay, while the final pose still gets to settle.
        if (animator != null) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    private static float Spring(float elapsed, float decay, float frequency)
        => elapsed < 0f || elapsed > .65f ? 0f : Mathf.Exp(-elapsed * decay) * Mathf.Cos(elapsed * frequency);

    private void LateUpdate()
    {
        if (!ready || player == null || visual == null) return;
        if (!player.enabled) { ResetVisual(); return; }
        if (player.MenuOpen && !player.IsDead) return;

        float now = Time.time;
        Vector2 stretch = Vector2.one;
        Vector2 offset = Vector2.zero;
        float tilt = 0f;
        if (player.IsDead)
        {
            float settle = Mathf.Clamp01((Time.unscaledTime - deathAt) / .24f);
            stretch = new Vector2(Mathf.Lerp(1.08f, 1f, settle), Mathf.Lerp(.88f, 1f, settle));
        }
        else
        {
            bool airborne = player.Machine.CurrentState == player.JumpState || player.Machine.CurrentState == player.FallState;
            if (player.IsDashing)
            {
                stretch = new Vector2(1.13f, .88f);
                tilt = -player.FaceDir * 5f;
                if (now >= nextEcho) { EmitEcho(); nextEcho = now + .045f; }
            }
            else if (airborne)
            {
                stretch = new Vector2(.97f, 1.035f);
                tilt = -player.XInput * 2.5f;
            }
            else if (!player.IsHurt)
            {
                float speed = body != null ? Mathf.Abs(body.linearVelocityX) : 0f;
                if (speed > .1f)
                {
                    float stride = Mathf.Sin(now * 18f);
                    stretch += new Vector2(stride * .018f, -stride * .018f);
                    offset.y = Mathf.Abs(stride) * .012f;
                    tilt = -player.XInput * 2f;
                }
                else
                {
                    float breath = Mathf.Sin(now * 3.8f) * .009f;
                    stretch += new Vector2(-breath * .55f, breath);
                }
            }
            float jump = Spring(now - jumpAt, 14f, 18f);
            float land = Spring(now - landAt, 12f, 21f) * (.055f + landingStrength * .085f);
            float hurt = Spring(now - hurtAt, 10f, 17f);
            float shot = Spring(now - shotAt, 20f, 20f);
            stretch += new Vector2(-jump * .08f + land + hurt * .08f - shot * .035f,
                jump * .1f - land - hurt * .09f + shot * .025f);
            offset += -shotDirection * shot * .035f;
            tilt += -hurtDirection * hurt * 6f;
        }
        stretch.x = Mathf.Clamp(stretch.x, .82f, 1.2f);
        stretch.y = Mathf.Clamp(stretch.y, .8f, 1.2f);
        Quaternion rotation = Quaternion.Euler(0f, 0f, tilt);
        Vector3 multiplier = new Vector3(stretch.x, stretch.y, 1f);
        // Scale and rotate around the original feet, avoiding the floating look of center-pivot squash.
        visual.localScale = Vector3.Scale(baseScale, multiplier);
        visual.localRotation = rotation * baseRotation;
        visual.localPosition = footAnchor + rotation * Vector3.Scale(basePosition - footAnchor, multiplier)
            + new Vector3(offset.x, offset.y, 0f);
        UpdateEchoes();
    }

    private void EmitEcho()
    {
        if (artworkRenderer.sprite == null) return;
        SpriteRenderer echo = echoes[echoIndex];
        if (echo == null)
        {
            GameObject image = new GameObject("Player Dash Echo") { hideFlags = HideFlags.DontSave };
            echo = image.AddComponent<SpriteRenderer>(); echoes[echoIndex] = echo;
        }
        echo.sprite = artworkRenderer.sprite; echo.sharedMaterial = artworkRenderer.sharedMaterial;
        echo.sortingLayerID = artworkRenderer.sortingLayerID; echo.sortingOrder = artworkRenderer.sortingOrder - 1;
        echo.flipX = artworkRenderer.flipX; echo.flipY = artworkRenderer.flipY;
        echo.transform.SetPositionAndRotation(visual.position, visual.rotation);
        echo.transform.localScale = visual.lossyScale;
        echo.color = new Color(.58f, 1f, .83f, .24f); echo.enabled = true;
        echoBorn[echoIndex] = Time.time; echoIndex = (echoIndex + 1) % EchoCount;
    }
    private void UpdateEchoes()
    {
        for (int i = 0; i < echoes.Length; i++)
        {
            SpriteRenderer echo = echoes[i];
            if (echo == null || !echo.enabled) continue;
            float progress = Mathf.Clamp01((Time.time - echoBorn[i]) / .18f);
            Color color = echo.color; color.a = (1f - progress) * .24f; echo.color = color;
            if (progress >= 1f) echo.enabled = false;
        }
    }
    private void ResetVisual()
    {
        if (!ready || visual == null) return;
        visual.localPosition = basePosition; visual.localScale = baseScale; visual.localRotation = baseRotation;
        if (animator != null) animator.updateMode = originalUpdateMode;
        foreach (SpriteRenderer echo in echoes) if (echo != null) echo.enabled = false;
    }
    private void OnDisable() => ResetVisual();
    private void OnDestroy()
    {
        foreach (SpriteRenderer echo in echoes) if (echo != null) Destroy(echo.gameObject);
    }
}
