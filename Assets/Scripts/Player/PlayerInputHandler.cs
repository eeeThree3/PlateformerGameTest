using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 이름을 PlayerInput이 아니라 PlayerInputHandler로 지은 이유:
// UnityEngine.InputSystem 패키지에 이미 "PlayerInput"이라는 컴포넌트가 있어서
// 이름이 겹치면 GetComponent<PlayerInput>() 호출 시 헷갈릴 수 있음.
public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; } = Vector2.zero;

    private float lastDashInvokeTime;
    private PlayerInput playerInput;
    private InputAction dashAction;
    private InputAction selfHealAction;

    // 다른 스크립트는 이 이벤트만 구독하면 됨. PlayerMove/PlayerAnimator를 전혀 몰라도 됨.
    public event Action<Vector2> MoveInputChanged;
    public event Action JumpPressed;
    public event Action DashPressed;
    public event Action AttackPressed;
    public event Action OnAttackInput;
    public event Action SelfHealPressed;
    public event Action SelfHealReleased;

    public void ClearMoveInput()
    {
        if (MoveInput == Vector2.zero)
            return;

        MoveInput = Vector2.zero;
        MoveInputChanged?.Invoke(MoveInput);
    }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        if (playerInput == null)
            return;

        dashAction = playerInput.actions.FindAction("Player/Dash", false);
        if (dashAction == null)
        {
            Debug.LogError("Player input action 'Player/Dash' was not found.", this);
        }
        else
        {
            dashAction.performed += HandleDashActionPerformed;
        }

        selfHealAction = playerInput.actions.FindAction("Player/SelfHeal", false);
        if (selfHealAction == null)
        {
            Debug.LogError("Player input action 'Player/SelfHeal' was not found.", this);
        }
        else
        {
            selfHealAction.started += HandleSelfHealStarted;
            selfHealAction.canceled += HandleSelfHealCanceled;
        }
    }

    private void OnDisable()
    {
        if (dashAction != null)
        {
            dashAction.performed -= HandleDashActionPerformed;
            dashAction = null;
        }

        if (selfHealAction != null)
        {
            selfHealAction.started -= HandleSelfHealStarted;
            selfHealAction.canceled -= HandleSelfHealCanceled;
            selfHealAction = null;
        }

        SelfHealReleased?.Invoke();
    }

    private void Update()
    {
        if (playerInput != null && playerInput.enabled)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        Vector2 keyboardMove = Vector2.zero;
        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            keyboardMove.x -= 1f;
        if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            keyboardMove.x += 1f;
        if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed)
            keyboardMove.y += 1f;
        if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
            keyboardMove.y -= 1f;

        if (keyboardMove != MoveInput)
        {
            MoveInput = keyboardMove;
            Debug.Log($"[Input] Fallback Move -> {MoveInput}");
            MoveInputChanged?.Invoke(MoveInput);
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            Debug.Log("[Input] Fallback Jump performed");
            JumpPressed?.Invoke();
        }

        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            float now = Time.unscaledTime;
            if (now - lastDashInvokeTime >= 0.08f)
            {
                lastDashInvokeTime = now;
                Debug.Log("[Input] Fallback Dash performed");
                DashPressed?.Invoke();
            }
        }

        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            Debug.Log("[Input] Fallback Attack performed");
            AttackPressed?.Invoke();
        }

        bool selfHealIsPressed = Keyboard.current.fKey.isPressed;
        if (selfHealIsPressed)
        {
            if (!fallbackSelfHealIsPressed)
            {
                fallbackSelfHealIsPressed = true;
                SelfHealPressed?.Invoke();
            }
        }
        else if (fallbackSelfHealIsPressed)
        {
            fallbackSelfHealIsPressed = false;
            SelfHealReleased?.Invoke();
        }
    }

    private bool fallbackSelfHealIsPressed;

    private void HandleSelfHealStarted(InputAction.CallbackContext context)
    {
        SelfHealPressed?.Invoke();
    }

    private void HandleSelfHealCanceled(InputAction.CallbackContext context)
    {
        SelfHealReleased?.Invoke();
    }

    private void HandleDashActionPerformed(InputAction.CallbackContext context)
    {
        RaiseDashPressed();
    }

    // Unity PlayerInput 컴포넌트(Send Messages 또는 Invoke Unity Events)가 호출
    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
        Debug.Log($"[Input] OnMove -> {MoveInput}");
        MoveInputChanged?.Invoke(MoveInput);
    }
    

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("[Input] Jump performed");
            JumpPressed?.Invoke();
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnAttackInput?.Invoke();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
            RaiseDashPressed();
    }

    private void RaiseDashPressed()
    {
        float now = Time.unscaledTime;
        if (now - lastDashInvokeTime < 0.08f)
        {
            Debug.Log($"[Input] Dash duplicate event ignored ({now - lastDashInvokeTime:F3}s)");
            return;
        }

        lastDashInvokeTime = now;
        Debug.Log("[Input] Dash performed");
        DashPressed?.Invoke();
    }
}
