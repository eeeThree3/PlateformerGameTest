using System;
using System.Collections;
using UnityEngine;

public class PlayerStat : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Attack")]
    [SerializeField] private int attackPower = 10;

    [Header("Hit Stun")]
    [SerializeField] private float hitStunDuration = 0.3f; // 피격 시 경직 지속 시간

    private PlayerDash playerDash;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int AttackPower => attackPower;
    public bool IsDead => currentHealth <= 0;

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

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);
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