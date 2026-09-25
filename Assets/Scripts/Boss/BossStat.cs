using UnityEngine;

public class BossStat : EnemyStat
{
    [Header("Boss Stats")]
    [SerializeField] private float attackPower = 20f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float contactDamage = 10f;

    public float AttackPower => attackPower;
    public float MoveSpeed => moveSpeed;
    public float ContactDamage => contactDamage;
}
