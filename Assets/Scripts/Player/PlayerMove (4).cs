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
    private PlayerDash playerDash;
    private PlayerAttack playerAttack;

    public Transform groundCheck;

    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 6f;

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

    private bool IsGrounded()
    {
        Vector2 origin = groundCheck.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, rayDistance, groundLayer);
        Debug.DrawRay(origin, Vector2.down * rayDistance, Color.red);
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

        bool grounded = IsGrounded();
        bool onSlope = IsOnSlope();

        bool applySlope = onSlope && grounded && !isJumping;

        //Debug.Log($"grounded:{grounded} onSlope:{onSlope} angle:{currentSlopeAngle:F1} vel:{rb.linearVelocity} isJumping:{isJumping}");

        if (applySlope)
        {
            rb.gravityScale = 0f;

            if (moveInput.x != 0f)
            {
                Vector2 slopeDirection = new Vector2(slopeNormal.y, -slopeNormal.x);
                rb.linearVelocity = slopeDirection * moveInput.x * moveSpeed;
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
        }

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
