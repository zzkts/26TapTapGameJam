using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Weapon), typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IActor
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private PlayerData playerData;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Weapon weapon;
    [SerializeField] private Transform attackTransform;
    [SerializeField] private Transform footBottom;
    [SerializeField] private BoxCollider2D bodyCollider;
    [SerializeField] private float checkGroundDis = 0.08f;
    [SerializeField] private LayerMask checkGroundLM = (1 << 0) | (1 << 3) | (1 << 9);
    public float moveSpeed = 3f;
    public float jumpVelocity = 4f;
    public float attackDuration = 0.15f;
    public float rollSpeedMultiplier = 1.5f;
    public float rollCooldown = 2f;
    public float nextRollTime;
    public float rollDuration = 0.2f;
    public float hp;
    public float maxHp = 10f;
    public float hurtDuration = 0.25f;

    public float XInput { get; private set; }
    public float FaceDir { get; private set; } = 1f;
    public float YVelocity => rb.linearVelocityY;
    public bool JumpPressed { get; private set; }
    public bool DownPlatformPressed { get; private set; }
    public bool RollPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool DashUnlocked { get; private set; }
    public bool IsDead => hp <= 0f;
    public bool MenuOpen { get; private set; }
    public bool IsDashing => Machine?.CurrentState == RollState;
    public bool IsHurt => Machine?.CurrentState == HurtState;
    public bool IsInvulnerable => IsDashing || Time.time < invulnerableUntil;
    public float PoisonResistance => playerData != null ? playerData.poisonResistance : 0f;
    public float DashCooldownRemaining => Mathf.Max(0f, nextRollTime - Time.time);
    public Vector2 AimPosition { get; private set; }
    public Weapon Weapon => weapon;
    public event Action HealthChanged;
    public event Action Died;

    public StateMachine Machine { get; private set; }
    public PlayerIdleState IdleState { get; private set; }
    public PlayerWalkState WalkState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerFallState FallState { get; private set; }
    public PlayerRollState RollState { get; private set; }
    public PlayerAttackState AttackState { get; private set; }
    public PlayerTimedState HurtState { get; private set; }
    public PlayerTimedState LandState { get; private set; }
    public PlayerTimedState DeadState { get; private set; }

    private InputActionAsset ownedInput;
    private InputActionMap actions;
    private Camera mainCamera;
    private GeneChainController genes;
    private Color originalColor;
    private float groundedUntil = float.NegativeInfinity;
    private float jumpBufferedUntil = float.NegativeInfinity;
    private float ignoreGroundUntil;
    private float invulnerableUntil;
    private float originalGravity;
    private int normalLayer;
    private int invulnerableLayer;
    private bool hitStopped;
    private bool wasGrounded;
    private bool ownsFallbackData;
    private bool initialized;
    private float attackVisualUntil;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[16];

    private void Awake()
    {
        rb = rb != null ? rb : GetComponent<Rigidbody2D>();
        bodyCollider = bodyCollider != null ? bodyCollider : GetComponent<BoxCollider2D>();
        animator = animator != null ? animator : GetComponentInChildren<Animator>();
        sr = sr != null ? sr : GetComponentInChildren<SpriteRenderer>();
        weapon = weapon != null ? weapon : GetComponent<Weapon>();
        genes = GetComponent<GeneChainController>();
        if (playerData == null) playerData = Resources.Load<PlayerData>("SO/Player/DefaultPlayerData");
        if (playerData == null) { playerData = ScriptableObject.CreateInstance<PlayerData>(); ownsFallbackData = true; }
        moveSpeed = playerData.moveSpeed;
        jumpVelocity = playerData.jumpVelocity;
        maxHp = playerData.maxHp;
        hp = maxHp;
        hurtDuration = playerData.hurtTime;
        rollCooldown = playerData.rollCooldown;
        rollDuration = playerData.rollDuration;
        rollSpeedMultiplier = playerData.rollSpeedMultiplier;
        DashUnlocked = playerData.dashUnlocked;
        originalGravity = rb.gravityScale;
        rb.freezeRotation = true;
        normalLayer = gameObject.layer;
        invulnerableLayer = LayerMask.NameToLayer("PlayerInvulnerable");
        originalColor = sr != null ? sr.color : Color.white;
        if (inputActions != null)
        {
            ownedInput = Instantiate(inputActions);
            actions = ownedInput.FindActionMap("Player");
        }
        mainCamera = Camera.main;
        AimPosition = (Vector2)transform.position + Vector2.right;
        Machine = new StateMachine();
        IdleState = new PlayerIdleState("Idle", this, animator);
        WalkState = new PlayerWalkState("Walk", this, animator);
        JumpState = new PlayerJumpState("Jump", this, animator);
        FallState = new PlayerFallState("Fall", this, animator);
        RollState = new PlayerRollState("Roll", this, animator);
        AttackState = new PlayerAttackState("Attack", this, animator);
        HurtState = new PlayerTimedState("Hurt", this, hurtDuration);
        LandState = new PlayerTimedState("Land", this, playerData.landingTime);
        DeadState = new PlayerTimedState("Dead", this, 0.7f);
        Flip(playerData.direction >= 0f);
        Machine.Switch(IdleState);
        initialized = true;
    }

    private void OnEnable() => actions?.Enable();
    private void Update()
    {
        UpdateFeedback();
        if (MenuOpen) return;
        if (transform.position.y < playerData.fallDeathY && !IsDead) Die();
        if (IsDead || IsHurt || IsDashing) { Machine.Update(); return; }
        if (attackVisualUntil > 0f && Time.time >= attackVisualUntil)
        {
            PlayAnimation(!IsGrounded() ? (YVelocity > 0f ? "Jump" : "Fall") :
                Mathf.Abs(XInput) > 0.01f ? "Walk" : "Idle");
        }
        ReadInput();
        bool grounded = IsGrounded();
        if (grounded) groundedUntil = Time.time + playerData.coyoteTime;
        if (grounded && !wasGrounded && Machine.CurrentState == FallState) Machine.Switch(LandState);
        wasGrounded = grounded;
        if (RollPressed && TryRoll()) return;
        if (JumpPressed) jumpBufferedUntil = Time.time + playerData.jumpBuffer;
        if (DownPlatformPressed && TryDownPlatform()) jumpBufferedUntil = float.NegativeInfinity;
        else if (Time.time <= jumpBufferedUntil && TryJump()) jumpBufferedUntil = float.NegativeInfinity;
        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (!overUi && AttackPressed) TryAttack();
        if (!overUi && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) genes?.TryActivate();
        Machine.Update();
    }

    private void ReadInput()
    {
        Keyboard kb = Keyboard.current;
        XInput = actions?.FindAction("Move")?.ReadValue<float>() ??
            ((kb != null && kb.dKey.isPressed ? 1f : 0f) - (kb != null && kb.aKey.isPressed ? 1f : 0f));
        JumpPressed = (actions?.FindAction("Jump")?.WasPressedThisFrame() ?? false) ||
            (kb != null && (kb.wKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame));
        DownPlatformPressed = JumpPressed && kb != null && kb.sKey.isPressed;
        RollPressed = (actions?.FindAction("Sprint")?.WasPressedThisFrame() ?? false) ||
            (kb != null && kb.leftShiftKey.wasPressedThisFrame);
        AttackPressed = (actions?.FindAction("Attack")?.IsPressed() ?? false) ||
            (Mouse.current != null && Mouse.current.leftButton.isPressed);
        if (mainCamera == null) mainCamera = Camera.main;
        if (Mouse.current != null && mainCamera != null)
        {
            Vector2 screen = Mouse.current.position.ReadValue();
            AimPosition = mainCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y,
                transform.position.z - mainCamera.transform.position.z));
        }
        if (Mathf.Abs(XInput) > 0.01f) Flip(XInput > 0f);
    }

    private void FixedUpdate() { if (!MenuOpen) Machine?.FixUpdate(); }
    public void UpdateLocomotion()
    {
        if (IsDead || IsHurt || IsDashing) return;
        State next = !IsGrounded() ? (YVelocity > 0.05f ? JumpState : FallState) :
            (Mathf.Abs(XInput) > 0.01f ? WalkState : IdleState);
        Machine.Switch(next);
    }

    public void PlayAnimation(string state)
    {
        if (state != "Attack") attackVisualUntil = 0f;
        if (animator == null || animator.runtimeAnimatorController == null) return;
        string name = state;
        if (state == "Walk" && !animator.HasState(0, Animator.StringToHash(name))) name = "PlayerRun";
        if ((state == "Fall" || state == "Roll") && !animator.HasState(0, Animator.StringToHash(name))) name = "Jump";
        if (!animator.HasState(0, Animator.StringToHash(name))) name = "Idle";
        if (animator.HasState(0, Animator.StringToHash(name))) animator.Play(name, 0, 0f);
    }

    public void Flip(bool isRight) { if (sr != null) sr.flipX = !isRight; FaceDir = isRight ? 1f : -1f; }
    public void Move(float direction, float speed) => rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocityY);
    public void FrzeeHorizontalVelocity() => Move(0f, 0f);
    public bool TryJump()
    {
        if (IsDead || MenuOpen || IsDashing || IsHurt || Time.time < ignoreGroundUntil) return false;
        if (!IsGrounded() && Time.time > groundedUntil) return false;
        groundedUntil = float.NegativeInfinity;
        ignoreGroundUntil = Time.time + 0.08f;
        rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpVelocity);
        Machine.Switch(JumpState);
        return true;
    }

    public bool TryDownPlatform()
    {
        RaycastHit2D hit = GetGroundHit();
        if (!hit || !hit.collider.TryGetComponent(out OneWayPlatform platform)) return false;
        platform.FallDown(bodyCollider.transform);
        ignoreGroundUntil = Time.time + 0.25f;
        groundedUntil = float.NegativeInfinity;
        rb.linearVelocity = new Vector2(rb.linearVelocityX, -1f);
        Machine.Switch(FallState);
        return true;
    }

    public bool TryAttack()
    {
        if (IsDead || MenuOpen || IsDashing || IsHurt || !weapon.CanAttack()) return false;
        Vector2 center = bodyCollider != null ? bodyCollider.bounds.center : transform.position;
        Vector2 direction = (AimPosition - center).normalized;
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.right * FaceDir;
        Vector2 origin = attackTransform != null ? (Vector2)attackTransform.position : center + Vector2.up * 0.2f;
        weapon.Attack(origin, direction);
        PlayAnimation("Attack");
        attackVisualUntil = Time.time + attackDuration;
        return true;
    }

    public bool TryRoll()
    {
        if (!DashUnlocked || IsDead || MenuOpen || IsHurt || IsDashing || Time.time < nextRollTime) return false;
        Flip(AimPosition.x >= transform.position.x);
        nextRollTime = Time.time + rollCooldown;
        jumpBufferedUntil = float.NegativeInfinity;
        Machine.Switch(RollState);
        UpdateFeedback();
        return true;
    }

    public void BeginDash() { rb.gravityScale = 0f; rb.linearVelocity = Vector2.right * FaceDir * moveSpeed * rollSpeedMultiplier; }
    public void EndDash() { rb.gravityScale = originalGravity; rb.linearVelocity = Vector2.zero; }
    public void UnlockDash() => DashUnlocked = true;
    public void TakeDamage(float damage) => TakeDamageFrom(damage, (Vector2)transform.position - Vector2.right * FaceDir);
    public void TakeDamageFrom(float damage, Vector2 source)
    {
        if (IsDead || MenuOpen || IsInvulnerable || damage <= 0f) return;
        hp = Mathf.Max(0f, hp - damage);
        HealthChanged?.Invoke();
        if (IsDead) { Die(); return; }
        invulnerableUntil = Time.time + playerData.invulnerabilityTime;
        float away = Mathf.Sign(transform.position.x - source.x);
        rb.linearVelocity = new Vector2((away == 0f ? -FaceDir : away) * playerData.knockbackSpeed, 1f);
        Machine.Switch(HurtState);
        UpdateFeedback();
        if (playerData.hitStopTime > 0f) StartCoroutine(HitStop());
    }

    public float Heal(float amount)
    {
        if (IsDead || amount <= 0f) return 0f;
        float previous = hp;
        hp = Mathf.Min(maxHp, hp + amount);
        if (hp != previous) HealthChanged?.Invoke();
        return hp - previous;
    }
    private IEnumerator HitStop()
    {
        hitStopped = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(playerData.hitStopTime);
        hitStopped = false;
        Time.timeScale = MenuOpen ? 0f : 1f;
    }
    private void Die()
    {
        hp = 0f;
        rb.linearVelocity = Vector2.zero;
        Machine.Switch(DeadState);
        HealthChanged?.Invoke();
        Died?.Invoke();
    }
    public void SetMenuOpen(bool value)
    {
        MenuOpen = value;
        XInput = 0f;
        if (value) rb.linearVelocity = Vector2.zero;
        Time.timeScale = value || hitStopped ? 0f : 1f;
    }
    private void UpdateFeedback()
    {
        int layer = IsInvulnerable && !IsDead && invulnerableLayer >= 0 ? invulnerableLayer : normalLayer;
        if (gameObject.layer != layer) gameObject.layer = layer;
        if (sr != null)
        {
            Color color = originalColor;
            if (IsInvulnerable && !IsDead) color.a *= Mathf.PingPong(Time.unscaledTime * 8f, 0.65f) + 0.35f;
            sr.color = color;
        }
    }
    public bool IsGrounded() => Time.time >= ignoreGroundUntil && YVelocity <= 0.1f && GetGroundHit().collider != null;
    private RaycastHit2D GetGroundHit()
    {
        if (bodyCollider == null) return default;
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(checkGroundLM);
        int count = bodyCollider.Cast(Vector2.down, filter, groundHits, checkGroundDis);
        for (int i = 0; i < count; i++)
            if (groundHits[i].normal.y > 0.4f && groundHits[i].collider != bodyCollider &&
                groundHits[i].collider.GetComponentInParent<EnemyAIController>() == null &&
                !Physics2D.GetIgnoreCollision(bodyCollider, groundHits[i].collider)) return groundHits[i];
        return default;
    }
    private void OnDisable()
    {
        if (!initialized) return;
        actions?.Disable();
        StopAllCoroutines();
        if (MenuOpen || hitStopped) Time.timeScale = 1f;
        MenuOpen = hitStopped = false;
        if (rb != null) rb.gravityScale = originalGravity;
        gameObject.layer = normalLayer;
        if (sr != null) sr.color = originalColor;
    }
    private void OnDestroy() { if (ownedInput != null) Destroy(ownedInput); if (ownsFallbackData) Destroy(playerData); }
}

