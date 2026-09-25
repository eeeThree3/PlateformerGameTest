using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMove : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerInputHandler input;
    private PlayerStat playerStat;

    private Vector2 moveInput = Vector2.zero;
    private float currentSlopeAngle;
    private Vector2 slopeNormal;
    private float defaultGravity;
    private bool isJumping = false;
    private bool lastWalking = false;
    private bool wasMovingOnSlope = false;
    private PlayerDash playerDash;
    private PlayerAttack playerAttack;

    public Transform groundCheck;

    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float wallCheckDistance = 0.2f;
    [SerializeField] private float wallSlideSpeed = 2f;

    [Header("Raycast Settings")]
    public float rayDistance = 0.4f;
    public LayerMask groundLayer;

    public event Action<bool> WalkingChanged;
    public event Action<bool> JumpingChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputHandler>();
        playerDash = GetComponent<PlayerDash>();
        playerAttack = GetComponent<PlayerAttack>();
        playerStat = GetComponent<PlayerStat>();

        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (playerLayer != -1 && enemyLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        }
    }

    private void OnEnable()
    {
        input.MoveInputChanged += HandleMoveInputChanged;
        input.JumpPressed += HandleJumpPressed;
    }

    private void OnDisable()
    {
        input.MoveInputChanged -= HandleMoveInputChanged;
        input.JumpPressed -= HandleJumpPressed;
    }

    private void Start()
    {
        defaultGravity = rb.gravityScale;
        if (groundCheck == null) groundCheck = transform;
    }

    private void HandleMoveInputChanged(Vector2 newInput)
    {
        moveInput = newInput;
    }

    public void ResetMovementState()
    {
        moveInput = Vector2.zero;
        isJumping = false;
        wasMovingOnSlope = false;
        lastWalking = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        WalkingChanged?.Invoke(false);
        JumpingChanged?.Invoke(false);
    }

    private bool IsGrounded()
    {
        Vector2 origin = groundCheck.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayDistance, groundLayer);
        Debug.DrawRay(origin, Vector2.down * rayDistance, Color.red);
        return hit.collider != null;
    }

    private bool IsAgainstWall()
    {
        float direction = Mathf.Sign(moveInput.x != 0f ? moveInput.x : rb.linearVelocity.x);
        if (direction == 0f)
        {
            return false;
        }

        Vector2 origin = (Vector2)groundCheck.position + Vector2.right * direction * 0.2f;
        Vector2 wallDirection = Vector2.right * direction;
        RaycastHit2D hit = Physics2D.Raycast(origin, wallDirection, wallCheckDistance, groundLayer);
        Debug.DrawRay(origin, wallDirection * wallCheckDistance, Color.yellow);
        return hit.collider != null;
    }

    public bool IsOnSlope()
    {
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, rayDistance, groundLayer);
        Debug.DrawRay(groundCheck.position, Vector2.down * rayDistance, Color.blue);

        if (hit.collider != null)
        {
            slopeNormal = hit.normal;
            currentSlopeAngle = Vector2.Angle(Vector2.up, slopeNormal);
            return currentSlopeAngle > 0.1f;
        }

        slopeNormal = Vector2.up;
        return false;
    }

    private void HandleJumpPressed()
    {
        if (playerAttack != null && playerAttack.IsAttacking) return;
        if (playerDash != null && playerDash.IsDashing) return;
        if (playerStat != null && playerStat.IsHitStunned) return;
        // 원본 코드의 "context.performed && IsGrounded()" 조건을 그대로 유지
        if (IsGrounded())
        {
            isJumping = true;
            rb.gravityScale = defaultGravity;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            JumpingChanged?.Invoke(true); // 원본처럼 점프 시작 즉시 알림
        }
    }

    private void FixedUpdate()
    {

        if (playerStat != null && playerStat.IsHitStunned)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        
        if (playerDash != null && playerDash.IsDashing)
        {
            bool dashWalking = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            if (dashWalking != lastWalking)
            {
                lastWalking = dashWalking;
                WalkingChanged?.Invoke(dashWalking);
            }
            return;
        }

        if (playerAttack != null && playerAttack.IsAttacking)
        {
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(playerAttack.AttackDirection * playerAttack.AttackMoveSpeed, rb.linearVelocity.y);
            return;
        }

        bool wallTouching = IsAgainstWall();
        bool grounded = IsGrounded() && !wallTouching;
        bool onSlope = IsOnSlope();

        bool applySlope = onSlope && grounded && !isJumping;

        if (applySlope)
        {
            rb.gravityScale = 0f;

            if (moveInput.x != 0f)
            {
                Vector2 slopeDirection = new Vector2(slopeNormal.y, -slopeNormal.x);
                Vector2 slopeVelocity = slopeDirection * moveInput.x * moveSpeed;
                rb.linearVelocity = new Vector2(slopeVelocity.x, Mathf.Min(slopeVelocity.y, 0f));
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

            bool leftSlopeWhileMoving = wasMovingOnSlope && !onSlope && !grounded && !isJumping;
            if (leftSlopeWhileMoving && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }

            if (wallTouching && rb.linearVelocity.y <= 0f && rb.linearVelocity.y > -wallSlideSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }
        }

        wasMovingOnSlope = applySlope;

        // 값이 바뀔 때만 이벤트 발생 (SetBool을 매 프레임 같은 값으로 불러도 결과는 동일하므로 동작은 원본과 같음)
        bool isWalking = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        if (isWalking != lastWalking)
        {
            lastWalking = isWalking;
            WalkingChanged?.Invoke(isWalking);
        }

        if (grounded && rb.linearVelocity.y <= 0.1f)
        {
            isJumping = false;
            JumpingChanged?.Invoke(false);
        }
    }
}
