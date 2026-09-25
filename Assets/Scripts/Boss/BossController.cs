using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    private enum BossPhase
    {
        PhaseOne
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private BossAnimation bossAnimation;
    [SerializeField] private BossStat bossStat;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject normalAttackEffect;
    [SerializeField] private SpriteRenderer bossSpriteRenderer;

    [Header("Wall Collision")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckRadius = 0.5f;
    [SerializeField] private float wallPadding = 0.02f;

    [Header("Phase One")]
    [SerializeField] private float battleStartDelay = 2f;
    [SerializeField] private float stopDistance = 3f;
    [SerializeField] private float teleportDistance = 3f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float idleAfterAttack = 1f;
    [SerializeField] private float minPatternInterval = 3f;
    [SerializeField] private float maxPatternInterval = 8f;
    [SerializeField] private float betweenPatternMoveDuration = 1.5f;
    [SerializeField] private float facePlayerInterval = 0.1f;

    [Header("Normal Attack")]
    [SerializeField] private string normalAttackAnimation = "BossNormalAttack";
    [SerializeField] private float attackWindow = 0.1f;
    [SerializeField] private float attackPathRadius = 0.5f;
    [SerializeField] private float effectRadius = 1f;
    [SerializeField] private float effectForwardOffset = 1f;
    [SerializeField] private float effectFadeDuration = 0.35f;
    [SerializeField] private float normalAttackDamage = 20f;
    [SerializeField] private LayerMask playerLayer;

    private BossPhase currentPhase = BossPhase.PhaseOne;
    private Coroutine battleRoutine;
    private bool isFacingLeft;
    private bool isDead;
    private bool isCutscenePlaying;
    private bool isAttackPatternPlaying;
    private bool normalAttackEventTriggered;
    private Vector3 previousPosition;
    private float facePlayerTimer;
    private SpriteRenderer[] effectRenderers = new SpriteRenderer[0];
    private Color[] effectOriginalColors = new Color[0];

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (bossAnimation == null) bossAnimation = GetComponent<BossAnimation>();
        if (bossStat == null) bossStat = GetComponent<BossStat>();
        if (bossSpriteRenderer == null) bossSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (playerTransform == null)
        {
            PlayerStat player = FindAnyObjectByType<PlayerStat>();
            if (player != null) playerTransform = player.transform;
        }

        if (wallLayer.value == 0)
            wallLayer = LayerMask.GetMask("Wall");

        if (playerLayer.value == 0)
        {
            int playerLayerIndex = LayerMask.NameToLayer("Player");
            playerLayer = playerLayerIndex >= 0 ? 1 << playerLayerIndex : ~0;
        }

        CacheEffectRenderers();
        previousPosition = transform.position;

        if (bossStat != null)
            bossStat.OnDeath += StopBattle;
    }

    private void Start()
    {
        if (!isCutscenePlaying)
            StartBattle();
    }

    private void Update()
    {
        if (isDead || isCutscenePlaying || isAttackPatternPlaying || playerTransform == null)
            return;

        facePlayerTimer -= Time.deltaTime;
        if (facePlayerTimer <= 0f)
        {
            FacePlayer();
            facePlayerTimer = Mathf.Max(0.01f, facePlayerInterval);
        }
    }

    private void OnDisable()
    {
        if (battleRoutine != null)
            StopCoroutine(battleRoutine);
    }

    private void OnDestroy()
    {
        if (bossStat != null)
            bossStat.OnDeath -= StopBattle;
    }

    private IEnumerator BattleRoutine()
    {
        yield return new WaitForSeconds(battleStartDelay);

        while (!isDead && !isCutscenePlaying)
        {
            if (playerTransform == null)
            {
                FindPlayer();
                yield return null;
                continue;
            }

            yield return ExecuteCurrentPhase();
            FacePlayer();
            yield return new WaitForSeconds(idleAfterAttack);
            float remainingInterval = Random.Range(minPatternInterval, maxPatternInterval) - idleAfterAttack;
            remainingInterval = Mathf.Max(0f, remainingInterval);
            float moveDuration = Mathf.Min(remainingInterval, betweenPatternMoveDuration);
            yield return MoveBetweenPatterns(moveDuration);
            yield return new WaitForSeconds(remainingInterval - moveDuration);
        }
    }

    private IEnumerator ExecuteCurrentPhase()
    {
        switch (currentPhase)
        {
            case BossPhase.PhaseOne:
                yield return ExecutePhaseOnePattern();
                break;
        }
    }

    private IEnumerator ExecutePhaseOnePattern()
    {
        yield return MoveToAttackDistance();

        if (isDead || playerTransform == null)
            yield break;

        isAttackPatternPlaying = true;
        FacePlayer();
        PlayNormalAttackAnimation();

        while (!normalAttackEventTriggered && !isDead && !isCutscenePlaying)
            yield return null;

        normalAttackEventTriggered = false;

        if (isDead || isCutscenePlaying)
        {
            isAttackPatternPlaying = false;
            yield break;
        }

        yield return ExecuteNormalAttack();
        isAttackPatternPlaying = false;
        FacePlayer();
    }

    public void BossNormalAttackFlash()
    {
        if (!isDead && !isCutscenePlaying)
            normalAttackEventTriggered = true;
    }

    private IEnumerator MoveToAttackDistance()
    {
        while (!isDead && playerTransform != null &&
               Mathf.Abs(playerTransform.position.x - transform.position.x) > stopDistance)
        {
            Vector3 position = transform.position;
            float moveDirection = Mathf.Sign(playerTransform.position.x - transform.position.x);
            float desiredDistance = Mathf.Min(Mathf.Abs(playerTransform.position.x - transform.position.x),
                GetMoveSpeed() * Time.deltaTime);
            float moveDistance = GetSafeTravelDistance(Vector2.right * moveDirection, desiredDistance);
            position.x += moveDirection * moveDistance;
            transform.position = position;
            UpdateFacingFromMovement();

            if (moveDistance < desiredDistance)
                yield break;

            yield return null;
        }
    }

    private IEnumerator MoveBetweenPatterns(float duration)
    {
        float elapsed = 0f;
        float targetSide = Random.value < 0.5f ? -1f : 1f;

        while (!isDead && !isCutscenePlaying && playerTransform != null && elapsed < duration)
        {
            float targetX = playerTransform.position.x + targetSide * stopDistance;
            Vector3 position = transform.position;
            float moveDirection = Mathf.Sign(targetX - transform.position.x);
            float desiredDistance = Mathf.Min(Mathf.Abs(targetX - transform.position.x),
                GetMoveSpeed() * Time.deltaTime);
            float moveDistance = GetSafeTravelDistance(Vector2.right * moveDirection, desiredDistance);
            position.x += moveDirection * moveDistance;
            transform.position = position;
            UpdateFacingFromMovement();

            if (moveDistance < desiredDistance)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ExecuteNormalAttack()
    {
        Vector2 startPosition = transform.position;
        Vector2 direction = isFacingLeft ? Vector2.left : Vector2.right;
        float dashDistance = GetSafeTravelDistance(direction, teleportDistance);
        Vector2 destination = startPosition + direction * dashDistance;

        transform.position = new Vector3(destination.x, transform.position.y, transform.position.z);
        UpdateFacingFromMovement();
        Vector2 effectPosition = (Vector2)transform.position + direction * Mathf.Max(0f, effectForwardOffset);
        ShowAttackEffect(effectPosition);

        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        float elapsed = 0f;
        while (elapsed < attackWindow)
        {
            DamageTargetsInAttackArea(startPosition, destination, damagedTargets);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return FadeOutAttackEffect();
    }

    private float GetSafeTravelDistance(Vector2 direction, float desiredDistance)
    {
        desiredDistance = Mathf.Max(0f, desiredDistance);
        if (desiredDistance <= 0f || wallLayer.value == 0)
            return desiredDistance;

        RaycastHit2D wallHit = Physics2D.CircleCast(transform.position, Mathf.Max(0f, wallCheckRadius),
            direction, desiredDistance, wallLayer);
        if (wallHit.collider == null)
            return desiredDistance;

        return Mathf.Clamp(wallHit.distance - Mathf.Max(0f, wallPadding), 0f, desiredDistance);
    }

    private void DamageTargetsInAttackArea(Vector2 startPosition, Vector2 destination,
        HashSet<IDamageable> damagedTargets)
    {
        Vector2 travel = destination - startPosition;
        float distance = travel.magnitude;
        Vector2 direction = distance > 0f ? travel / distance : Vector2.right;
        RaycastHit2D[] pathHits = Physics2D.CircleCastAll(startPosition, attackPathRadius,
            direction, distance, playerLayer.value);
        Collider2D[] effectHits = Physics2D.OverlapCircleAll(destination, effectRadius, playerLayer);

        ApplyPathDamage(pathHits, damagedTargets);
        ApplyDamage(effectHits, damagedTargets);
    }

    private void ApplyPathDamage(RaycastHit2D[] hits, HashSet<IDamageable> damagedTargets)
    {
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null)
                ApplyDamage(hit.collider, damagedTargets);
        }
    }

    private void ApplyDamage(Collider2D[] hits, HashSet<IDamageable> damagedTargets)
    {
        foreach (Collider2D hit in hits)
        {
            ApplyDamage(hit, damagedTargets);
        }
    }

    private void ApplyDamage(Collider2D hit, HashSet<IDamageable> damagedTargets)
    {
        if (hit == null || hit.GetComponentInParent<PlayerStat>() == null)
            return;

        IDamageable damageable = hit.GetComponentInParent<IDamageable>();
        if (damageable != null && damagedTargets.Add(damageable))
            damageable.TakeDamage(GetAttackDamage());
    }

    private void PlayNormalAttackAnimation()
    {
        if (bossAnimation != null)
        {
            bossAnimation.PlayAttackAnimation();
            return;
        }

        if (animator != null && !string.IsNullOrEmpty(normalAttackAnimation))
            animator.Play(normalAttackAnimation, 0, 0f);
    }

    private void ShowAttackEffect(Vector2 position)
    {
        if (normalAttackEffect == null) return;

        normalAttackEffect.transform.position = position;
        normalAttackEffect.SetActive(true);
        for (int i = 0; i < effectRenderers.Length; i++)
        {
            effectRenderers[i].enabled = true;
            effectRenderers[i].color = effectOriginalColors[i];
        }
    }

    private IEnumerator FadeOutAttackEffect()
    {
        if (normalAttackEffect == null) yield break;

        float elapsed = 0f;
        while (elapsed < effectFadeDuration)
        {
            float alpha = 1f - elapsed / Mathf.Max(0.01f, effectFadeDuration);
            for (int i = 0; i < effectRenderers.Length; i++)
            {
                Color color = effectOriginalColors[i];
                color.a *= alpha;
                effectRenderers[i].color = color;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        normalAttackEffect.SetActive(false);
    }

    private void CacheEffectRenderers()
    {
        if (normalAttackEffect == null) return;

        effectRenderers = normalAttackEffect.GetComponentsInChildren<SpriteRenderer>(true);
        effectOriginalColors = new Color[effectRenderers.Length];
        for (int i = 0; i < effectRenderers.Length; i++)
            effectOriginalColors[i] = effectRenderers[i].color;
        normalAttackEffect.SetActive(false);
    }

    private void UpdateFacingFromMovement()
    {
        float horizontalMovement = transform.position.x - previousPosition.x;
        if (Mathf.Abs(horizontalMovement) <= 0.001f)
            return;

        isFacingLeft = horizontalMovement < 0f;

        if (bossSpriteRenderer != null)
            bossSpriteRenderer.flipX = !isFacingLeft;

        previousPosition = transform.position;
    }

    private void FacePlayer()
    {
        if (playerTransform == null)
            return;

        float horizontalDirection = playerTransform.position.x - transform.position.x;
        if (Mathf.Abs(horizontalDirection) <= 0.001f)
            return;

        isFacingLeft = horizontalDirection < 0f;
        ApplyFacingDirection();
    }

    private void ApplyFacingDirection()
    {
        if (bossSpriteRenderer != null)
            bossSpriteRenderer.flipX = !isFacingLeft;
    }

    private void FindPlayer()
    {
        PlayerStat player = FindAnyObjectByType<PlayerStat>();
        if (player != null) playerTransform = player.transform;
    }

    private float GetMoveSpeed()
    {
        return bossStat != null ? bossStat.MoveSpeed : moveSpeed;
    }

    private float GetAttackDamage()
    {
        return normalAttackDamage;
    }

    public void SetPhase(int phaseIndex)
    {
        if (phaseIndex == 1)
            currentPhase = BossPhase.PhaseOne;
    }

    public void StopBattle()
    {
        isDead = true;
        if (battleRoutine != null)
            StopCoroutine(battleRoutine);
    }

    public void BeginCutscene()
    {
        if (isDead) return;

        isCutscenePlaying = true;
        isAttackPatternPlaying = false;
        normalAttackEventTriggered = false;
        if (battleRoutine != null)
        {
            StopCoroutine(battleRoutine);
            battleRoutine = null;
        }

        if (normalAttackEffect != null)
            normalAttackEffect.SetActive(false);

        bossAnimation?.SetCutsceneMode(true);
    }

    public void EndCutscene()
    {
        if (isDead) return;

        isCutscenePlaying = false;
        bossAnimation?.SetCutsceneMode(false);
        StartBattle();
    }

    private void StartBattle()
    {
        if (isDead || isCutscenePlaying || battleRoutine != null)
            return;

        battleRoutine = StartCoroutine(BattleRoutine());
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, effectRadius);
    }
}
