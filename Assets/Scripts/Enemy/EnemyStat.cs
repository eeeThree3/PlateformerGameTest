using System;
using UnityEngine;

public class EnemyStat : MonoBehaviour, IDamageable
{
    [Header("Base Stats")]
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float defense = 0f;       // 방어력 (데미지 감소량)
    [SerializeField] private float knockbackResistance = 0f; // 0~1, 1이면 넉백 안 먹음
    [SerializeField] private float deathAnimationDuration = 1f;

    public event Action<float, float> OnHealthChanged; // (현재체력, 최대체력) - UI/이펙트용
    public event Action OnDamaged;                       // 피격 이펙트/사운드용
    public event Action OnDeath;                          // 사망 처리용

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        float finalDamage = Mathf.Max(0f, damage - defense);
        CurrentHealth -= finalDamage;
        CurrentHealth = Mathf.Max(0f, CurrentHealth);

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        OnDamaged?.Invoke();

        Debug.Log($"[{gameObject.name}] took {finalDamage} damage. HP: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    // 넉백까지 같이 처리하고 싶을 때 쓰는 오버로드 (선택사항)
    public void TakeDamage(float damage, Vector2 knockback, Rigidbody2D targetRb)
    {
        TakeDamage(damage);

        if (targetRb != null && knockbackResistance < 1f)
        {
            targetRb.linearVelocity = Vector2.zero;
            targetRb.AddForce(knockback * (1f - knockbackResistance), ForceMode2D.Impulse);
        }
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        OnDeath?.Invoke();
        Debug.Log($"[{gameObject.name}] died");

        // 간단하게는 바로 파괴, 나중에 사망 애니메이션 넣으면 이 부분만 교체
        Destroy(gameObject, deathAnimationDuration);
    }
}