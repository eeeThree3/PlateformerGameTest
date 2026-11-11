using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    private enum BossPhase
    {
        PhaseOne
    }

    private enum AttackPattern
    {
        NormalAttack,
        Strike,
        UnderSlash
    }

    private static readonly AttackPattern[] phaseOnePatterns =
    {
        AttackPattern.NormalAttack,
        AttackPattern.Strike,
        AttackPattern.UnderSlash
    };

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private BossAnimation bossAnimation;
    [SerializeField] private BossStat bossStat;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject normalAttackEffect;
    [SerializeField] private SpriteRenderer bossSpriteRenderer;
    [SerializeField] private GameObject underSlashEffectObject;
    [SerializeField] private Transform bossWallLeft;
    [SerializeField] private Transform bossWallRight;

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

    [Header("Strike Attack")]
    [SerializeField] private string strikeAnimation = "BossStrike";
    [SerializeField] private float strikeRange = 3.5f;
    [SerializeField] private float strikeWidth = 3f;
    [SerializeField] private float strikeDamage = 20f;
    [SerializeField] private GameObject strikeEffectObject;
    [SerializeField] private float strikeEffectFadeDuration = 0.4f;
    [SerializeField] private float strikeCameraShakeDuration = 0.15f;
    [SerializeField] private float strikeCameraShakeMagnitude = 0.12f;

    [Header("Under Slash")]
    [SerializeField] private string underSlashAnimation = "BossUnderSlash";
    [SerializeField] private float underSlashRushDuration = 0.45f;
    [SerializeField] private float underSlashEffectFadeDuration = 0.5f;
    [SerializeField] private float underSlashHitNormalizedTime = 0.8f;
    [SerializeField] private float underSlashWallInset = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float underSlashDamage = 20f;

    private BossPhase currentPhase = BossPhase.PhaseOne;
    private Coroutine battleRoutine;
    private Coroutine strikeEffectFadeRoutine;
    private SpriteRenderer strikeEffectRenderer;
    private Color strikeEffectOriginalColor;
    private MainCameraFollow cameraFollow;
    private bool isFacingLeft;
    private bool isDead;
    private bool isCutscenePlaying;
    private bool isAttackPatternPlaying;
    private bool normalAttackEventTriggered;
    private bool isStrikePatternPlaying;
    private bool strikeEventTriggered;
    private bool isUnderSlashPatternPlaying;
    private bool underSlashEventTriggered;
    private Vector3 previousPosition;
    private float facePlayerTimer;
    private SpriteRenderer[] effectRenderers = new SpriteRenderer[0];
    private Color[] effectOriginalColors = new Color[0];
    private bool[] effectOriginalFlipX = new bool[0];
    private SpriteRenderer underSlashEffectRenderer;
    private Color underSlashEffectOriginalColor;
    private Coroutine underSlashEffectFadeRoutine;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (bossAnimation == null) bossAnimation = GetComponent<BossAnimation>();
        if (bossStat == null) bossStat = GetComponent<BossStat>();
        if (bossSpriteRenderer == null) bossSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        cameraFollow = FindAnyObjectByType<MainCameraFollow>();
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

        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Ground");

        CacheEffectRenderers();
        CacheStrikeEffect();
        CacheUnderSlashEffect();
        previousPosition = transform.position;

        if (bossStat != null)
            bossStat.OnDeath += StopBattle;
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
        if (!TryChoosePhaseOnePattern(out AttackPattern pattern))
        {
            Debug.LogError("No phase one attack patterns have a positive weight.", this);
            yield break;
        }

        switch (pattern)
        {
            case AttackPattern.NormalAttack:
                yield return ExecuteNormalAttackPattern();
                break;
            case AttackPattern.Strike:
                yield return ExecuteStrikePattern();
                break;
            case AttackPattern.UnderSlash:
                yield return ExecuteUnderSlashPattern();
                break;
        }
    }

    private bool TryChoosePhaseOnePattern(out AttackPattern selectedPattern)
    {
        float totalWeight = 0f;
        foreach (AttackPattern pattern in phaseOnePatterns)
            totalWeight += GetPhaseOnePatternWeight(pattern);

        if (totalWeight <= 0f)
        {
            selectedPattern = default;
            return false;
        }

        float randomWeight = Random.value * totalWeight;
        AttackPattern lastWeightedPattern = default;
        foreach (AttackPattern pattern in phaseOnePatterns)
        {
            float weight = GetPhaseOnePatternWeight(pattern);
            if (weight <= 0f)
                continue;

            lastWeightedPattern = pattern;
            if (randomWeight < weight)
            {
                selectedPattern = pattern;
                return true;
            }

            randomWeight -= weight;
        }

        selectedPattern = lastWeightedPattern;
        return true;
    }

    private float GetPhaseOnePatternWeight(AttackPattern pattern)
    {
        switch (pattern)
        {
            case AttackPattern.NormalAttack:
            case AttackPattern.Strike:
            case AttackPattern.UnderSlash:
                return 1f;
            default:
                return 0f;
        }
    }

    private IEnumerator ExecuteNormalAttackPattern()
    {
        yield return MoveToAttackDistance();

        if (isDead || playerTransform == null)
            yield break;

        FacePlayer();
        isAttackPatternPlaying = true;
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
        yield return WaitForNormalAttackAnimation();
        isAttackPatternPlaying = false;
        FacePlayer();
    }

    private IEnumerator ExecuteStrikePattern()
    {
        yield return MoveToAttackDistance();

        if (isDead || isCutscenePlaying || playerTransform == null)
            yield break;

        if (animator == null || string.IsNullOrEmpty(strikeAnimation) ||
            !animator.HasState(0, Animator.StringToHash($"Base Layer.{strikeAnimation}")))
        {
            Debug.LogError($"Boss strike animation state '{strikeAnimation}' is missing.", this);
            yield break;
        }

        FacePlayer();
        isAttackPatternPlaying = true;
        isStrikePatternPlaying = true;
        strikeEventTriggered = false;
        animator.Play(strikeAnimation, 0, 0f);

        while (!isDead && !isCutscenePlaying)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(strikeAnimation) && stateInfo.normalizedTime >= 1f)
                break;

            yield return null;
        }

        if (!isDead && !isCutscenePlaying)
            animator.Play("BossIdle", 0, 0f);

        isStrikePatternPlaying = false;
        isAttackPatternPlaying = false;
        FacePlayer();
    }

    private IEnumerator ExecuteUnderSlashPattern()
    {
        if (!TryGetArenaBounds(out Bounds arenaBounds))
        {
            Debug.LogError("Boss under-slash requires at least one active collider on the Ground layer.", this);
            yield break;
        }

        if (animator == null || string.IsNullOrEmpty(underSlashAnimation) ||
            !animator.HasState(0, Animator.StringToHash($"Base Layer.{underSlashAnimation}")))
        {
            Debug.LogError($"Boss under-slash animation state '{underSlashAnimation}' is missing.", this);
            yield break;
        }

        isAttackPatternPlaying = true;
        isUnderSlashPatternPlaying = true;
        underSlashEventTriggered = false;

        if (!TryGetBossWallBounds(out float wallLeftX, out float wallRightX))
        {
            Debug.LogError("Boss under-slash requires BossWallLeft and BossWallRight references.", this);
            yield break;
        }

        bool targetRightSide = Random.value < 0.5f;
        float sideInset = bossSpriteRenderer != null ? bossSpriteRenderer.bounds.extents.x : wallCheckRadius;
        float targetX = targetRightSide
            ? wallRightX - Mathf.Max(sideInset, underSlashWallInset)
            : wallLeftX + Mathf.Max(sideInset, underSlashWallInset);
        targetX = Mathf.Clamp(targetX, wallLeftX, wallRightX);

        yield return RushToUnderSlashSide(targetX);

        if (isDead || isCutscenePlaying)
        {
            isUnderSlashPatternPlaying = false;
            isAttackPatternPlaying = false;
            yield break;
        }

        if (!TryGetGroundY(targetX, arenaBounds, out float groundY))
        {
            Debug.LogError("Could not find the ground below the boss for the under-slash pattern.", this);
            isUnderSlashPatternPlaying = false;
            isAttackPatternPlaying = false;
            yield break;
        }

        LandOnGround(targetX, groundY);
        isFacingLeft = targetRightSide;
        ApplyFacingDirection();
        animator.Play(underSlashAnimation, 0, 0f);

        while (!isDead && !isCutscenePlaying)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(underSlashAnimation))
            {
                if (!underSlashEventTriggered &&
                    stateInfo.normalizedTime >= underSlashHitNormalizedTime)
                {
                    BossUnderSlashHit();
                }

                if (stateInfo.normalizedTime >= 1f)
                    break;
            }

            yield return null;
        }

        if (!isDead && !isCutscenePlaying)
            animator.Play("BossIdle", 0, 0f);

        isUnderSlashPatternPlaying = false;
        isAttackPatternPlaying = false;
        FacePlayer();
    }

    private IEnumerator RushToUnderSlashSide(float targetX)
    {
        Vector3 startPosition = transform.position;
        float duration = Mathf.Max(0.01f, underSlashRushDuration);
        float elapsed = 0f;
        float nextAfterimageTime = 0f;
        bool isMovingLeft = targetX < startPosition.x;

        while (elapsed < duration && !isDead && !isCutscenePlaying)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            progress = progress * progress * (3f - 2f * progress);
            Vector3 position = Vector3.Lerp(startPosition, new Vector3(targetX, startPosition.y, startPosition.z), progress);
            transform.position = position;
            isFacingLeft = isMovingLeft;
            ApplyFacingDirection();

            if (elapsed >= nextAfterimageTime)
            {
                SpawnUnderSlashAfterimage();
                nextAfterimageTime = elapsed + 0.04f;
            }

            yield return null;
        }

        if (!isDead && !isCutscenePlaying)
            transform.position = new Vector3(targetX, startPosition.y, startPosition.z);
    }

    private bool TryGetArenaBounds(out Bounds arenaBounds)
    {
        Collider2D[] colliders = FindObjectsByType<Collider2D>();
        bool foundGround = false;
        arenaBounds = default;

        foreach (Collider2D groundCollider in colliders)
        {
            if (groundCollider == null || !groundCollider.enabled ||
                (groundLayer.value & (1 << groundCollider.gameObject.layer)) == 0)
                continue;

            if (!foundGround)
            {
                arenaBounds = groundCollider.bounds;
                foundGround = true;
            }
            else
            {
                arenaBounds.Encapsulate(groundCollider.bounds);
            }
        }

        return foundGround;
    }

    private bool TryGetBossWallBounds(out float wallLeftX, out float wallRightX)
    {
        wallLeftX = 0f;
        wallRightX = 0f;
        if (bossWallLeft == null || bossWallRight == null)
            return false;

        wallLeftX = Mathf.Min(bossWallLeft.position.x, bossWallRight.position.x);
        wallRightX = Mathf.Max(bossWallLeft.position.x, bossWallRight.position.x);
        return wallRightX > wallLeftX;
    }

    private bool TryGetGroundY(float x, Bounds arenaBounds, out float groundY)
    {
        Vector2 rayOrigin = new Vector2(x, arenaBounds.max.y + 10f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down,
            arenaBounds.size.y + 20f, groundLayer);
        if (hit.collider == null)
        {
            groundY = 0f;
            return false;
        }

        groundY = hit.point.y;
        return true;
    }

    private void LandOnGround(float x, float groundY)
    {
        float feetOffset = bossSpriteRenderer != null
            ? bossSpriteRenderer.bounds.min.y - transform.position.y
            : 0f;
        transform.position = new Vector3(x, groundY - feetOffset, transform.position.z);
        previousPosition = transform.position;
    }

    private void SpawnUnderSlashAfterimage()
    {
        if (bossSpriteRenderer == null || bossSpriteRenderer.sprite == null)
            return;

        GameObject afterimage = new GameObject("BossUnderSlashAfterimage");
        afterimage.transform.SetPositionAndRotation(bossSpriteRenderer.transform.position,
            bossSpriteRenderer.transform.rotation);
        afterimage.transform.localScale = bossSpriteRenderer.transform.lossyScale;

        SpriteRenderer afterimageRenderer = afterimage.AddComponent<SpriteRenderer>();
        afterimageRenderer.sprite = bossSpriteRenderer.sprite;
        afterimageRenderer.flipX = bossSpriteRenderer.flipX;
        afterimageRenderer.sortingLayerID = bossSpriteRenderer.sortingLayerID;
        afterimageRenderer.sortingOrder = bossSpriteRenderer.sortingOrder - 1;
        Color color = bossSpriteRenderer.color;
        color.a *= 0.45f;
        afterimageRenderer.color = color;
        StartCoroutine(FadeUnderSlashAfterimage(afterimageRenderer));
    }

    private IEnumerator FadeUnderSlashAfterimage(SpriteRenderer afterimageRenderer)
    {
        const float duration = 0.2f;
        Color originalColor = afterimageRenderer.color;
        float elapsed = 0f;

        while (elapsed < duration && afterimageRenderer != null)
        {
            Color color = originalColor;
            color.a *= 1f - elapsed / duration;
            afterimageRenderer.color = color;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (afterimageRenderer != null)
            Destroy(afterimageRenderer.gameObject);
    }

    public void BossUnderSlashHit()
    {
        if (isDead || isCutscenePlaying || !isUnderSlashPatternPlaying || underSlashEventTriggered)
            return;

        underSlashEventTriggered = true;
        if (!TryGetArenaBounds(out Bounds arenaBounds) ||
            !TryGetBossWallBounds(out float wallLeftX, out float wallRightX) ||
            !TryGetGroundY((wallLeftX + wallRightX) * 0.5f, arenaBounds, out float groundY))
        {
            Debug.LogError("Could not determine the boss room floor for the under-slash hit.", this);
            return;
        }

        float wallSpan = wallRightX - wallLeftX;
        float wallCenterX = (wallLeftX + wallRightX) * 0.5f;
        Vector2 effectPosition = new Vector2(wallCenterX, groundY);
        ShowUnderSlashEffect(effectPosition);

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            new Vector2(wallCenterX, groundY + 0.5f),
            new Vector2(wallSpan, 1f),
            0f,
            playerLayer);
        ApplyDamage(hits, new HashSet<IDamageable>(), underSlashDamage);
    }

    private void CacheUnderSlashEffect()
    {
        if (underSlashEffectObject == null)
        {
            Debug.LogError("UnderSlashEffect scene object is not assigned.", this);
            return;
        }

        underSlashEffectRenderer = underSlashEffectObject.GetComponent<SpriteRenderer>();
        if (underSlashEffectRenderer == null)
        {
            Debug.LogError("UnderSlashEffect scene object is missing a SpriteRenderer.", underSlashEffectObject);
            return;
        }

        underSlashEffectRenderer.sortingLayerID = bossSpriteRenderer != null
            ? bossSpriteRenderer.sortingLayerID
            : 0;
        underSlashEffectRenderer.sortingOrder = 100;
        underSlashEffectOriginalColor = underSlashEffectRenderer.color;
        underSlashEffectRenderer.enabled = false;
    }

    private void ShowUnderSlashEffect(Vector2 position)
    {
        if (underSlashEffectObject == null || underSlashEffectRenderer == null)
        {
            Debug.LogError("Boss under-slash effect sprite is not assigned.", this);
            return;
        }

        if (underSlashEffectFadeRoutine != null)
            StopCoroutine(underSlashEffectFadeRoutine);

        underSlashEffectObject.transform.position = position;
        underSlashEffectRenderer.flipX = isFacingLeft;
        underSlashEffectRenderer.color = underSlashEffectOriginalColor;
        underSlashEffectRenderer.enabled = true;
        underSlashEffectFadeRoutine = StartCoroutine(FadeOutUnderSlashEffect());
    }

    private IEnumerator FadeOutUnderSlashEffect()
    {
        float elapsed = 0f;
        while (elapsed < underSlashEffectFadeDuration)
        {
            Color color = underSlashEffectOriginalColor;
            color.a *= 1f - elapsed / Mathf.Max(0.01f, underSlashEffectFadeDuration);
            underSlashEffectRenderer.color = color;
            elapsed += Time.deltaTime;
            yield return null;
        }

        Color fadedColor = underSlashEffectOriginalColor;
        fadedColor.a = 0f;
        underSlashEffectRenderer.color = fadedColor;
        underSlashEffectRenderer.enabled = false;
        underSlashEffectFadeRoutine = null;
    }

    private IEnumerator WaitForNormalAttackAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(normalAttackAnimation))
            yield break;

        while (!isDead && !isCutscenePlaying &&
               animator.GetCurrentAnimatorStateInfo(0).IsName(normalAttackAnimation))
        {
            yield return null;
        }
    }

    public void BossNormalAttackFlash()
    {
        if (!isDead && !isCutscenePlaying)
            normalAttackEventTriggered = true;
    }

    public void BossStrikeHit()
    {
        if (isDead || isCutscenePlaying || !isStrikePatternPlaying || strikeEventTriggered)
            return;

        strikeEventTriggered = true;
        Vector2 direction = isFacingLeft ? Vector2.left : Vector2.right;
        Vector2 spriteCenter = bossSpriteRenderer != null
            ? bossSpriteRenderer.bounds.center
            : transform.position;
        Vector2 attackCenter = spriteCenter + direction * (strikeRange * 0.5f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter,
            new Vector2(strikeRange, strikeWidth), 0f, playerLayer);

        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        ApplyDamage(hits, damagedTargets, strikeDamage);

        ShowStrikeEffect(attackCenter);
        cameraFollow?.Shake(strikeCameraShakeDuration, strikeCameraShakeMagnitude);
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
        ShowAttackEffect(effectPosition, direction);

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
        ApplyDamage(effectHits, damagedTargets, GetAttackDamage());
    }

    private void ApplyPathDamage(RaycastHit2D[] hits, HashSet<IDamageable> damagedTargets)
    {
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null)
                ApplyDamage(hit.collider, damagedTargets, GetAttackDamage());
        }
    }

    private void ApplyDamage(Collider2D[] hits, HashSet<IDamageable> damagedTargets, float damage)
    {
        foreach (Collider2D hit in hits)
        {
            ApplyDamage(hit, damagedTargets, damage);
        }
    }

    private void ApplyDamage(Collider2D hit, HashSet<IDamageable> damagedTargets, float damage)
    {
        if (hit == null || hit.GetComponentInParent<PlayerStat>() == null)
            return;

        IDamageable damageable = hit.GetComponentInParent<IDamageable>();
        if (damageable != null && damagedTargets.Add(damageable))
            damageable.TakeDamage(damage);
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

    private void ShowAttackEffect(Vector2 position, Vector2 direction)
    {
        if (normalAttackEffect == null) return;

        normalAttackEffect.transform.position = position;
        normalAttackEffect.SetActive(true);
        for (int i = 0; i < effectRenderers.Length; i++)
        {
            effectRenderers[i].enabled = true;
            effectRenderers[i].color = effectOriginalColors[i];
            effectRenderers[i].flipX = effectOriginalFlipX[i] ^ (direction.x < 0f);
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

    private void CacheStrikeEffect()
    {
        if (strikeEffectObject == null)
            return;

        strikeEffectRenderer = strikeEffectObject.GetComponent<SpriteRenderer>();
        if (strikeEffectRenderer == null)
        {
            Debug.LogError("Strike effect object is missing a SpriteRenderer.", strikeEffectObject);
            return;
        }

        if (bossSpriteRenderer != null)
        {
            strikeEffectRenderer.sortingLayerID = bossSpriteRenderer.sortingLayerID;
            strikeEffectRenderer.sortingOrder = bossSpriteRenderer.sortingOrder + 1;
        }

        strikeEffectOriginalColor = strikeEffectRenderer.color;
        strikeEffectObject.SetActive(false);
    }

    private void ShowStrikeEffect(Vector2 position)
    {
        if (strikeEffectObject == null)
            return;

        if (strikeEffectFadeRoutine != null)
            StopCoroutine(strikeEffectFadeRoutine);

        strikeEffectObject.transform.position = position;
        strikeEffectRenderer.flipX = isFacingLeft;
        strikeEffectRenderer.color = strikeEffectOriginalColor;
        strikeEffectObject.SetActive(true);
        strikeEffectFadeRoutine = StartCoroutine(FadeOutStrikeEffect());
    }

    private IEnumerator FadeOutStrikeEffect()
    {
        float elapsed = 0f;
        while (elapsed < strikeEffectFadeDuration)
        {
            Color color = strikeEffectOriginalColor;
            color.a *= 1f - elapsed / Mathf.Max(0.01f, strikeEffectFadeDuration);
            strikeEffectRenderer.color = color;
            elapsed += Time.deltaTime;
            yield return null;
        }

        strikeEffectObject.SetActive(false);
        strikeEffectFadeRoutine = null;
    }

    private void CacheEffectRenderers()
    {
        if (normalAttackEffect == null) return;

        effectRenderers = normalAttackEffect.GetComponentsInChildren<SpriteRenderer>(true);
        effectOriginalColors = new Color[effectRenderers.Length];
        effectOriginalFlipX = new bool[effectRenderers.Length];
        for (int i = 0; i < effectRenderers.Length; i++)
        {
            effectOriginalColors[i] = effectRenderers[i].color;
            effectOriginalFlipX[i] = effectRenderers[i].flipX;
        }
        normalAttackEffect.SetActive(false);
    }

    private void UpdateFacingFromMovement()
    {
        float horizontalMovement = transform.position.x - previousPosition.x;
        previousPosition = transform.position;

        if (isAttackPatternPlaying)
            return;

        if (Mathf.Abs(horizontalMovement) <= 0.001f)
            return;

        isFacingLeft = horizontalMovement < 0f;

        if (bossSpriteRenderer != null)
            bossSpriteRenderer.flipX = !isFacingLeft;
    }

    private void FacePlayer()
    {
        if (isAttackPatternPlaying || playerTransform == null)
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

    public void StartBattle()
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
