using UnityEngine;
using System;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PlayerMove))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator anim;
    private SpriteRenderer spriteRenderer;
    private PlayerMove playerMove;
    private PlayerInputHandler input;
    private PlayerDash playerDash;
    [SerializeField] private PlayerAttack playerAttack;
     public event Action OnComboWindowOpen;
    public event Action OnAttackAnimationEnd;
    

    private void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerMove = GetComponent<PlayerMove>();
        input = GetComponent<PlayerInputHandler>();
        playerDash = GetComponent<PlayerDash>();
        if (playerAttack == null)
            playerAttack = GetComponent<PlayerAttack>();
        
    }

    private void OnEnable()
    {
        Debug.Log("[PlayerAnimator] OnEnable called");
        playerMove.WalkingChanged += HandleWalkingChanged;
        playerMove.JumpingChanged += HandleJumpingChanged;
        input.MoveInputChanged += HandleMoveInputChanged;
        playerAttack.OnAttackTriggered += PlayAttackAnimation;

        if (playerDash != null)
        {
            playerDash.DashStateChanged += HandleDashStateChanged;
            playerDash.DashTriggered += HandleDashTriggered;
        }

        
    }

    private void OnDisable()
    {
        playerMove.WalkingChanged -= HandleWalkingChanged;
        playerMove.JumpingChanged -= HandleJumpingChanged;
        input.MoveInputChanged -= HandleMoveInputChanged;
        playerAttack.OnAttackTriggered -= PlayAttackAnimation;

        if (playerDash != null)
        {
            playerDash.DashStateChanged -= HandleDashStateChanged;
            playerDash.DashTriggered -= HandleDashTriggered;
        }
    }

     private void PlayAttackAnimation(int comboStep)
    {
        anim.SetInteger("ComboStep", comboStep);
        anim.SetTrigger("Attack");
    }

    public void AE_OpenComboWindow()
    {
        OnComboWindowOpen?.Invoke();
    }

    public void AE_AttackAnimationEnd()
    {
        OnAttackAnimationEnd?.Invoke();
    }

    private void HandleWalkingChanged(bool isWalking)
    {
        anim.SetBool("isWalking", isWalking);
    }

    private void HandleJumpingChanged(bool isJumping)
    {
        anim.SetBool("isJumping", isJumping);
    }

    private void HandleMoveInputChanged(Vector2 moveInput)
    {
        if (playerAttack != null && playerAttack.IsAttacking) return;
        if (moveInput.x != 0)
            spriteRenderer.flipX = moveInput.x < 0;
    }

    private void HandleDashStateChanged(bool isDashing)
    {
        anim.SetBool("isDashing", isDashing);
    }

    private void HandleDashTriggered()
    {
        anim.SetTrigger("Dash");
    }

    private void HandleAttackTriggered(int comboIndex)
    {
        // Animator에 int 파라미터 "ComboIndex"와 트리거 "Attack"을 만들어두고
        // Any State -> Attack 전환 조건에서 ComboIndex 값으로 콤보별 애니메이션을 분기하면 됨
        anim.SetInteger("ComboIndex", comboIndex);
        anim.SetTrigger("Attack");
    }
}
