using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BossAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BossStat bossStat;

    [Header("Animator Parameters")]
    [SerializeField] private string walkingParameter = "isWalking";
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string deathTrigger = "Death";

    [Header("Movement")]
    [SerializeField] private float movementThreshold = 0.001f;

    private Vector3 previousPosition;
    private bool isDead;
    private bool isCutscenePlaying;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (bossStat == null) bossStat = GetComponent<BossStat>();

        previousPosition = transform.position;
    }

    private void OnEnable()
    {
        if (bossStat == null) return;

        bossStat.OnDamaged += HandleDamaged;
        bossStat.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (bossStat == null) return;

        bossStat.OnDamaged -= HandleDamaged;
        bossStat.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (animator == null || isDead || isCutscenePlaying)
        {
            previousPosition = transform.position;
            return;
        }

        float horizontalMovement = transform.position.x - previousPosition.x;
        bool isWalking = Mathf.Abs(horizontalMovement) > movementThreshold;

        animator.SetBool(walkingParameter, isWalking);
        animator.SetFloat(speedParameter, Mathf.Abs(horizontalMovement) / Mathf.Max(Time.deltaTime, 0.0001f));

        previousPosition = transform.position;
    }

    private void HandleDamaged()
    {
        if (!isDead && !isCutscenePlaying && animator != null)
            animator.SetTrigger(hitTrigger);
    }

    public void PlayAttackAnimation()
    {
        if (!isDead && !isCutscenePlaying && animator != null)
            animator.SetTrigger(attackTrigger);
    }

    public void SetCutsceneMode(bool enabled)
    {
        isCutscenePlaying = enabled;
        previousPosition = transform.position;

        if (enabled && animator != null)
            animator.SetBool(walkingParameter, false);
    }

    private void HandleDeath()
    {
        isDead = true;

        if (animator != null)
        {
            animator.SetBool(walkingParameter, false);
            animator.SetTrigger(deathTrigger);
        }
    }
}
