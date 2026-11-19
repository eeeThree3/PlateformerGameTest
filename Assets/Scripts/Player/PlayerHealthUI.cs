using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    private static readonly Color HealthBarColor = Color.red;
    private static readonly Color HealthBarBackgroundColor = new Color(0f, 0f, 0f, 0.85f);

    [Header("References")]
    [SerializeField] private PlayerStat playerStat;
    [SerializeField] private Slider healthSlider;   // Slider의 min=0, max=1로 설정 권장
    [SerializeField] private Text healthText;        // 없으면 비워둬도 됨 (예: "80 / 100")

    private void Awake()
    {
        StyleHealthBar();
    }

    private void OnEnable()
    {
        if (playerStat != null)
        {
            playerStat.HealthChanged += UpdateHealthUI;
            UpdateHealthUI(playerStat.CurrentHealth, playerStat.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (playerStat != null)
        {
            playerStat.HealthChanged -= UpdateHealthUI;
        }
    }

    private void StyleHealthBar()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.interactable = false;
            healthSlider.transition = Selectable.Transition.None;
            healthSlider.direction = Slider.Direction.LeftToRight;

            Image fill = healthSlider.fillRect != null
                ? healthSlider.fillRect.GetComponent<Image>()
                : null;
            if (fill != null)
                fill.color = HealthBarColor;

            Transform backgroundTransform = healthSlider.transform.Find("Background");
            Image background = backgroundTransform != null
                ? backgroundTransform.GetComponent<Image>()
                : null;
            if (background != null)
            {
                background.color = HealthBarBackgroundColor;
                healthSlider.targetGraphic = background;
            }
        }

        if (healthText != null)
        {
            healthText.color = HealthBarColor;
            healthText.fontStyle = FontStyle.Bold;
        }
    }

    private void UpdateHealthUI(int current, int max)
    {
        if (healthSlider != null)
        {
            healthSlider.value = max > 0 ? (float)current / max : 0f;
        }

        if (healthText != null)
        {
            healthText.text = $"{current} / {max}";
        }
    }
}