using Config;
using MainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI winText;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Game References")]
    public LevelGenerator levelGenerator;
    public GameObject ball;

    private int score;
    private int mapLevelNumber = 1;
    private int currentHelixLevel = 1;
    private int helixLevelsToComplete = 1;
    private int additionalFloorsPerCompletedLevel = 2;
    private bool gameOver;
    private bool levelTransition;
    private bool mapLevelCompleted;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        ReadSelectedLevelConfig();
        SetupButtons();
        HideEndUi();
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void LevelComplete()
    {
        if (gameOver || levelTransition)
            return;

        levelTransition = true;

        if (currentHelixLevel >= helixLevelsToComplete)
        {
            CompleteFinalHelixLevel();
            levelTransition = false;
            return;
        }

        currentHelixLevel++;

        if (levelGenerator != null)
        {
            levelGenerator.numberOfLevels += additionalFloorsPerCompletedLevel;
            levelGenerator.GenerateLevel();
        }

        ResetBall();
        UpdateUI();
        levelTransition = false;
    }

    public void AddScore(int points)
    {
        if (gameOver)
            return;

        score += points;
        UpdateUI();
    }

    public void GameOver()
    {
        if (gameOver)
            return;

        gameOver = true;

        if (gameOverText != null)
        {
            ShowConfiguredText(gameOverText, "Game Over! Score: {score}", 0, false);
        }

        ShowRestartButton(true);
        ShowMainMenuButton(true);

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;

        if (GameLauncher.Instance != null)
        {
            GameLauncher.Instance.ReturnToMainMenu();
            return;
        }

        SceneManager.LoadScene(0);
    }

    private void ReadSelectedLevelConfig()
    {
        if (GameLauncher.Instance == null)
            return;

        if (GameLauncher.Instance.CurrentLevelNumber > 0)
            mapLevelNumber = GameLauncher.Instance.CurrentLevelNumber;

        LevelData levelData = GameLauncher.Instance.GetCurrentLevelData();
        if (levelData == null || levelData.EffectiveGameType != LevelGameType.Helix)
            return;

        helixLevelsToComplete = Mathf.Max(1, levelData.helix.levelsToComplete);
        additionalFloorsPerCompletedLevel = Mathf.Max(0, levelData.helix.additionalFloorsPerCompletedLevel);
    }

    private void SetupButtons()
    {
        EnsureEventSystem();

        if (restartButton != null)
        {
            ReplaceButtonAction(restartButton, RestartGame);
        }

        if (mainMenuButton != null)
            ReplaceButtonAction(mainMenuButton, ReturnToMenu);
    }

    private void HideEndUi()
    {
        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);

        if (winText != null)
            winText.gameObject.SetActive(false);

        ShowRestartButton(false);
        ShowMainMenuButton(false);
    }

    private void CompleteFinalHelixLevel()
    {
        int coins = CompleteMapLevelOnce();

        gameOver = true;

        if (winText != null)
        {
            ShowConfiguredText(winText, "Level Complete!\nScore: {score}\nCoins: +{coins}", coins, false);
        }
        else if (gameOverText != null)
        {
            ShowConfiguredText(gameOverText, "Level Complete!\nScore: {score}\nCoins: +{coins}", coins, true);
        }

        ShowRestartButton(false);
        ShowMainMenuButton(true);

        Time.timeScale = 0f;
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();

        if (levelText != null)
        {
            if (helixLevelsToComplete > 1)
                levelText.text = "Level " + currentHelixLevel + "/" + helixLevelsToComplete;
            else
                levelText.text = "Level " + mapLevelNumber;
        }
    }

    private void ResetBall()
    {
        if (ball == null)
            return;

        ball.transform.position = new Vector3(0f, 5f, -1.5f);

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private int CompleteMapLevelOnce()
    {
        if (mapLevelCompleted)
            return 0;

        if (GameLauncher.Instance != null && GameLauncher.Instance.IsBonusMode)
        {
            int coins = SkinRewardCalculator.HelixCoins(score);
            GameLauncher.Instance.CompleteLevel(3, score, coins);
            mapLevelCompleted = true;
            return coins;
        }

        return 0;
    }

    private void ShowRestartButton(bool isVisible)
    {
        if (restartButton == null)
            return;

        restartButton.gameObject.SetActive(isVisible);
    }

    private void ShowMainMenuButton(bool isVisible)
    {
        if (mainMenuButton == null)
            return;

        mainMenuButton.gameObject.SetActive(isVisible);
    }

    private void ReplaceButtonAction(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
    }

    private void ShowConfiguredText(TextMeshProUGUI label, string fallbackTemplate, int coins, bool forceFallbackTemplate)
    {
        if (label == null)
            return;

        string template = forceFallbackTemplate ? fallbackTemplate : label.text;
        if (string.IsNullOrWhiteSpace(template))
            template = fallbackTemplate;

        label.text = FormatResultText(template, coins);
        label.gameObject.SetActive(true);
    }

    private string FormatResultText(string template, int coins)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return template
            .Replace("{score}", score.ToString())
            .Replace("{coins}", coins.ToString())
            .Replace("{level}", mapLevelNumber.ToString());
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
            return;
        }

        if (eventSystem.GetComponent<BaseInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }
}
