using UnityEngine;
using UnityEngine.SceneManagement;

public class MainCameraFollow : MonoBehaviour
{
    [SerializeField] private float heightOffset = 4f;

    private Transform playerTarget;

    private void Awake()
    {
        FindPlayerTarget();
    }

    private void LateUpdate()
    {
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
