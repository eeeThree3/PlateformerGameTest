using UnityEngine;
using System.Collections;

public class HitEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.15f;

    private bool isSpawnedEffect;
    private SpriteRenderer[] spriteRenderers;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void Start()
    {
        if (!isSpawnedEffect)
            SetRenderersVisible(false);
    }

    public void SpawnAt(Vector3 position)
    {
        HitEffect spawnedEffect = Instantiate(this, position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
        spawnedEffect.isSpawnedEffect = true;
        spawnedEffect.SetRenderersVisible(true);
        spawnedEffect.StartCoroutine(spawnedEffect.DestroyAfterDuration());
    }

    private IEnumerator DestroyAfterDuration()
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    private void SetRenderersVisible(bool isVisible)
    {
        if (spriteRenderers == null) return;

        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            spriteRenderer.enabled = isVisible;
    }
}
