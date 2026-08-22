using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimator : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private EnemyStat enemyStat;
    [SerializeField] private EnemyAttack enemyAttack;
    [SerializeField] private Collider2D bodyCollider; // 사망 시 충돌 꺼주기용 (선택)

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (enemyStat == null) enemyStat = GetComponent<EnemyStat>();
        if (enemyAttack == null) enemyAttack = GetComponent<EnemyAttack>();
    }

    private void OnEnable()
    {
        if (enemyStat != null)
        {
            enemyStat.OnDamaged += HandleDamaged;
            enemyStat.OnDeath += HandleDeath;
        }

        if (enemyAttack != null)
        {
            enemyAttack.OnAttackTriggered += HandleAttackTriggered;
        }
    }

    private void OnDisable()
    {
        if (enemyStat != null)
        {
            enemyStat.OnDamaged -= HandleDamaged;
            enemyStat.OnDeath -= HandleDeath;
        }

        if (enemyAttack != null)
        {
            enemyAttack.OnAttackTriggered -= HandleAttackTriggered;
        }
    }

    private void HandleDamaged()
    {
        anim.SetTrigger("Hit");
    }

    private void HandleDeath()
    {
        anim.SetTrigger("Death");

        if (bodyCollider != null)
        {
            bodyCollider.enabled = false; // 죽는 순간 더는 맞지도, 부딪히지도 않게
        }
    }

    private void HandleAttackTriggered()
    {
        anim.SetTrigger("Attack");
    }
}