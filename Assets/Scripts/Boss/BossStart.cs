using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class BossStart : MonoBehaviour
{
	[SerializeField] private LayerMask playerLayer;
	[SerializeField] private bool triggerOnce = true;
	[SerializeField] private Transform cameraFocusPoint;
	[SerializeField] private float cameraMoveDuration = 1f;
	[SerializeField] private float cameraOrthographicSize = 8f;
	[SerializeField] private PlayableDirector bossTimeline;
	[SerializeField] private BossController bossController;

	public bool HasPlayerEntered { get; private set; }

	private PlayerMove playerMove;
	private PlayerDash playerDash;
	private PlayerAttack playerAttack;
	private PlayerInputHandler playerInputHandler;
	private PlayerInput playerInput;
	private Rigidbody2D playerRigidbody;
	private Transform playerTransform;
	private SpriteRenderer playerSpriteRenderer;
	private float previousPlayerX;
	private RigidbodyType2D originalBodyType;
	private bool originalPlayerMoveEnabled;
	private bool originalPlayerDashEnabled;
	private bool originalPlayerAttackEnabled;
	private bool originalPlayerInputEnabled;
	private bool originalPlayerInputComponentEnabled;
	private bool playerLocked;

	private void LateUpdate()
	{
		if (!playerLocked || playerSpriteRenderer == null || playerTransform == null)
			return;

		float horizontalMovement = playerTransform.position.x - previousPlayerX;
		if (Mathf.Abs(horizontalMovement) > 0.001f)
			playerSpriteRenderer.flipX = horizontalMovement < 0f;

		previousPlayerX = playerTransform.position.x;
	}

	private void Awake()
	{
		if (bossController == null)
			bossController = FindAnyObjectByType<BossController>();

		Collider2D areaCollider = GetComponent<Collider2D>();
		if (areaCollider == null || !areaCollider.isTrigger)
		{
			Debug.LogError("BossStart requires a Collider2D with Is Trigger enabled.", this);
		}

		if (playerLayer.value == 0)
		{
			int layer = LayerMask.NameToLayer("Player");
			if (layer >= 0)
			{
				playerLayer = 1 << layer;
			}
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		TryDetectPlayer(other);
	}

	private void OnTriggerStay2D(Collider2D other)
	{
		TryDetectPlayer(other);
	}

	private void TryDetectPlayer(Collider2D other)
	{
		if (triggerOnce && HasPlayerEntered)
		{
			return;
		}

		bool isPlayerLayer = (playerLayer.value & (1 << other.gameObject.layer)) != 0;
		bool hasPlayerComponent = other.GetComponentInParent<PlayerStat>() != null;
		if (!isPlayerLayer && !hasPlayerComponent)
		{
			return;
		}

		HasPlayerEntered = true;
		CachePlayerComponents(other);
		StartBossCutscene();
	}

	private void StartBossCutscene()
	{
		MainCameraFollow cameraFollow = FindAnyObjectByType<MainCameraFollow>();
		Transform focusPoint = cameraFocusPoint != null ? cameraFocusPoint : transform;

		if (bossTimeline != null)
		{
			bossController?.BeginCutscene();
			LockPlayerForCutscene();
			bossTimeline.stopped += HandleTimelineStopped;
			if (cameraFollow != null)
			{
				cameraFollow.FocusOn(focusPoint, cameraMoveDuration, cameraOrthographicSize);
			}

			bossTimeline.Play();
			return;
		}

		if (cameraFollow == null)
		{
			return;
		}

		cameraFollow.FocusOn(focusPoint, cameraMoveDuration, cameraOrthographicSize);
	}

	private void CachePlayerComponents(Collider2D other)
	{
		PlayerStat playerStat = other.GetComponentInParent<PlayerStat>();
		if (playerStat == null)
			return;

		GameObject playerObject = playerStat.gameObject;
		playerTransform = playerObject.transform;
		playerMove = playerObject.GetComponent<PlayerMove>();
		playerDash = playerObject.GetComponent<PlayerDash>();
		playerAttack = playerObject.GetComponent<PlayerAttack>();
		playerInputHandler = playerObject.GetComponent<PlayerInputHandler>();
		playerInput = playerObject.GetComponent<PlayerInput>();
		playerRigidbody = playerObject.GetComponent<Rigidbody2D>();
		playerSpriteRenderer = playerObject.GetComponentInChildren<SpriteRenderer>();
	}

	private void LockPlayerForCutscene()
	{
		if (playerLocked)
			return;

		if (playerMove != null)
		{
			originalPlayerMoveEnabled = playerMove.enabled;
			playerMove.enabled = false;
		}

		if (playerDash != null)
		{
			originalPlayerDashEnabled = playerDash.enabled;
			playerDash.enabled = false;
		}

		if (playerAttack != null)
		{
			originalPlayerAttackEnabled = playerAttack.enabled;
			playerAttack.enabled = false;
		}

		if (playerInputHandler != null)
		{
			originalPlayerInputEnabled = playerInputHandler.enabled;
			playerInputHandler.enabled = false;
		}

		if (playerInput != null)
		{
			originalPlayerInputComponentEnabled = playerInput.enabled;
			playerInput.enabled = false;
		}

		if (playerRigidbody != null)
		{
			originalBodyType = playerRigidbody.bodyType;
			playerRigidbody.linearVelocity = Vector2.zero;
			playerRigidbody.bodyType = RigidbodyType2D.Kinematic;
		}

		if (playerSpriteRenderer != null)
		{
			playerSpriteRenderer.flipX = false;
			previousPlayerX = playerTransform != null ? playerTransform.position.x : playerSpriteRenderer.transform.position.x;
		}

		playerLocked = true;
	}

	private void HandleTimelineStopped(PlayableDirector director)
	{
		director.stopped -= HandleTimelineStopped;
		bossController?.EndCutscene();
		UnlockPlayerAfterCutscene();
	}

	private void UnlockPlayerAfterCutscene()
	{
		if (!playerLocked)
			return;

		playerInputHandler?.ClearMoveInput();
		playerMove?.ResetMovementState();

		if (playerRigidbody != null)
			playerRigidbody.bodyType = originalBodyType;
		if (playerInputHandler != null)
			playerInputHandler.enabled = originalPlayerInputEnabled;
		if (playerInput != null)
			playerInput.enabled = originalPlayerInputComponentEnabled;
		if (playerAttack != null)
			playerAttack.enabled = originalPlayerAttackEnabled;
		if (playerDash != null)
			playerDash.enabled = originalPlayerDashEnabled;
		if (playerMove != null)
			playerMove.enabled = originalPlayerMoveEnabled;

		playerLocked = false;
	}
}
