using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private PlayerDash playerDash;
    [SerializeField] private PlayerStat playerStat;

    [Header("Combo Settings")]
    [SerializeField] private float comboResetTime = 1.0f;
    [SerializeField] private int maxCombo = 3;
    [SerializeField] private float attackMoveSpeed = 0.5f;

    [Header("Attack Hit")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.6f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private HitEffect hitEffect;

    private Vector3 attackPointLocalPos;
    private SpriteRenderer spriteRenderer;
    private bool facingRight = true;
    private bool pendingFacingRight = true;
    private bool hasPendingFacingDirection;
    private int attackDirection = 1;

    public event Action<int> OnAttackTriggered;
    public bool IsAttacking => isAttacking;
    public int AttackDirection => attackDirection;
    public float AttackMoveSpeed => attackMoveSpeed;

    private int comboStep = 0;
    private bool isAttacking = false;
    private bool comboWindowOpen = false;
    private bool queuedInput = false;
    private Coroutine comboResetRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (attackPoint != null)
        {
            attackPointLocalPos = attackPoint.localPosition;
        }
    }
    
    private void OnEnable()
{
     Debug.Log("[PlayerAttack] OnEnable - subscribing");
    Debug.Log("[PlayerAttack] OnEnable called");
    inputHandler.OnAttackInput += HandleAttackInput;
    inputHandler.MoveInputChanged += HandleMoveInputChanged;
    Debug.Log("[PlayerAttack] MoveInputChanged subscribed");
    playerAnimator.OnComboWindowOpen += HandleComboWindowOpen;
    playerAnimator.OnAttackAnimationEnd += HandleAttackAnimationEnd;
}

    private void OnDisable()
    {
        inputHandler.OnAttackInput -= HandleAttackInput;
        inputHandler.MoveInputChanged -= HandleMoveInputChanged;
        playerAnimator.OnComboWindowOpen -= HandleComboWindowOpen;
        playerAnimator.OnAttackAnimationEnd -= HandleAttackAnimationEnd;
    }

    private void HandleAttackInput()
    {
        if (playerDash != null && playerDash.IsDashing)
        {
            return; // 대시 중엔 공격 입력 자체를 무시
        }
        
        if (!isAttacking)
        {
            StartAttack();
        }
        else if (comboWindowOpen)
        {
            queuedInput = true; // 다음 콤보 예약
        }
        // isAttacking이면서 윈도우가 안 열렸으면 그냥 무시 (씹힘)
    }

    private void StartAttack()
    {
        if (comboResetRoutine != null)
            StopCoroutine(comboResetRoutine);

        if (inputHandler != null && Mathf.Abs(inputHandler.MoveInput.x) > 0.01f)
        {
            pendingFacingRight = inputHandler.MoveInput.x > 0f;
            hasPendingFacingDirection = true;
        }

        if (hasPendingFacingDirection)
        {
            ApplyFacingDirection(pendingFacingRight);
            hasPendingFacingDirection = false;
        }

        isAttacking = true;
        comboWindowOpen = false;
        queuedInput = false;

        comboStep = (comboStep % maxCombo) + 1; // 1 -> 2 -> 3 -> 1 ...
        attackDirection = facingRight ? 1 : -1;

        OnAttackTriggered?.Invoke(comboStep);
    }

    // Animation Event로 "이제 다음 입력 받아도 됨" 시점에 호출됨
    private void HandleComboWindowOpen()
    {
        comboWindowOpen = true;
    }

    // Animation Event로 "애니메이션 다 끝남" 시점에 호출됨
    private void HandleAttackAnimationEnd()
    {
        isAttacking = false;
        comboWindowOpen = false;

        if (queuedInput)
        {
            queuedInput = false;
            StartAttack(); // 예약된 입력 있으면 바로 다음 콤보
        }
        else
        {
            comboResetRoutine = StartCoroutine(ComboResetTimer());
        }
    }

    private IEnumerator ComboResetTimer()
    {
        yield return new WaitForSeconds(comboResetTime);
        comboStep = 0;
    }

    // 이건 Animation Event로 데미지 타이밍에 직접 호출해도 되고,
    // 여기 comboStep 값으로 데미지/판정범위를 다르게 줄 수 있음
    
    public void DealDamage()
{
    if (attackPoint == null)
    {
        Debug.LogWarning("[Attack] attackPoint가 연결 안 됨");
        return;
    }

    Debug.Log("[Attack] DealDamage called");
    Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
    Debug.Log($"[Attack] DealDamage called, hits={hits.Length}");
    HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

    int damage = playerStat != null ? playerStat.AttackPower : 10;

    if (comboStep == 3)
    {
        damage = Mathf.RoundToInt(damage * 1.5f);
    }

    foreach (Collider2D hit in hits)
    {
        IDamageable damageable = hit.GetComponentInParent<IDamageable>();
        if (damageable != null && damagedTargets.Add(damageable))
        {
            damageable.TakeDamage(damage); // IDamageable도 float 받으니 int->float 자동 변환됨

            if (hitEffect != null)
                hitEffect.SpawnAt(hit.bounds.center);
        }
    }
}

private void OnDrawGizmosSelected()
{
    if (attackPoint == null) return;
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(attackPoint.position, attackRange);
}

private void HandleMoveInputChanged(Vector2 moveInput)
{
    if (moveInput.x == 0f) return;

    bool shouldFaceRight = moveInput.x > 0f;
    if (isAttacking)
    {
        pendingFacingRight = shouldFaceRight;
        hasPendingFacingDirection = true;
        return;
    }

    ApplyFacingDirection(shouldFaceRight);
}

private void ApplyFacingDirection(bool shouldFaceRight)
{
    if (shouldFaceRight == facingRight) return;

    facingRight = shouldFaceRight;

    if (attackPoint != null)
    {
        Vector3 pos = attackPointLocalPos;
        pos.x = facingRight ? Mathf.Abs(pos.x) : -Mathf.Abs(pos.x);
        attackPoint.localPosition = pos;
    }

    if (spriteRenderer != null)
        spriteRenderer.flipX = !facingRight;

    Debug.Log($"[Attack] facingRight={facingRight}");
}
}