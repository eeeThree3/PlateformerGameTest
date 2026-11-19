using UnityEngine;
using UnityEngine.UI;

public class BossBar : MonoBehaviour
{
    private static readonly Color BarColor = Color.yellow;

    private BossStat bossStat;
    private GameObject canvasObject;
    private Slider healthSlider;

    private void Awake()
    {
        bossStat = GetComponent<BossStat>();
        if (bossStat == null)
        {
            Debug.LogError("BossBar requires a BossStat component.", this);
            return;
        }

        CreateHealthBar();
    }

    private void OnEnable()
    {
        if (bossStat != null)
        {
            bossStat.OnHealthChanged += UpdateHealthBar;
            bossStat.OnDeath += Hide;
        }
    }

    private void OnDisable()
    {
        if (bossStat != null)
        {
            bossStat.OnHealthChanged -= UpdateHealthBar;
            bossStat.OnDeath -= Hide;
        }
    }

    private void OnDestroy()
    {
        if (canvasObject != null)
            Destroy(canvasObject);
    }

    public void Show()
    {
        if (bossStat == null || canvasObject == null)
            return;

        canvasObject.SetActive(true);
        UpdateHealthBar(bossStat.CurrentHealth, bossStat.MaxHealth);
    }

    private void Hide()
    {
        if (canvasObject != null)
            canvasObject.SetActive(false);
    }

    private void CreateHealthBar()
    {
        canvasObject = new GameObject("BossHealthCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject barObject = new GameObject("BossHealthBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        barObject.transform.SetParent(canvasObject.transform, false);

        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 1f);
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0f, -48f);
        barRect.sizeDelta = new Vector2(600f, 28f);

        Image background = barObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(barObject.transform, false);
        Image fill = fillObject.GetComponent<Image>();
        fill.color = BarColor;

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);

        healthSlider = barObject.GetComponent<Slider>();
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.interactable = false;
        healthSlider.transition = Selectable.Transition.None;
        healthSlider.targetGraphic = background;
        healthSlider.fillRect = fillRect;
        healthSlider.direction = Slider.Direction.LeftToRight;

        GameObject labelObject = new GameObject("BossLabel", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -82f);
        labelRect.sizeDelta = new Vector2(240f, 44f);

        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 32;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = BarColor;
        label.text = "Boss";

        canvasObject.SetActive(false);
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (healthSlider != null)
            healthSlider.value = max > 0f ? current / max : 0f;
    }
}
