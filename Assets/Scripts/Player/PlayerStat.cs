using System;
using System.Collections;
using UnityEngine;

public class PlayerStat : MonoBehaviour, IDamageable
{
    private const int SelfHealAmount = 50;
    private const int MaxSelfHealUses = 3;
    private const float SelfHealHoldDuration = 3f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Attack")]
    [SerializeField] private int attackPower = 10;

    [Header("Hit Stun")]
    [SerializeField] private float hitStunDuration = 0.3f; // 피격 시 경직 지속 시간

    private PlayerDash playerDash;
    private PlayerInputHandler inputHandler;
    private Coroutine selfHealRoutine;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int AttackPower => attackPower;
    public bool IsDead => currentHealth <= 0;
    private int selfHealUses;

    // 이동/공격 스크립트에서 이 값을 체크해서 경직 중엔 입력을 무시하도록 연동해주세요.
    // 예) if (playerStat.IsHitStunned) return;  (Update()나 Move() 맨 앞)
    public bool IsHitStunned { get; private set; }

    public event Action<int, int> HealthChanged;   // (currentHealth, maxHealth)
    public event Action<int> AttackPowerChanged;   // (attackPower)
    public event Action Died;
    public event Action<float> HitStunStarted;     // (경직 시간) - 넉백/피격 이펙트 등에서 구독
    public event Action HitStunEnded;

    private Coroutine stunRoutine;

    private void Awake()
    {
        currentHealth = maxHealth;
        playerDash = GetComponent<PlayerDash>();
        inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void OnEnable()
    {
        if (inputHandler != null)
        {
            inputHandler.SelfHealPressed += StartSelfHeal;
            inputHandler.SelfHealReleased += CancelSelfHeal;
        }
    }

    private void OnDisable()
    {
        if (inputHandler != null)
        {
            inputHandler.SelfHealPressed -= StartSelfHeal;
            inputHandler.SelfHealReleased -= CancelSelfHeal;
        }

        CancelSelfHeal();
    }

    private void Start()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
        AttackPowerChanged?.Invoke(attackPower);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0 || (playerDash != null && playerDash.IsDashing)) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        Debug.Log($"[{gameObject.name}] took {amount} damage. HP: {currentHealth}/{maxHealth}");
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Died?.Invoke();
        }
        else
        {
            StartHitStun();
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(Mathf.RoundToInt(damage));
    }

    private void StartHitStun()
    {
        if (stunRoutine != null)
            StopCoroutine(stunRoutine);

        stunRoutine = StartCoroutine(HitStunRoutine());
    }

    private IEnumerator HitStunRoutine()
    {
        IsHitStunned = true;
        HitStunStarted?.Invoke(hitStunDuration);

        yield return new WaitForSeconds(hitStunDuration);

        IsHitStunned = false;
        HitStunEnded?.Invoke();
        stunRoutine = null;
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth > previousHealth)
            ShowHealOutline();
    }

    private void ShowHealOutline()
    {
        SpriteRenderer source = GetComponent<SpriteRenderer>();
        if (source == null || source.sprite == null)
        {
            Debug.LogError("Cannot show healing outline: player SpriteRenderer or sprite is missing.", this);
            return;
        }

        GameObject outlineObject = new GameObject("HealOutline");
        outlineObject.transform.SetParent(source.transform, false);
        outlineObject.transform.localScale = Vector3.one * 1.16f;

        SpriteRenderer outline = outlineObject.AddComponent<SpriteRenderer>();
        outline.sprite = source.sprite;
        outline.drawMode = source.drawMode;
        outline.size = source.size;
        outline.flipX = source.flipX;
        outline.flipY = source.flipY;
        outline.maskInteraction = source.maskInteraction;
        outline.sharedMaterial = source.sharedMaterial;
        outline.sortingLayerID = source.sortingLayerID;
        outline.sortingOrder = source.sortingOrder - 1;
        outline.color = Color.green;

        StartCoroutine(FadeHealOutline(source, outline));
    }

    private IEnumerator FadeHealOutline(SpriteRenderer source, SpriteRenderer outline)
    {
        const float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration && source != null && outline != null)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            outline.sprite = source.sprite;
            outline.flipX = source.flipX;
            outline.flipY = source.flipY;

            Color color = Color.green;
            color.a = 1f - progress;
            outline.color = color;
            yield return null;
        }

        if (outline != null)
            Destroy(outline.gameObject);
    }

    private void StartSelfHeal()
    {
        if (selfHealRoutine != null)
            StopCoroutine(selfHealRoutine);

        selfHealRoutine = StartCoroutine(SelfHealAfterHold());
    }

    private void CancelSelfHeal()
    {
        if (selfHealRoutine == null)
            return;

        StopCoroutine(selfHealRoutine);
        selfHealRoutine = null;
    }

    private IEnumerator SelfHealAfterHold()
    {
        yield return new WaitForSeconds(SelfHealHoldDuration);

        if (selfHealUses >= MaxSelfHealUses)
        {
            ShowSelfHealUnavailable();
        }
        else
        {
            Heal(SelfHealAmount);
            selfHealUses++;
        }

        selfHealRoutine = null;
    }

    private void ShowSelfHealUnavailable()
    {
        GameObject markerObject = new GameObject("SelfHealUnavailable");
        markerObject.transform.SetParent(transform, false);
        Vector3 startPosition = new Vector3(0f, 1.25f, 0f);
        markerObject.transform.localPosition = startPosition;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            markerObject.transform.rotation = Quaternion.LookRotation(-mainCamera.transform.forward, mainCamera.transform.up);

        TextMesh marker = markerObject.AddComponent<TextMesh>();
        marker.text = "X";
        marker.anchor = TextAnchor.MiddleCenter;
        marker.alignment = TextAlignment.Center;
        marker.fontSize = 100;
        marker.characterSize = 0.1f;
        marker.color = Color.red;

        MeshRenderer markerRenderer = markerObject.GetComponent<MeshRenderer>();
        markerRenderer.sortingOrder = 10;

        StartCoroutine(AnimateSelfHealUnavailable(markerObject, marker, startPosition));
    }

    private IEnumerator AnimateSelfHealUnavailable(GameObject markerObject, TextMesh marker, Vector3 startPosition)
    {
        const float duration = 1.5f;
        const float riseDistance = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            markerObject.transform.localPosition = startPosition + Vector3.up * (riseDistance * progress);
            Color color = Color.red;
            color.a = 1f - progress;
            marker.color = color;
            yield return null;
        }

        Destroy(markerObject);
    }

    public void SetAttackPower(int newAttackPower)
    {
        attackPower = Mathf.Max(newAttackPower, 0);
        AttackPowerChanged?.Invoke(attackPower);
    }

    public void ModifyAttackPower(int delta)
    {
        SetAttackPower(attackPower + delta);
    }
}