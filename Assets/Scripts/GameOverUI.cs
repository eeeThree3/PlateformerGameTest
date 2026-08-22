using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStat playerStat;
    [SerializeField] private GameObject gameOverPanel; // 재시작 버튼이 들어있는 UI 패널
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        if (playerStat == null)
            playerStat = FindAnyObjectByType<PlayerStat>();

        if (restartButton == null && gameOverPanel != null)
            restartButton = gameOverPanel.GetComponentInChildren<Button>(true);
    }

    private void OnEnable()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnClickRestart);

        if (playerStat != null)
        {
            playerStat.Died += HandlePlayerDied;
        }
    }

    private void OnDisable()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnClickRestart);

        if (playerStat != null)
        {
            playerStat.Died -= HandlePlayerDied;
        }
    }

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void HandlePlayerDied()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Time.timeScale = 0f; // 게임 정지 (Update 기반 로직도 모두 멈춤)
    }

    // 재시작 버튼의 OnClick()에 이 메서드를 연결하세요
    public void OnClickRestart()
    {
        Time.timeScale = 1f; // 다음 씬에서 정상 동작하도록 timeScale 복구는 필수
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}