using MainMenu;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinSystem : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private TowerGenerator towerGenerator;
    [SerializeField] private ObstacleGenerator obstacleGenerator;
    [SerializeField] private GameObject winCanvas; // 👈 ОТДЕЛЬНЫЙ Canvas!
    [SerializeField] private PlayerController playerController;
    

    [Header("Тексты")]
    [SerializeField] private TextMeshProUGUI winTitleText; // Заголовок "ПОБЕДА!"
    [SerializeField] private TextMeshProUGUI winStatsText; // Статистика (сколько блоков, время и т.д.)

    [Header("Настройки победы")]
    [SerializeField] private string winMessage = "ПОБЕДА! 🎉";
    [SerializeField] private Color winColor = Color.green;
    [SerializeField] private float pauseDelay = 2f;

    [Header("Кнопки (опционально)")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    private bool isWin = false;
    private int totalBlocksAtStart = 0;
    private Coroutine pauseRoutine;

    private void Awake()
    {
        if (towerGenerator == null)
            towerGenerator = GetComponent<TowerGenerator>();

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        // Проверяем Canvas
        if (winCanvas == null)
        {
            Debug.LogError("❌ WinSystem: winCanvas не назначен! Создаю автоматически...");
            CreateWinCanvas();
        }
        else
        {
            winCanvas.SetActive(false);
        }

        // Подписываемся на кнопки
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuClicked);
    }

    private void OnDestroy()
    {
        // Отписываемся от кнопок
        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);

        if (menuButton != null)
            menuButton.onClick.RemoveListener(OnMenuClicked);
    }

    private void OnEnable()
    {
        if (towerGenerator != null)
        {
            towerGenerator.OnBlockDestroyed += OnBlockDestroyedHandler;
            towerGenerator.OnTowerGenerated += OnTowerGeneratedHandler;
        }
    }

    private void OnDisable()
    {
        if (towerGenerator != null)
        {
            towerGenerator.OnBlockDestroyed -= OnBlockDestroyedHandler;
            towerGenerator.OnTowerGenerated -= OnTowerGeneratedHandler;
        }
    }

    private void OnTowerGeneratedHandler(int totalBlocks)
    {
        totalBlocksAtStart = totalBlocks;
        isWin = false;
        FireballRoundState.Reset();
        SetPlayerControls(true);

        // Снимаем паузу
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        CancelInvoke(nameof(PauseGame));
        StopPauseRoutine();

        // Прячем Canvas победы
        if (winCanvas != null)
            winCanvas.SetActive(false);

        Debug.Log($"🏰 Башня создана! Всего блоков: {totalBlocksAtStart}");
    }

    private void OnBlockDestroyedHandler(int destroyedCount)
    {
        int remainingBlocks = towerGenerator.GetRemainingBlocks();
        Debug.Log($"💥 Блоков осталось: {remainingBlocks}/{totalBlocksAtStart}");

        if (!isWin && remainingBlocks <= 0)
        {
            IsWinningSon();
        }
    }

    private void IsWinningSon()
    {
        if (isWin) return;
        if (!FireballRoundState.TryFinish()) return;

        isWin = true;
        SetPlayerControls(false);

        Debug.Log($"🎉🎉🎉 ПОБЕДА! Уничтожена башня из {totalBlocksAtStart} блоков! 🎉🎉🎉");

        // Обновляем тексты
        if (winTitleText != null)
        {
            winTitleText.text = winMessage;
            winTitleText.color = winColor;
        }

        if (winStatsText != null)
        {
            int score = towerGenerator.GetCurrentScore();
            int coins = SkinRewardCalculator.FireballCoins(score);
            winStatsText.text = $"Блоков уничтожено: {totalBlocksAtStart}\nОчки: {score}\nМонеты: +{coins}";
        }

        if (GameLauncher.Instance != null)
        {
            int score = towerGenerator.GetCurrentScore();
            int coins = SkinRewardCalculator.FireballCoins(score);
            GameLauncher.Instance.CompleteLevel(3, score, coins);
        }

        // Показываем Canvas СРАЗУ
        if (winCanvas != null)
        {
            winCanvas.SetActive(true);

            // Анимация появления (опционально)
            CanvasGroup cg = winCanvas.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = winCanvas.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
        }

        // Планируем паузу через N секунд
        StartPauseRoutine();
    }

    private void PauseGame()
    {
        if (!isWin) return;

        Debug.Log($"⏸ Игра на паузе!");
        Time.timeScale = 0f;

        // Курсор уже должен быть виден, но на всякий случай
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void StartPauseRoutine()
    {
        StopPauseRoutine();
        pauseRoutine = StartCoroutine(PauseAfterDelayRoutine());
    }

    private IEnumerator PauseAfterDelayRoutine()
    {
        float delay = Mathf.Max(0f, pauseDelay);
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        pauseRoutine = null;
        PauseGame();
    }

    private void StopPauseRoutine()
    {
        if (pauseRoutine == null)
            return;

        StopCoroutine(pauseRoutine);
        pauseRoutine = null;
    }

    private void SetPlayerControls(bool isEnabled)
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (playerController != null)
            playerController.SetControlsEnabled(isEnabled);
    }

    #region Создание Canvas (если не назначен в инспекторе)

    private void CreateWinCanvas()
    {
        // Создаём корневой Canvas
        GameObject canvasGO = new GameObject("WinCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Поверх всего!

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Добавляем CanvasGroup для анимаций
        CanvasGroup canvasGroup = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        // Создаём фон
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);

        RectTransform bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);

        // Создаём контейнер для контента
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(canvasGO.transform, false);

        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(600, 400);
        contentRect.anchoredPosition = Vector2.zero;

        // Создаём заголовок победы
        GameObject titleGO = new GameObject("WinTitle");
        titleGO.transform.SetParent(contentGO.transform, false);

        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(400, 100);
        titleRect.anchoredPosition = new Vector2(0, -50);

        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = winMessage;
        titleText.color = winColor;
        titleText.fontSize = 72;
        titleText.alignment = TextAlignmentOptions.Center;

        // Создаём текст статистики
        GameObject statsGO = new GameObject("StatsText");
        statsGO.transform.SetParent(contentGO.transform, false);

        RectTransform statsRect = statsGO.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.5f, 0.5f);
        statsRect.anchorMax = new Vector2(0.5f, 0.5f);
        statsRect.pivot = new Vector2(0.5f, 0.5f);
        statsRect.sizeDelta = new Vector2(400, 100);
        statsRect.anchoredPosition = new Vector2(0, 0);

        TextMeshProUGUI statsText = statsGO.AddComponent<TextMeshProUGUI>();
        statsText.text = "Блоков уничтожено: 0\nОчки: 0";
        statsText.color = Color.white;
        statsText.fontSize = 36;
        statsText.alignment = TextAlignmentOptions.Center;

        // Создаём заглушку для кнопок
        GameObject buttonsGO = new GameObject("Buttons");
        buttonsGO.transform.SetParent(contentGO.transform, false);

        RectTransform buttonsRect = buttonsGO.AddComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0.5f, 0f);
        buttonsRect.anchorMax = new Vector2(0.5f, 0f);
        buttonsRect.pivot = new Vector2(0.5f, 0f);
        buttonsRect.sizeDelta = new Vector2(400, 100);
        buttonsRect.anchoredPosition = new Vector2(0, 50);

        TextMeshProUGUI buttonsText = buttonsGO.AddComponent<TextMeshProUGUI>();
        buttonsText.text = "▶ СЛЕДУЮЩИЙ УРОВЕНЬ\n↺ ЗАНОВО\n🏠 В МЕНЮ";
        buttonsText.color = Color.gray;
        buttonsText.fontSize = 28;
        buttonsText.alignment = TextAlignmentOptions.Center;

        // Сохраняем ссылки
        winCanvas = canvasGO;
        winTitleText = titleText;
        winStatsText = statsText;

        winCanvas.SetActive(false);

        Debug.Log("✅ WinCanvas создан автоматически!");
    }

    #endregion

    #region Обработчики кнопок

    private void OnNextLevelClicked()
    {
        Debug.Log("📌 Следующий уровень");
        ResetWin();
       
        // Завершить текущий уровень (3 звезды)
        if (GameLauncher.Instance != null)
        {
            GameLauncher.Instance.CompleteLevel(3);
            GameLauncher.Instance.LaunchNextLevel();
        }
    }

    private void OnRestartClicked()
    {
        Debug.Log("📌 Полный рестарт уровня");
        
        if (GameLauncher.Instance != null)
        {
            // Сбрасываем Time.timeScale перед рестартом
            Time.timeScale = 1f;
            GameLauncher.Instance.RestartLevel();
        }
    }

    private void OnMenuClicked()
    {
        Debug.Log("📌 Выход в меню");
        
        // Сбрасываем генераторы для новой игры
        if (towerGenerator != null)
        {
            towerGenerator.ResetForNewGame();
        }
        
        if (GameLauncher.Instance != null)
        {
            Time.timeScale = 1f;
            GameLauncher.Instance.ReturnToMenu();
        }
    }

    #endregion

    public void ResetWin()
    {
        isWin = false;
        totalBlocksAtStart = 0;
        FireballRoundState.Reset();
        SetPlayerControls(true);
        StopPauseRoutine();

        if (obstacleGenerator != null)
        {
            obstacleGenerator.ResetForNewGame();
        }

        if (winCanvas != null)
            winCanvas.SetActive(false);

        Time.timeScale = 1f;
        CancelInvoke(nameof(PauseGame));
    }
}
