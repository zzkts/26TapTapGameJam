using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Weapon))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float XInput { get; private set; }
    public float FaceDir { get; private set; } = 1f;
    public float YVelocity => rb.linearVelocityY;

    public bool JumpPressed { get; private set; }
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

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Weapon weapon;
    [SerializeField] private Transform attackTransform;

    [Header("Ground Check")]
    [SerializeField] private Transform footBottom;
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
    private InputAction rollAction;
    private InputAction attackAction;
    private Camera mainCamera;
    private float attackPointX;
    private Vector2 mouseWorldPosition;


    private void Awake()
    {
        mainCamera = Camera.main;
        attackPointX = Mathf.Abs(attackTransform.localPosition.x);

        if (inputActions == null)
        {
            Debug.LogError("PlayerController requires an InputActionAsset.", this);
            enabled = false;
            return;
        }

        playerActionMap = inputActions.FindActionMap("Player", true);
        moveAction = playerActionMap.FindAction("Move", true);
        jumpAction = playerActionMap.FindAction("Jump", true);
        rollAction = playerActionMap.FindAction("Sprint", true);
        attackAction = playerActionMap.FindAction("Attack", true);

        Machine = new StateMachine();
        IdleState = new PlayerIdleState("Idle", this, animator);
        WalkState = new PlayerWalkState("Walk", this, animator);
        JumpState = new PlayerJumpState("Jump", this, animator);
        FallState = new PlayerFallState("Fall", this, animator);
        RollState = new PlayerRollState("Roll", this, animator);
        AttackState = new PlayerAttackState("Attack", this, animator);

        hp = maxHp;
    }

    private void OnEnable()
    {
        playerActionMap?.Enable();
    }

    private void Start()
    {
        Machine.Switch(IdleState);
    }

    private void FixedUpdate()
    {
        Machine.FixUpdate();
    }

    private void Update()
    {
        XInput = moveAction.ReadValue<Vector2>().x;
        JumpPressed = jumpAction.WasPressedThisFrame();
        RollPressed = rollAction.WasPressedThisFrame();
        AttackPressed = attackAction.WasPressedThisFrame();

        UpdateAim();
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
    public bool TryAttack()
    {
        if(weapon.CanAttack())
        {
            Vector3 attackPosition = attackTransform.localPosition;
            attackPosition.x = attackPointX * FaceDir;
            attackTransform.localPosition = attackPosition;

            weapon.Attack(attackTransform.position, GetAimDirection());
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
        // TODO : 受伤处理
    }

    public bool IsGrounded()
    {
        return Physics2D.Raycast(footBottom.position, Vector2.down, checkGroundDis, checkGroundLM);
    }

    private void OnDisable()
    {
        playerActionMap?.Disable();
    }


    private Vector2 GetAimDirection()
    {
        Vector2 direction = mouseWorldPosition - (Vector2)attackTransform.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return Vector2.right * FaceDir;

        return direction.normalized;
    }

    private void UpdateAim()
    {
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        float distanceToWorldPlane = transform.position.z - mainCamera.transform.position.z;
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, distanceToWorldPlane));

        mouseWorldPosition = worldPosition;

        float horizontalAim = mouseWorldPosition.x - transform.position.x;
        if (Mathf.Abs(horizontalAim) > 0.01f)
            Flip(horizontalAim > 0f);
    }
}

