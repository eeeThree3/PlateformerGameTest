using UnityEngine;
using UnityEngine.SceneManagement;

public class MainCameraFollow : MonoBehaviour
{
    [SerializeField] private float heightOffset = 4f;

    private Transform playerTarget;
    private Camera cameraComponent;
    private Coroutine focusRoutine;
    private bool isFocused;
    private Vector3 basePosition;
    private Vector3 shakeOffset;
    private float shakeDuration;
    private float shakeTimeRemaining;
    private float shakeMagnitude;

    public bool IsFocused => isFocused;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        basePosition = transform.position;
        FindPlayerTarget();
    }

    private void LateUpdate()
    {
        if (!isFocused)
        {
            if (playerTarget == null)
                FindPlayerTarget();

            if (playerTarget != null)
            {
                basePosition.x = playerTarget.position.x;
                basePosition.y = playerTarget.position.y + heightOffset;
            }
        }

        shakeOffset = Vector3.zero;
        if (shakeTimeRemaining > 0f)
        {
            shakeTimeRemaining = Mathf.Max(0f, shakeTimeRemaining - Time.deltaTime);
            if (shakeTimeRemaining > 0f)
            {
                float strength = shakeMagnitude * shakeTimeRemaining / shakeDuration;
                float noiseTime = Time.time * 30f;
                shakeOffset = new Vector3(
                    (Mathf.PerlinNoise(noiseTime, 0f) * 2f - 1f) * strength,
                    (Mathf.PerlinNoise(0f, noiseTime + 17f) * 2f - 1f) * strength,
                    0f);
            }
            else
            {
                shakeDuration = 0f;
                shakeMagnitude = 0f;
            }
        }

        transform.position = basePosition + shakeOffset;
    }

    public void Shake(float duration, float magnitude)
    {
        if (duration <= 0f || magnitude <= 0f)
            return;

        shakeDuration = Mathf.Max(shakeDuration, duration);
        shakeTimeRemaining = Mathf.Max(shakeTimeRemaining, duration);
        shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
    }

    public void FocusOn(Transform focusPoint, float duration, float targetOrthographicSize)
    {
        if (focusPoint == null)
            return;

        if (focusRoutine != null)
            StopCoroutine(focusRoutine);

        focusRoutine = StartCoroutine(FocusRoutine(focusPoint, duration, targetOrthographicSize));
    }

    private System.Collections.IEnumerator FocusRoutine(Transform focusPoint, float duration, float targetOrthographicSize)
    {
        isFocused = true;

        Vector3 startPosition = basePosition;
        Vector3 targetPosition = new Vector3(focusPoint.position.x, focusPoint.position.y, basePosition.z);
        float startSize = cameraComponent != null ? cameraComponent.orthographicSize : 0f;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            basePosition = targetPosition;
            SetOrthographicSize(targetOrthographicSize);
            focusRoutine = null;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            progress = progress * progress * (3f - 2f * progress);

            basePosition = Vector3.Lerp(startPosition, targetPosition, progress);
            if (cameraComponent != null && targetOrthographicSize > 0f)
                cameraComponent.orthographicSize = Mathf.Lerp(startSize, targetOrthographicSize, progress);

            yield return null;
        }

        basePosition = targetPosition;
        SetOrthographicSize(targetOrthographicSize);
        focusRoutine = null;
    }

    private void SetOrthographicSize(float size)
    {
        if (cameraComponent != null && size > 0f)
            cameraComponent.orthographicSize = size;
    }

    private void FindPlayerTarget()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer < 0)
            return;

        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects)
        {
            Transform target = FindLayeredTransform(rootObject.transform, playerLayer);
            if (target != null)
            {
                playerTarget = target;
                return;
            }
        }
    }

    private Transform FindLayeredTransform(Transform current, int playerLayer)
    {
        if (current.gameObject.layer == playerLayer)
            return current;

        foreach (Transform child in current)
        {
            Transform target = FindLayeredTransform(child, playerLayer);
            if (target != null)
                return target;
        }

        return null;
    }
}
