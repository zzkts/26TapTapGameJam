using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家角色控制器。
/// 保存玩家数据、读取输入，并向各个状态提供角色操作接口。
/// </summary>
[RequireComponent(typeof(Weapon), typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, ICombatable
{
    public float XInput { get; private set; }
    public float FaceDir { get; private set; } = 1f;
    public float YVelocity => rb.linearVelocityY;

    public bool JumpPressed { get; private set; }
    public bool DownPlatformPressed { get; private set; }
    public bool RollPressed { get; private set; }
    public bool AttackPressed { get; private set; }

    public StateMachine Machine { get; private set; }
    public PlayerIdleState IdleState { get; private set; }
    public PlayerWalkState WalkState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerFallState FallState { get; private set; }
    public PlayerRollState RollState { get; private set; }
    public PlayerAttackState AttackState { get; private set; }

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Data")]
    [SerializeField] private PlayerData playerData;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Weapon weapon;
    [SerializeField] private GeneInventory geneInventory;
    [SerializeField] private Transform attackTransform;

    [Header("Ground Check")]
    [SerializeField] private Transform footBottom;
    [SerializeField] private BoxCollider2D bodyCollider;
    [SerializeField] private float checkGroundDis = 0.15f;
    [SerializeField] private LayerMask checkGroundLM;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float jumpVelocity = 4f;

    [Header("Attack")]
    public float attackDuration = 0.15f;

    [Header("Roll")]
    public float rollSpeedMultiplier = 1.5f;
    public float rollCooldown = 2f;
    public float nextRollTime;
    public float rollDuration = 0.5f;

    [Header("Health")]
    public float hp;
    public float maxHp = 10f;

    public float Hp => hp;
    public float MaxHp => maxHp;
    public bool IsDead => hp <= 0f;
    public bool IsInvulnerable { get; private set; }
    public bool MenuOpen { get; private set; }
    public bool DashUnlocked => true;
    public float DashCooldownRemaining => Mathf.Max(0f, nextRollTime - Time.time);
    public float PoisonResistance => 0f;
    public Vector2 AimPosition => mouseWorldPosition;
    public Weapon Weapon => weapon;
    public GeneInventory Genes => geneInventory;
    public IReadOnlyList<BuffInstance> BuffList => buffs.BuffList;

    public event Action HealthChanged;
    public event Action Died;

    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction downPlatformAction;
    private InputAction rollAction;
    private InputAction attackAction;

    private Camera mainCamera;
    private Vector2 mouseWorldPosition;
    private readonly BuffComponent buffs = new();

    private void Awake()
    {
        mainCamera = Camera.main;

        playerActionMap = inputActions.FindActionMap("Player", true);
        moveAction = playerActionMap.FindAction("Move", true);
        jumpAction = playerActionMap.FindAction("Jump", true);
        downPlatformAction = playerActionMap.FindAction("DownPlatform", true);
        rollAction = playerActionMap.FindAction("Sprint", true);
        attackAction = playerActionMap.FindAction("Attack", true);

        Machine = new StateMachine();
        IdleState = new PlayerIdleState("idle", this, animator);
        WalkState = new PlayerWalkState("move", this, animator);
        JumpState = new PlayerJumpState("jump", this, animator);
        FallState = new PlayerFallState("fall", this, animator);
        RollState = new PlayerRollState("roll", this, animator);
        AttackState = new PlayerAttackState("atk", this, animator);
    }

    private void OnEnable()
    {
        playerActionMap.Enable();
    }

    private void Start()
    {
        moveSpeed = playerData.moveSpeed;
        Flip(playerData.direction > 0f);
        jumpVelocity = playerData.jumpVelocity;
        maxHp = playerData.maxHp;
        rollCooldown = playerData.rollCooldown;
        rollSpeedMultiplier = playerData.rollSpeedMultiplier;
        rollDuration = playerData.rollDuration;

        hp = maxHp;
        Machine.Switch(IdleState);
    }

    private void FixedUpdate()
    {
        if (!MenuOpen)
            Machine.FixUpdate();
    }

    private void Update()
    {
        buffs.Update(Time.deltaTime);
        if (MenuOpen) return;

        XInput = moveAction.ReadValue<float>();
        JumpPressed = jumpAction.WasPressedThisFrame();
        DownPlatformPressed = downPlatformAction.WasPressedThisFrame();
        RollPressed = rollAction.WasPressedThisFrame();
        AttackPressed = attackAction.WasPressedThisFrame();

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        mouseWorldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPosition.x, mouseScreenPosition.y,
                transform.position.z - mainCamera.transform.position.z));

        float mouseHorizontalDir = mouseWorldPosition.x - transform.position.x;
        if (Mathf.Abs(mouseHorizontalDir) > 0.01f)
            Flip(mouseHorizontalDir > 0f);

        Machine.Update();
    }

    public void Flip(bool isRight)
    {
        sr.flipX = !isRight;
        FaceDir = isRight ? 1f : -1f;
    }

    public void Move(float xDirection, float speed)
    {
        rb.linearVelocity = new Vector2(xDirection * speed, rb.linearVelocityY);
    }

    public void FrzeeHorizontalVelocity()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocityY);
    }

    public bool TryJump()
    {
        if (!IsGrounded()) return false;

        rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpVelocity);
        return true;
    }

    public bool TryDownPlatform()
    {
        RaycastHit2D groundHit = GetGroundHit();
        if (!groundHit) return false;
        if (!groundHit.collider.TryGetComponent(out OneWayPlatform platform)) return false;

        platform.FallDown(bodyCollider.transform);
        return true;
    }

    public bool TryAttack()
    {
        if (!weapon.CanAttack()) return false;

        Vector3 attackPosition = attackTransform.localPosition;
        attackPosition.x = Mathf.Abs(attackPosition.x) * FaceDir;
        attackTransform.localPosition = attackPosition;

        weapon.Attack(attackTransform.position,
            (mouseWorldPosition - (Vector2)transform.position).normalized);
        return true;
    }

    public bool TryRoll()
    {
        if (Time.time < nextRollTime) return false;

        nextRollTime = Time.time + rollCooldown;
        return true;
    }

    public void SetInvincible(bool value)
    {
        IsInvulnerable = value;
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(new DamageInfo(damage));
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead || IsInvulnerable || damageInfo.damage <= 0f) return;

        hp = Mathf.Max(0f, hp - damageInfo.damage);
        HealthChanged?.Invoke();

        if (damageInfo.elementType != E_ElementType.None)
            ElementSystem.Apply(buffs, damageInfo.elementType, damageInfo.hitPoint,
                damageInfo.damage, damageInfo.targetLM);

        if (IsDead)
            Died?.Invoke();
    }

    public void TakeDamageFrom(float damage, Vector2 source)
    {
        TakeDamage(damage);
    }

    public float Heal(float amount)
    {
        if (IsDead || amount <= 0f) return 0f;

        float previousHp = hp;
        hp = Mathf.Min(maxHp, hp + amount);
        if (hp != previousHp)
            HealthChanged?.Invoke();
        return hp - previousHp;
    }

    public float IncreaseHp(float amount)
    {
        return Heal(amount);
    }

    public void AddBuff(E_ElementType elementType)
    {
        ElementSystem.Apply(buffs, elementType, transform.position);
    }

    public void RemBuff(E_ElementType elementType)
    {
        buffs.Remove(elementType);
    }

    public void UnlockDash()
    {
    }

    public void SetMenuOpen(bool value)
    {
        MenuOpen = value;
        Time.timeScale = value ? 0f : 1f;
    }

    public bool IsGrounded()
    {
        RaycastHit2D groundHit = GetGroundHit();
        if (!groundHit) return false;

        return !Physics2D.GetIgnoreCollision(bodyCollider, groundHit.collider);
    }

    private RaycastHit2D GetGroundHit()
    {
        return Physics2D.Raycast(footBottom.position, Vector2.down, checkGroundDis, checkGroundLM);
    }

    private void OnDisable()
    {
        playerActionMap.Disable();
        if (MenuOpen)
            Time.timeScale = 1f;
    }
}
