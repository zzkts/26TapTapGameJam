using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Weapon), typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, ICombatable
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private PlayerData playerData;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Weapon weapon;
    [SerializeField] private GeneInventory geneInventory;
    [SerializeField] private Transform attackTransform;
    [SerializeField] private Transform footBottom;
    [SerializeField] private BoxCollider2D bodyCollider;
    [SerializeField] private float checkGroundDis = 0.08f;
    [SerializeField] private LayerMask checkGroundLM = (1 << 3) | (1 << 9);
    public float moveSpeed = 3f, jumpVelocity = 4f, attackDuration = 0.15f;
    public float rollSpeedMultiplier = 1.5f, rollCooldown = 2f, nextRollTime, rollDuration = 0.2f;
    public float hp, maxHp = 10f;
    public float XInput { get; private set; }
    public float FaceDir { get; private set; } = 1f;
    public float YVelocity => rb != null ? rb.linearVelocityY : 0f;
    public bool JumpPressed { get; private set; }
    public bool DownPlatformPressed { get; private set; }
    public bool RollPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public float Hp => hp;
    public float MaxHp => maxHp;
    public bool IsDead => hp <= 0f;
    public bool IsDashing => Machine != null && Machine.CurrentState == RollState;
    public bool IsHurt => Machine != null && Machine.CurrentState == HurtState;
    public bool IsInvulnerable => forcedInvulnerable || IsDashing || Time.time < invulnerableUntil;
    public bool MenuOpen { get; private set; }
    public bool DashUnlocked { get; private set; }
    public float DashCooldownRemaining => Mathf.Max(0f, nextRollTime - Time.time);
    public float PoisonResistance => playerData != null ? playerData.poisonResistance : 0f;
    public Vector2 AimPosition { get; private set; }
    public Weapon Weapon => weapon;
    public GeneInventory Genes => geneInventory;
    public PlayerPresentation Presentation { get; private set; }
    public float CoyoteTime => playerData != null ? playerData.coyoteTime : .22f;
    public float JumpBufferTime => playerData != null ? playerData.jumpBuffer : .22f;
    public bool IsShooting => shotVisualUntil > Time.time;
    public IReadOnlyList<BuffInstance> BuffList => buffs.BuffList;
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
    private readonly BuffComponent buffs = new();
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[24];
    private float groundedUntil = float.NegativeInfinity, bufferedUntil = float.NegativeInfinity;
    private float ignoreGroundUntil, invulnerableUntil, originalGravity, shotVisualUntil;
    private int normalLayer, invulnerableLayer;
    private Color originalColor;
    private string locomotionAnimation = "Idle";
    private PhysicsMaterial2D movementMaterial;
    private float fallingSpeed;
    private readonly Dictionary<string, float> animationDurations = new();
    private bool initialized, wasGrounded, forcedInvulnerable, hitStopped, deathSignaled, ownsData;
    private bool inputJump;
    private void Awake()
    {
        rb = rb != null ? rb : GetComponent<Rigidbody2D>();
        bodyCollider = bodyCollider != null ? bodyCollider : GetComponent<BoxCollider2D>();
        if (bodyCollider != null)
        {
            // Horizontal input keeps pushing into walls; friction must not cancel a jump or fall.
            movementMaterial = new PhysicsMaterial2D("Player Movement")
            { hideFlags = HideFlags.DontSave, friction = 0f, bounciness = 0f };
            bodyCollider.sharedMaterial = movementMaterial;
        }
        animator = animator != null ? animator : GetComponentInChildren<Animator>();
        sr = sr != null ? sr : GetComponentInChildren<SpriteRenderer>();
        weapon = weapon != null ? weapon : GetComponent<Weapon>();
        geneInventory = geneInventory != null ? geneInventory : GetComponent<GeneInventory>();
        if (playerData == null) playerData = Resources.Load<PlayerData>("SO/Player/DefaultPlayerData");
        if (playerData == null) { playerData = ScriptableObject.CreateInstance<PlayerData>(); ownsData = true; }
        moveSpeed = playerData.moveSpeed; jumpVelocity = playerData.jumpVelocity; maxHp = playerData.maxHp;
        rollCooldown = playerData.rollCooldown; rollDuration = playerData.rollDuration;
        rollSpeedMultiplier = playerData.rollSpeedMultiplier; DashUnlocked = playerData.dashUnlocked;
        hp = maxHp;
        originalGravity = rb.gravityScale;
        rb.freezeRotation = true; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        normalLayer = gameObject.layer; invulnerableLayer = LayerMask.NameToLayer("PlayerInvulnerable");
        originalColor = sr != null ? sr.color : Color.white;
        if (animator != null && animator.runtimeAnimatorController != null)
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                animationDurations[clip.name] = clip.length;
        Presentation = GetComponent<PlayerPresentation>();
        if (Presentation == null) Presentation = gameObject.AddComponent<PlayerPresentation>();
        Presentation.Initialize(this, sr, rb, bodyCollider);
        if (inputActions != null) { ownedInput = Instantiate(inputActions); actions = ownedInput.FindActionMap("Player"); }
        mainCamera = Camera.main; AimPosition = (Vector2)transform.position + Vector2.right;
        Machine = new StateMachine();
        IdleState = new PlayerIdleState("Idle", this, animator);
        WalkState = new PlayerWalkState("move", this, animator);
        JumpState = new PlayerJumpState("Jump", this, animator);
        FallState = new PlayerFallState("Fall", this, animator);
        RollState = new PlayerRollState("Roll", this, animator);
        AttackState = new PlayerAttackState("Attack", this, animator);
        HurtState = new PlayerTimedState("Hurt", this, playerData.hurtTime);
        LandState = new PlayerTimedState("Land", this, playerData.landingTime);
        DeadState = new PlayerTimedState("Dead", this, 0.7f);
        Flip(playerData.direction >= 0f); initialized = true;
        Machine.Switch(IdleState);
        if (GetComponent<ActorElementEffects>() == null) gameObject.AddComponent<ActorElementEffects>();
    }
    private void OnEnable() => actions?.Enable();
    private void Update()
    {
        if (!initialized) return;
        buffs.Update(Time.deltaTime); UpdateFeedback();
        if (MenuOpen) return;
        if (transform.position.y < playerData.fallDeathY && !IsDead) Die();
        if (IsDead || IsDashing) { Machine.Update(); return; }
        ReadInput();
        // A press near the end of a short hurt reaction is retained until control returns.
        if (JumpPressed) bufferedUntil = Time.time + playerData.jumpBuffer;
        if (IsHurt) { Machine.Update(); return; }
        bool grounded = IsGrounded();
        if (grounded) groundedUntil = Time.time + playerData.coyoteTime;
        if (!grounded) fallingSpeed = Mathf.Max(fallingSpeed, -YVelocity);
        if (grounded && !wasGrounded)
        {
            Presentation?.Land(fallingSpeed);
            fallingSpeed = 0f;
            if (Machine.CurrentState == FallState) Machine.Switch(LandState);
        }
        wasGrounded = grounded;
        if (grounded) { inputJump = false; }
        Machine.Update();
        // The touch world surface excludes UI buttons and owns its own pointer.
        bool overUi = !MobileGameInput.Enabled && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (!overUi && AttackPressed) TryAttack();
        if (MobileGameInput.Enabled ? MobileGameInput.Consume(MobileGameInput.Command.Enchant) :
            !overUi && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            GetComponent<GeneChainController>()?.TryActivate();
        if (shotVisualUntil > 0f && Time.time >= shotVisualUntil)
        {
            shotVisualUntil = 0f;
            PlayAnimationImmediate(locomotionAnimation);
        }
    }
    private void ReadInput()
    {
        if (MobileGameInput.Enabled)
        {
            XInput = MobileGameInput.Move.x;
            JumpPressed = MobileGameInput.Consume(MobileGameInput.Command.Jump);
            DownPlatformPressed = MobileGameInput.Consume(MobileGameInput.Command.Drop) || JumpPressed && MobileGameInput.Move.y < -.65f;
            RollPressed = MobileGameInput.Consume(MobileGameInput.Command.Dash);
            AttackPressed = MobileGameInput.ConsumeFireRequest() || MobileGameInput.Firing;
            JumpHeld = MobileGameInput.JumpHeld;
            Vector2 center = bodyCollider != null ? bodyCollider.bounds.center : transform.position;
            Vector2 direction = Vector2.right * (Mathf.Abs(XInput) > .12f ? Mathf.Sign(XInput) : FaceDir);
            AimPosition = center + direction * 8f;
            if (AttackPressed)
            {
                if (mainCamera == null) mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    var point = MobileGameInput.AimScreenPosition;
                    AimPosition = mainCamera.ScreenToWorldPoint(new Vector3(point.x, point.y,
                        transform.position.z - mainCamera.transform.position.z));
                    direction = AimPosition - center;
                    MobileGameInput.SetAimDirection(direction);
                }
            }
            if (Mathf.Abs(direction.x) > .05f) Flip(direction.x > 0);
            return;
        }
        Keyboard keys = Keyboard.current;
        XInput = actions?.FindAction("Move")?.ReadValue<float>() ??
            ((keys != null && keys.dKey.isPressed ? 1f : 0f) - (keys != null && keys.aKey.isPressed ? 1f : 0f));
        JumpPressed = (actions?.FindAction("Jump")?.WasPressedThisFrame() ?? false) ||
            (keys != null && (keys.wKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame));
        DownPlatformPressed = JumpPressed && keys != null && keys.sKey.isPressed;
        RollPressed = (actions?.FindAction("Sprint")?.WasPressedThisFrame() ?? false) ||
            (keys != null && keys.leftShiftKey.wasPressedThisFrame);
        AttackPressed = (actions?.FindAction("Attack")?.IsPressed() ?? false) ||
            (Mouse.current != null && Mouse.current.leftButton.isPressed);
        JumpHeld = keys != null && (keys.spaceKey.isPressed || keys.wKey.isPressed);
        if (mainCamera == null) mainCamera = Camera.main;
        if (Mouse.current != null && mainCamera != null)
            AimPosition = mainCamera.ScreenToWorldPoint(new Vector3(Mouse.current.position.x.ReadValue(),
                Mouse.current.position.y.ReadValue(), transform.position.z - mainCamera.transform.position.z));
        float aim = AimPosition.x - transform.position.x;
        if (Mathf.Abs(aim) > 0.05f) Flip(aim > 0f);
    }
    private void FixedUpdate()
    {
        if (!initialized || MenuOpen) return;
        Machine.FixUpdate();
        if (IsDashing) return;
        float multiplier = !IsDead && !IsHurt
            ? YVelocity < -.1f ? playerData.fallingGravity : inputJump && JumpHeld && YVelocity > .1f ? playerData.heldJumpGravity : 1f
            : 1f;
        rb.gravityScale = originalGravity * multiplier;
    }
    public bool ProcessMovementActions()
    {
        if (RollPressed && TryRoll()) { Machine.Switch(RollState); return true; }
        if (DownPlatformPressed && TryDownPlatform()) { bufferedUntil = float.NegativeInfinity; Machine.Switch(FallState); return true; }
        if (Time.time <= bufferedUntil && TryJump()) { bufferedUntil = float.NegativeInfinity; Machine.Switch(JumpState); return true; }
        return false;
    }
    public void PlayAnimation(string state)
    {
        locomotionAnimation = state;
        // Shooting never owns movement. Jump, dash and damage still show their own poses.
        if (state == "Dead" || state == "Hurt" || state == "Roll" || state == "Jump" || state == "Fall")
            shotVisualUntil = 0f;
        else if (IsShooting && state != "Attack") return;
        PlayAnimationImmediate(state);
    }
    private void PlayAnimationImmediate(string state)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        string name = state;
        if (!animator.HasState(0, Animator.StringToHash(name)))
            name = state == "Roll" || state == "Fall" ? "Jump" : "Idle";
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.type == AnimatorControllerParameterType.Bool) animator.SetBool(parameter.name, false);
        if (animator.HasState(0, Animator.StringToHash(name))) animator.Play(name, 0, 0f);
    }
    public void Flip(bool right) { if (sr != null) sr.flipX = !right; FaceDir = right ? 1f : -1f; }
    public void Move(float direction, float speed) { if (rb != null && !IsDead) rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocityY); }
    public void FrzeeHorizontalVelocity() => Move(0f, 0f);
    public bool TryJump()
    {
        if (IsDead || MenuOpen || IsDashing || IsHurt || Time.time < ignoreGroundUntil) return false;
        if (!IsGrounded() && Time.time > groundedUntil) return false;
        groundedUntil = float.NegativeInfinity; ignoreGroundUntil = Time.time + 0.08f;
        bufferedUntil = float.NegativeInfinity; inputJump = true;
        rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpVelocity);
        Presentation?.Jump();
        return true;
    }
    public bool TryDownPlatform()
    {
        if (IsDead || MenuOpen || IsDashing || IsHurt) return false;
        RaycastHit2D hit = GetGroundHit();
        if (!hit || !hit.collider.TryGetComponent(out OneWayPlatform platform)) return false;
        platform.FallDown(bodyCollider.transform);
        ignoreGroundUntil = Time.time + 0.25f; groundedUntil = float.NegativeInfinity;
        rb.linearVelocity = new Vector2(rb.linearVelocityX, -1f);
        return true;
    }
    public bool TryAttack()
    {
        if (IsDead || MenuOpen || IsDashing || IsHurt || weapon == null || !weapon.CanAttack()) return false;
        Vector2 center = bodyCollider != null ? bodyCollider.bounds.center : transform.position;
        Vector2 direction = (AimPosition - center).normalized;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.right * FaceDir;
        if (attackTransform != null)
        {
            Vector3 local = attackTransform.localPosition; local.x = Mathf.Abs(local.x) * FaceDir;
            attackTransform.localPosition = local;
        }
        Vector2 origin = attackTransform != null ? attackTransform.position : center;
        weapon.Attack(origin, direction);
        Presentation?.Shoot(direction);
        if (!IsShooting && IsGrounded())
        {
            PlayAnimationImmediate("Attack");
            float visualDuration = animationDurations.TryGetValue("Attack", out float duration) ? duration : attackDuration;
            shotVisualUntil = Time.time + Mathf.Max(attackDuration, visualDuration);
        }
        return true;
    }
    public bool TryRoll()
    {
        if (!DashUnlocked || IsDead || MenuOpen || IsHurt || IsDashing || Time.time < nextRollTime) return false;
        Flip(AimPosition.x >= transform.position.x);
        nextRollTime = Time.time + rollCooldown; bufferedUntil = float.NegativeInfinity;
        return true;
    }
    public void BeginDash()
    {
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.right * FaceDir * (moveSpeed * rollSpeedMultiplier);
        Presentation?.Dash();
        UpdateFeedback();
    }
    public void EndDash()
    {
        rb.gravityScale = originalGravity; rb.linearVelocity = Vector2.zero;
        // StateMachine calls Exit before replacing CurrentState; exclude the outgoing dash here.
        UpdateFeedback(false);
    }
    public void UnlockDash() => DashUnlocked = true;

    public void SetInvincible(bool value) { forcedInvulnerable = value; UpdateFeedback(); }
    public void TakeDamage(float damage) => TakeDamageFrom(damage, (Vector2)transform.position - Vector2.right * FaceDir);
    public void TakeDamage(DamageInfo info)
    {
        if (IsDead || MenuOpen || IsInvulnerable || info.damage <= 0f) return;
        ApplyDamage(info.damage, info.hitPoint);
        if (!IsDead && info.elementType != E_ElementType.None)
            ElementSystem.Apply(buffs, info.elementType, transform.position, info.damage, info.targetLM);
    }
    public void TakeDamageFrom(float damage, Vector2 source)
    {
        if (IsDead || MenuOpen || IsInvulnerable || damage <= 0f) return;
        ApplyDamage(damage, source);
    }
    private void ApplyDamage(float damage, Vector2 source)
    {
        hp = Mathf.Max(0f, hp - damage); HealthChanged?.Invoke();
        GetComponent<ActorElementEffects>()?.Flash();
        Presentation?.Hurt(source);
        if (IsDead) { Die(); return; }
        float away = Mathf.Sign(transform.position.x - source.x);
        rb.linearVelocity = new Vector2((away == 0f ? -FaceDir : away) * playerData.knockbackSpeed, 1f);
        invulnerableUntil = Time.time + playerData.invulnerabilityTime;
        Machine?.Switch(HurtState); UpdateFeedback();
        if (Application.isPlaying && playerData.hitStopTime > 0f && !hitStopped) StartCoroutine(HitStop());
    }
    private IEnumerator HitStop()
    {
        hitStopped = true; Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(playerData.hitStopTime);
        hitStopped = false; Time.timeScale = MenuOpen ? 0f : 1f;
    }
    public float Heal(float amount)
    {
        if (IsDead || amount <= 0f) return 0f;
        float previous = hp; hp = Mathf.Min(maxHp, hp + amount);
        if (previous != hp) HealthChanged?.Invoke();
        return hp - previous;
    }
    public float IncreaseHp(float amount) => Heal(amount);
    public void AddBuff(E_ElementType element) => ElementSystem.Apply(buffs, element, transform.position,
        0f, LayerMask.GetMask("Player", "PlayerInvulnerable"));
    public void RemBuff(E_ElementType element) => buffs.Remove(element);
    private void Die()
    {
        if (deathSignaled) return;
        deathSignaled = true; hp = 0f;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        Presentation?.Die();
        Machine?.Switch(DeadState); HealthChanged?.Invoke(); Died?.Invoke();
    }
    public void SetMenuOpen(bool value)
    {
        if (MobileGameInput.Enabled) { MobileGameInput.Reset(); MobileControlsUI.Current?.SetSuspended(value); }
        GameplayUIFactory.ModalInputFrame = Time.frameCount;
        MenuOpen = value; XInput = 0f;
        if (value && rb != null) rb.linearVelocity = Vector2.zero;
        if (value) { bufferedUntil = groundedUntil = float.NegativeInfinity; JumpHeld = false; }
        Time.timeScale = value || hitStopped ? 0f : 1f;
    }
    private void UpdateFeedback(bool includeDash = true)
    {
        if (!initialized) return;
        bool dashing = includeDash && IsDashing;
        bool invulnerable = forcedInvulnerable || dashing || Time.time < invulnerableUntil;
        gameObject.layer = invulnerable && !IsDead && invulnerableLayer >= 0 ? invulnerableLayer : normalLayer;
        if (sr != null)
        {
            Color color = originalColor;
            if (invulnerable && !IsDead && !dashing)
                color.a *= Mathf.PingPong(Time.unscaledTime * 8f, 0.45f) + 0.55f;
            sr.color = color;
        }
    }
    public bool IsGrounded() => Time.time >= ignoreGroundUntil && YVelocity <= 0.1f && GetGroundHit().collider != null;
    private RaycastHit2D GetGroundHit()
    {
        if (bodyCollider == null) return default;
        ContactFilter2D filter = new() { useTriggers = false }; filter.SetLayerMask(checkGroundLM);
        int count = bodyCollider.Cast(Vector2.down, filter, groundHits, checkGroundDis);
        for (int i = 0; i < count; i++)
        {
            if(groundHits[i].collider!=null&&groundHits[i].collider.GetComponent<OneWayPlatform>()!=null&&
                groundHits[i].collider.bounds.max.y>bodyCollider.bounds.min.y+.12f)continue;
            if (groundHits[i].normal.y > 0.4f && groundHits[i].collider != bodyCollider &&
                groundHits[i].collider.GetComponentInParent<EnemyAIController>() == null &&
                !Physics2D.GetIgnoreCollision(bodyCollider, groundHits[i].collider)) return groundHits[i];
        }
        return default;
    }
    private void OnDisable()
    {
        if (MobileGameInput.Enabled) MobileGameInput.Reset();
        if (!initialized) return;
        actions?.Disable(); StopAllCoroutines();
        if (MenuOpen || hitStopped) Time.timeScale = 1f;
        MenuOpen = hitStopped = false;
        shotVisualUntil = 0f;
        rb.gravityScale = originalGravity; gameObject.layer = normalLayer;
        if (sr != null) sr.color = originalColor;
    }
    private void OnDestroy()
    {
        if (ownedInput != null) Destroy(ownedInput);
        if (ownsData) Destroy(playerData);
        if (movementMaterial != null)
        {
            if (Application.isPlaying) Destroy(movementMaterial);
            else DestroyImmediate(movementMaterial);
        }
    }
}
