using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private EnemyStat enemyStat;

    [Header("Attack Settings")]
    [SerializeField] private float detectRange = 4f;      // 이 거리 안이면 추적/공격 시도
    [SerializeField] private float attackRange = 1f;       // 이 거리 안이면 실제로 때림
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private LayerMask playerLayer;

    public event Action OnAttackTriggered; // 애니메이터가 구독

    private float cooldownTimer = 0f;
    private bool isAttacking = false;

    private void Awake()
    {
        if (attackPoint == null)
            attackPoint = transform;
        if (enemyStat == null) enemyStat = GetComponent<EnemyStat>();
        FindPlayer();
    }

    private void Update()
    {
        if (enemyStat != null && enemyStat.IsDead) return; // 죽었으면 아무것도 안 함
        if (playerTransform == null || isAttacking) return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance <= attackRange)
        {
            StartAttack();
        }
        // else if (distance <= detectRange) { 추적 로직은 별도 EnemyMove에서 처리 }
    }

    private void StartAttack()
    {
        isAttacking = true;
        cooldownTimer = attackCooldown;
        Debug.Log($"[{gameObject.name}] attack started");
        OnAttackTriggered?.Invoke(); // 애니메이션 재생 트리거
    }

    private void FindPlayer()
    {
        if (playerTransform != null) return;

        PlayerStat playerStat = FindAnyObjectByType<PlayerStat>();
        if (playerStat != null)
            playerTransform = playerStat.transform;
    }

    // Animation Event로 실제 타격 프레임에 호출 (플레이어 쪽 DealDamage와 동일한 패턴)
    public void AE_DealDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, 0.5f, playerLayer);
        Debug.Log($"[{gameObject.name}] AE_DealDamage called, hits={hits.Length}");
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && damagedTargets.Add(damageable))
            {
                damageable.TakeDamage(damage);
            }
        }
    }

    // Animation Event로 공격 애니메이션 끝나는 시점에 호출
    public void AE_AttackEnd()
    {
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}