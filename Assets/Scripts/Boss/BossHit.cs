using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class BossHit : MonoBehaviour, IDamageable
{
    [Header("Hitbox")]
    [SerializeField] private float hitboxRadius = 0.5f;

    [Header("References")]
    [SerializeField] private BossStat bossStat;

    private CircleCollider2D hitboxCollider;

    private void Awake()
    {
        hitboxCollider = GetComponent<CircleCollider2D>();

        if (bossStat == null)
            bossStat = GetComponentInParent<BossStat>();

        ApplyHitboxSettings();
    }

    private void OnValidate()
    {
        hitboxRadius = Mathf.Max(0f, hitboxRadius);

        if (hitboxCollider == null)
            hitboxCollider = GetComponent<CircleCollider2D>();

        ApplyHitboxSettings();
    }

    public void TakeDamage(float damage)
    {
        if (bossStat == null)
        {
            Debug.LogWarning($"[{gameObject.name}] BossStat reference is missing.", this);
            return;
        }

        bossStat.TakeDamage(damage);
        Debug.Log($"[{bossStat.gameObject.name}] current HP: {bossStat.CurrentHealth}/{bossStat.MaxHealth}");
    }

    private void ApplyHitboxSettings()
    {
        if (hitboxCollider == null) return;

        hitboxCollider.radius = hitboxRadius;
        hitboxCollider.isTrigger = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, hitboxRadius);
    }
}
