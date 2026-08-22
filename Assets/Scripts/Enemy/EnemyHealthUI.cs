using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    [SerializeField] private EnemyStat enemyStat;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.75f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(1f, 0.12f);

    private GameObject healthBarObject;
    private Slider healthSlider;

    private void Awake()
    {
        if (enemyStat == null)
        {
            enemyStat = GetComponent<EnemyStat>();
        }
    }

    private void OnEnable()
    {
        if (enemyStat != null)
        {
            enemyStat.OnHealthChanged += UpdateHealthUI;
            enemyStat.OnDeath += HideHealthBar;
        }
    }

    private void Start()
    {
        CreateHealthBar();
        UpdateHealthUI(enemyStat.CurrentHealth, enemyStat.MaxHealth);
    }

    private void OnDisable()
    {
        if (enemyStat != null)
        {
            enemyStat.OnHealthChanged -= UpdateHealthUI;
            enemyStat.OnDeath -= HideHealthBar;
        }
    }

    private void CreateHealthBar()
    {
        healthBarObject = new GameObject("EnemyHealthBar");
        healthBarObject.transform.SetParent(transform, false);
        healthBarObject.transform.localPosition = offset;

        Canvas canvas = healthBarObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = healthBarObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100f, 12f);
        canvasRect.localScale = new Vector3(barSize.x / 100f, barSize.y / 12f, 1f);

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(healthBarObject.transform, false);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = Color.black;

        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        healthSlider = healthBarObject.AddComponent<Slider>();
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.interactable = false;
        healthSlider.targetGraphic = background;

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(healthBarObject.transform, false);
        Image fill = fillObject.AddComponent<Image>();
        fill.color = Color.red;

        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        healthSlider.fillRect = fillRect;
        healthSlider.direction = Slider.Direction.LeftToRight;
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.value = max > 0f ? current / max : 0f;
        }
    }

    private void HideHealthBar()
    {
        if (healthBarObject != null)
        {
            healthBarObject.SetActive(false);
        }
    }
}
