using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private EnemyStat enemyStat;

    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectRange = 4f;   // EnemyAttack의 detectRange와 값 맞춰주세요
    [SerializeField] private float attackRange = 1f;    // EnemyAttack의 attackRange와 값 맞춰주세요

    private Rigidbody2D rigid;
    private int patrolMove;   // 평소 랜덤 순찰 방향
    private bool isChasing;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        if (enemyStat == null) enemyStat = GetComponent<EnemyStat>();
        FindPlayer();
        Invoke(nameof(Think), 5f);
    }

    void FixedUpdate()
    {
        // 죽었으면 정지
        if (enemyStat != null && enemyStat.IsDead)
        {
            rigid.linearVelocity = new Vector2(0f, rigid.linearVelocity.y);
            return;
        }

        int moveDir = patrolMove;
        isChasing = false;

        if (playerTransform == null)
            FindPlayer();

        if (playerTransform != null)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);

            if (distance <= detectRange)
            {
                isChasing = true;
                int playerDirection = playerTransform.position.x > transform.position.x ? 1 : -1;
                FlipTowards(playerDirection);

                if (distance > attackRange)
                {
                    // 공격 사거리 밖이면 플레이어 쪽으로 이동
                    moveDir = playerDirection;
                }
                else
                {
                    // 공격 사거리 안이면 멈춰서 EnemyAttack이 공격하도록 함
                    moveDir = 0;
                }
            }
        }

        rigid.linearVelocity = new Vector2(moveDir * moveSpeed, rigid.linearVelocity.y);

        if (moveDir != 0)
        {
            FlipTowards(moveDir);
        }

        // 순찰 중일 때만 낭떠러지 체크 (추적 중엔 플레이어를 향해 적극적으로 따라감)
        if (!isChasing)
        {
            Vector2 frontVec = new Vector2(rigid.position.x + moveDir, rigid.position.y);
            Debug.DrawRay(frontVec, Vector3.down, Color.green);
            RaycastHit2D rayHit = Physics2D.Raycast(frontVec, Vector3.down, 1f, LayerMask.GetMask("Ground"));
            if (rayHit.collider == null)
            {
                patrolMove *= -1;
            }
        }
    }

    private void FlipTowards(int moveDir)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (moveDir > 0 ? 1 : -1);
        transform.localScale = scale;
    }

    private void FindPlayer()
    {
        PlayerStat playerStat = FindAnyObjectByType<PlayerStat>();
        if (playerStat != null)
            playerTransform = playerStat.transform;
    }

    void Think()
    {
        // 추적 중이 아닐 때만 새로운 순찰 방향을 정함
        if (!isChasing)
        {
            patrolMove = Random.Range(-1, 2);
        }
        Invoke(nameof(Think), 5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}