using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDash : MonoBehaviour
{
    public event Action<bool> DashStateChanged;
    public event Action DashTriggered;

    [Header("References")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Rigidbody2D rigidbody2D;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerAttack playerAttack; 

    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("Options")]
    [SerializeField] private bool cameraRelative = true;
    [SerializeField] private bool stopGravityDuringDash = true;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    public bool IsDashing => isDashing;

    private bool isDashing;
    private float cooldownTimer;
    private float originalGravityScale;
    private Vector2 lastMoveDirection = Vector2.right;
    private readonly bool[] originalLayerCollisions = new bool[32];
    private bool layerCollisionsIgnored;

    private void Awake()
    {
        if (inputHandler == null)
        {
            inputHandler = GetComponent<PlayerInputHandler>();
        }

        if (playerAttack == null)   // 추가
        {
            playerAttack = GetComponent<PlayerAttack>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (rigidbody2D == null)
        {
            rigidbody2D = GetComponent<Rigidbody2D>();
        }

        if (rigidbody2D != null)
        {
            originalGravityScale = rigidbody2D.gravityScale;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (groundCheck == null)
        {
            groundCheck = transform;
        }

        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        Debug.Log($"[Dash] Awake - inputHandler={inputHandler != null}, CharacterController={characterController != null}, Rigidbody2D={rigidbody2D != null}, Camera={cameraTransform != null}");
    }

    private void OnEnable()
    {
        if (inputHandler != null)
        {
            inputHandler.DashPressed += HandleDash;
            Debug.Log("[Dash] subscribed to DashPressed event");
        }
        else
        {
            Debug.LogWarning("[Dash] inputHandler is null in OnEnable");
        }
    }

    private void OnDisable()
    {
        if (inputHandler != null)
        {
            inputHandler.DashPressed -= HandleDash;
            Debug.Log("[Dash] unsubscribed from DashPressed event");
        }

        RestoreIgnoredCollisions();
        isDashing = false;
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer < 0f)
            {
                cooldownTimer = 0f;
            }
        }

        if (inputHandler != null && inputHandler.MoveInput.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = inputHandler.MoveInput.normalized;
        }

        if (inputHandler == null && Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame)
        {
            Debug.Log("[Dash] fallback Z key pressed this frame");
            HandleDash();
        }
    }

    private void HandleDash()
    {
        Debug.Log($"[Dash] HandleDash called - isDashing={isDashing}, cooldown={cooldownTimer}, inputHandler={(inputHandler != null)}");

        if (playerAttack != null && playerAttack.IsAttacking)
        {
            Debug.Log("[Dash] rejected: attacking");
            return;
        }
        
        if (isDashing)
        {
            Debug.Log("[Dash] rejected: already dashing");
            return;
        }

        if (cooldownTimer > 0f)
        {
            Debug.Log($"[Dash] rejected: cooldown active ({cooldownTimer:F2}s)");
            return;
        }

        if (inputHandler == null)
        {
            inputHandler = GetComponent<PlayerInputHandler>();
            Debug.Log($"[Dash] inputHandler auto-found: {inputHandler != null}");
        }

        Vector2 input = lastMoveDirection;
        Debug.Log($"[Dash] dash direction uses lastMoveDirection = {input}");

        if (input.sqrMagnitude < 0.01f)
        {
            input = Vector2.right;
            Debug.Log("[Dash] still no direction, fallback to right");
        }

        lastMoveDirection = input.normalized;

        Vector3 dashDirection = GetDashDirection(input);
        Debug.Log($"[Dash] dashDirection computed = {dashDirection}");

        if (dashDirection.sqrMagnitude < 0.01f)
        {
            Debug.Log("[Dash] rejected: dashDirection is zero");
            return;
        }

        Debug.Log($"[Dash] accepted: starting coroutine with direction={dashDirection}");
        StartCoroutine(DashRoutine(dashDirection));
    }

    private Vector3 GetDashDirection(Vector2 input)
    {
        Vector2 moveDir = input.sqrMagnitude > 0.01f ? input.normalized : lastMoveDirection.normalized;

        bool onSlope = IsOnSlope();
        if (onSlope)
        {
            Vector2 slopeDirection = GetSlopeDirection();
            if (slopeDirection.sqrMagnitude > 0.01f)
            {
                float sign = Mathf.Sign(moveDir.x == 0f ? lastMoveDirection.x : moveDir.x);
                if (sign == 0f)
                {
                    sign = 1f;
                }

                Vector2 desired = slopeDirection * sign;
                return new Vector3(desired.x, desired.y, 0f).normalized;
            }
        }

        if (rigidbody2D != null)
        {
            Vector3 direction = new Vector3(moveDir.x, 0f, 0f);
            return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.zero;
        }

        if (cameraRelative && cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            Vector3 direction = forward * moveDir.y + right * moveDir.x;

            if (direction.sqrMagnitude > 0.01f)
            {
                return direction.normalized;
            }
        }

        return new Vector3(moveDir.x, 0f, moveDir.y).normalized;
    }

    private bool IsOnSlope()
    {
        if (groundCheck == null)
        {
            return false;
        }

        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.7f, groundLayer);
        if (hit.collider == null)
        {
            return false;
        }

        Vector2 normal = hit.normal.normalized;
        float angle = Vector2.Angle(Vector2.up, normal);
        return angle > 2f && angle < 80f;
    }

    private Vector2 GetSlopeDirection()
    {
        if (groundCheck == null)
        {
            return Vector2.zero;
        }

        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.7f, groundLayer);
        if (hit.collider == null)
        {
            return Vector2.zero;
        }

        Vector2 normal = hit.normal.normalized;
        float angle = Vector2.Angle(Vector2.up, normal);
        if (angle <= 2f || angle >= 80f)
        {
            return Vector2.zero;
        }

        return new Vector2(normal.y, -normal.x).normalized;
    }

    private IEnumerator DashRoutine(Vector3 direction)
    {
        isDashing = true;
        cooldownTimer = dashCooldown;
        DashStateChanged?.Invoke(true);
        DashTriggered?.Invoke();
        IgnoreNonGroundCollisions();

        if (rigidbody2D != null && stopGravityDuringDash)
        {
            rigidbody2D.gravityScale = 0f;
        }

        float elapsed = 0f;
        float dashSpeed = dashDistance / dashDuration;
        float ghostSpawnInterval = 0.03f;
        float nextGhostTime = 0f;

        Debug.Log($"[Dash] routine started - direction={direction}, speed={dashSpeed}, duration={dashDuration}");

        while (elapsed < dashDuration)
        {
            float deltaTime = Mathf.Min(Time.deltaTime, dashDuration - elapsed);
            Vector3 movement = direction * dashSpeed * deltaTime;

            if (characterController != null)
            {
                characterController.Move(movement);
                Debug.Log($"[Dash] CharacterController.Move({movement})");
            }
            else if (rigidbody2D != null)
            {
                Vector2 dashVelocity = new Vector2(direction.x, direction.y) * dashSpeed;
                rigidbody2D.linearVelocity = dashVelocity;
                Debug.Log($"[Dash] Rigidbody2D velocity set to {rigidbody2D.linearVelocity}");
            }

            elapsed += deltaTime;

            if (elapsed >= nextGhostTime)
            {
                SpawnDashGhost();
                nextGhostTime += ghostSpawnInterval;
            }

            yield return null;
        }

        if (rigidbody2D != null && stopGravityDuringDash)
        {
            rigidbody2D.gravityScale = originalGravityScale;
        }

        RestoreIgnoredCollisions();
        isDashing = false;
        DashStateChanged?.Invoke(false);
        Debug.Log("[Dash] routine finished");
    }

    private void IgnoreNonGroundCollisions()
    {
        if (layerCollisionsIgnored)
            return;

        int playerLayer = gameObject.layer;
        for (int otherLayer = 0; otherLayer < 32; otherLayer++)
        {
            if ((groundLayer.value & (1 << otherLayer)) != 0)
                continue;

            originalLayerCollisions[otherLayer] = Physics2D.GetIgnoreLayerCollision(playerLayer, otherLayer);
            Physics2D.IgnoreLayerCollision(playerLayer, otherLayer, true);
        }

        layerCollisionsIgnored = true;
    }

    private void RestoreIgnoredCollisions()
    {
        if (!layerCollisionsIgnored)
            return;

        int playerLayer = gameObject.layer;
        for (int otherLayer = 0; otherLayer < 32; otherLayer++)
        {
            if ((groundLayer.value & (1 << otherLayer)) != 0)
                continue;

            Physics2D.IgnoreLayerCollision(playerLayer, otherLayer, originalLayerCollisions[otherLayer]);
        }

        layerCollisionsIgnored = false;
    }

    private void OnDestroy()
    {
        RestoreIgnoredCollisions();
    }

    private void SpawnDashGhost()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        int ghostCount = 10;
        for (int i = 0; i < ghostCount; i++)
        {
            GameObject ghost = new GameObject("DashGhost");
            ghost.transform.position = transform.position;
            ghost.transform.rotation = transform.rotation;
            ghost.transform.localScale = transform.localScale;

            SpriteRenderer ghostRenderer = ghost.AddComponent<SpriteRenderer>();
            ghostRenderer.sprite = spriteRenderer.sprite;
            ghostRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
            ghostRenderer.flipX = spriteRenderer.flipX;
            ghostRenderer.color = new Color(1f, 1f, 1f, 0.9f);

            Material ghostMaterial = new Material(Shader.Find("Sprites/Default"));
            ghostRenderer.material = ghostMaterial;

            float ghostDuration = 0.1f + i * 0.03f;
            var ghostFade = ghost.AddComponent<DashGhostFade>();
            ghostFade.Setup(ghostDuration, ghostRenderer, 0.9f - i * 0.08f);
        }
    }

    private class DashGhostFade : MonoBehaviour
    {
        private float duration;
        private float elapsed;
        private SpriteRenderer spriteRenderer;
        private float startAlpha;

        public void Setup(float ghostDuration, SpriteRenderer targetRenderer, float initialAlpha)
        {
            duration = ghostDuration;
            spriteRenderer = targetRenderer;
            startAlpha = initialAlpha;
            elapsed = 0f;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Max(0f, alpha);
                spriteRenderer.color = color;
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}