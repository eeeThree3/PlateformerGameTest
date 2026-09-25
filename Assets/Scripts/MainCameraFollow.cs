using UnityEngine;
using UnityEngine.SceneManagement;

public class MainCameraFollow : MonoBehaviour
{
    [SerializeField] private float heightOffset = 4f;

    private Transform playerTarget;
    private Camera cameraComponent;
    private Coroutine focusRoutine;
    private bool isFocused;

    public bool IsFocused => isFocused;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        FindPlayerTarget();
    }

    private void LateUpdate()
    {
        if (isFocused)
            return;

        if (playerTarget == null)
        {
            FindPlayerTarget();
            if (playerTarget == null)
                return;
        }

        Vector3 cameraPosition = transform.position;
        cameraPosition.x = playerTarget.position.x;
        cameraPosition.y = playerTarget.position.y + heightOffset;
        transform.position = cameraPosition;
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

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = new Vector3(focusPoint.position.x, focusPoint.position.y, transform.position.z);
        float startSize = cameraComponent != null ? cameraComponent.orthographicSize : 0f;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            transform.position = targetPosition;
            SetOrthographicSize(targetOrthographicSize);
            focusRoutine = null;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            progress = progress * progress * (3f - 2f * progress);

            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
            if (cameraComponent != null && targetOrthographicSize > 0f)
                cameraComponent.orthographicSize = Mathf.Lerp(startSize, targetOrthographicSize, progress);

            yield return null;
        }

        transform.position = targetPosition;
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
