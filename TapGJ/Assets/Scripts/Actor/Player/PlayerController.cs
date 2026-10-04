using cfg;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Weapon))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IActor
{
    public float XInput { get; private set; }
    public float FaceDir { get; private set; } = 1f;
    public float YVelocity => rb.linearVelocityY;

    public bool JumpPressed { get; private set; }
    public bool DownPlatformPressed { get; private set; }
    public bool RollPressed { get; private set; }
    public bool AttackPressed { get; private set; }

    #region FSM
    public StateMachine Machine { get; private set; }
    public PlayerIdleState IdleState { get; private set; }
    public PlayerWalkState WalkState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerFallState FallState { get; private set; }
    public PlayerRollState RollState { get; private set; }
    public PlayerAttackState AttackState { get; private set; }
    #endregion

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Data")]
    [SerializeField] private PlayerData playerData;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Weapon weapon;
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
    public float nextRollTime = 0f;
    public float rollDuration = 0.5f;

    [Header("Health")]
    public float hp;
    public float maxHp = 10f;
    public float hurtDuration = 0.3f;

    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction downPlatformAction;
    private InputAction rollAction;
    private InputAction attackAction;

    private Camera mainCamera;
    private Vector2 mouseWorldPosition;


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
        IdleState = new PlayerIdleState("Idle", this, animator);
        WalkState = new PlayerWalkState("Walk", this, animator);
        JumpState = new PlayerJumpState("Jump", this, animator);
        FallState = new PlayerFallState("Fall", this, animator);
        RollState = new PlayerRollState("Roll", this, animator);
        AttackState = new PlayerAttackState("Attack", this, animator);

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
        Machine.FixUpdate();
    }

    private void Update()
    {
        XInput = moveAction.ReadValue<float>();
        JumpPressed = jumpAction.WasPressedThisFrame();
        DownPlatformPressed = downPlatformAction.WasPressedThisFrame();
        RollPressed = rollAction.WasPressedThisFrame();
        AttackPressed = attackAction.WasPressedThisFrame();

        // 实时计算鼠标世界空间下位置
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        mouseWorldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, transform.position.z - mainCamera.transform.position.z));

        float mouseHorizontalDir = mouseWorldPosition.x - transform.position.x;
        if (Mathf.Abs(mouseHorizontalDir) > 0.01f) Flip(mouseHorizontalDir > 0f);

        Machine.Update();
    }

    #region State Provide
    public void Flip(bool isRight)
    {
        sr.flipX = !isRight;
        FaceDir = isRight ? 1f : -1f;
    }
    public void Move(float xDirection, float speed)
    {
        rb.linearVelocity = new Vector2(xDirection * speed, rb.linearVelocityY);
    }
    public void FrzeeHorizontalVelocity() => rb.linearVelocity = new Vector2(0f, rb.linearVelocityY);
    public bool TryJump()
    {
        if(IsGrounded())
        { 
            rb.linearVelocity = new Vector2(rb.linearVelocityX, jumpVelocity);
            return true;
        }
        return false;
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
        if(weapon.CanAttack())
        {
            Vector3 attackPosition = attackTransform.localPosition;
            attackPosition.x = Mathf.Abs(attackPosition.x) * FaceDir;
            attackTransform.localPosition = attackPosition;



            weapon.Attack(attackTransform.position, (mouseWorldPosition - (Vector2)transform.position).normalized);
            return true;
        }
        return false;
    }
    public bool TryRoll()
    {
        if(Time.time >= nextRollTime)
        {
            nextRollTime = Time.time + rollCooldown;
            return true;
        }
        return false;
    }
    #endregion

    public void TakeDamage(float damage)
    {
        hp = Mathf.Max(0f, hp - damage);
    }

    public bool IsGrounded()
    {
        RaycastHit2D groundHit = GetGroundHit();
        if (!groundHit) return false;

        return !Physics2D.GetIgnoreCollision(bodyCollider, groundHit.collider);
    }

    private RaycastHit2D GetGroundHit() =>
        Physics2D.Raycast(footBottom.position, Vector2.down, checkGroundDis, checkGroundLM);

    private void OnDisable()
    {
        playerActionMap.Disable();
    }

}

