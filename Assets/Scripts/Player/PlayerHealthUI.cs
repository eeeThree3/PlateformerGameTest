using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStat playerStat;
    [SerializeField] private Slider healthSlider;   // Slider의 min=0, max=1로 설정 권장
    [SerializeField] private Text healthText;        // 없으면 비워둬도 됨 (예: "80 / 100")

    private void OnEnable()
    {
        if (playerStat != null)
        {
            playerStat.HealthChanged += UpdateHealthUI;
        }
    }

    private void OnDisable()
    {
        if (playerStat != null)
        {
            playerStat.HealthChanged -= UpdateHealthUI;
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