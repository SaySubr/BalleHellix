using MainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Система отображения поражения (Game Over).
/// Аналогично WinSystem, но для проигрыша.
/// </summary>
public class LoseSystem : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private ObstacleGenerator obstacleGenerator;
    [SerializeField] private TankHealth tankHealth;
    [SerializeField] private TowerGenerator towerGenerator; // Для подсчёта блоков
    [SerializeField] private GameObject loseCanvas; // ОТДЕЛЬНЫЙ Canvas для поражения!

    [Header("Тексты")]
    [SerializeField] private TextMeshProUGUI loseTitleText; // Заголовок "ПОРАЖЕНИЕ!"
    [SerializeField] private TextMeshProUGUI loseStatsText; // Статистика

    [Header("Настройки поражения")]
    [SerializeField] private string loseMessage = "ПОРАЖЕНИЕ! 💀";
    [SerializeField] private Color loseColor = Color.red;
    [SerializeField] private float pauseDelay = 1.5f;

    [Header("Кнопки (опционально)")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    private bool isLose = false;

    private void Awake()
    {
        // Проверяем Canvas
        if (loseCanvas == null)
        {
            Debug.LogError("❌ LoseSystem: loseCanvas не назначен! Создаю автоматически...");
            CreateLoseCanvas();
        }
        else
        {
            loseCanvas.SetActive(false);
        }

        // Ищем TankHealth если не назначен
        if (tankHealth == null)
        {
            tankHealth = GetComponent<TankHealth>();
        }

        // Ищем ObstacleGenerator если не назначен
        if (obstacleGenerator == null)
        {
            obstacleGenerator = GetComponent<ObstacleGenerator>();
        }

        // Подписываемся на кнопки
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuClicked);
    }

    private void OnDestroy()
    {
        // Отписываемся от кнопок
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);

        if (menuButton != null)
            menuButton.onClick.RemoveListener(OnMenuClicked);
    }

    private void OnEnable()
    {
        // Подписываемся на разрушение танка
        if (tankHealth != null)
        {
            tankHealth.OnTankDestroyed += OnTankDestroyedHandler;
        }
    }

    private void OnDisable()
    {
        if (tankHealth != null)
        {
            tankHealth.OnTankDestroyed -= OnTankDestroyedHandler;
        }
    }

    private void OnTankDestroyedHandler()
    {
        ShowLoseScreen();
    }

    public void ShowLoseScreen()
    {
        if (isLose) return;
        isLose = true;

        Debug.Log($"💀💀💀 ПОРАЖЕНИЕ! Танк уничтожен! 💀💀💀");

        // Обновляем тексты
        if (loseTitleText != null)
        {
            loseTitleText.text = loseMessage;
            loseTitleText.color = loseColor;
        }

        if (loseStatsText != null)
        {
            // Получаем количество оставшихся блоков башни
            int remainingBlocks = towerGenerator != null
                ? towerGenerator.GetRemainingBlocks()
                : 0;

            // Получаем количество оставшихся препятствий
            int obstaclesRemaining = obstacleGenerator != null
                ? obstacleGenerator.GetRemainingObstacles()
                : 0;

            loseStatsText.text = $"Блоков осталось: {remainingBlocks}\nПрепятствий: {obstaclesRemaining}";
        }

        // Показываем Canvas СРАЗУ
        if (loseCanvas != null)
        {
            loseCanvas.SetActive(true);

            // Анимация появления (опционально)
            CanvasGroup cg = loseCanvas.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = loseCanvas.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
        }

        // Планируем паузу через N секунд
        Invoke(nameof(PauseGame), pauseDelay);
    }

    private void PauseGame()
    {
        if (!isLose) return;

        Debug.Log($"⏸ Игра на паузе (Game Over)!");
        Time.timeScale = 0f;

        // Показываем курсор
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    #region Создание Canvas (если не назначен в инспекторе)

    private void CreateLoseCanvas()
    {
        // Создаём корневой Canvas
        GameObject canvasGO = new GameObject("LoseCanvas");
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
        bgImage.color = new Color(0.3f, 0, 0, 0.8f); // Красноватый фон

        // Создаём контейнер для контента
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(canvasGO.transform, false);

        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(600, 400);
        contentRect.anchoredPosition = Vector2.zero;

        // Создаём заголовок поражения
        GameObject titleGO = new GameObject("LoseTitle");
        titleGO.transform.SetParent(contentGO.transform, false);

        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(400, 100);
        titleRect.anchoredPosition = new Vector2(0, -50);

        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = loseMessage;
        titleText.color = loseColor;
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
        statsText.text = "Танк уничтожен!";
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
        buttonsText.text = "↺ ЗАНОВО\n🏠 В МЕНЮ";
        buttonsText.color = Color.gray;
        buttonsText.fontSize = 28;
        buttonsText.alignment = TextAlignmentOptions.Center;

        // Сохраняем ссылки
        loseCanvas = canvasGO;
        loseTitleText = titleText;
        loseStatsText = statsText;

        loseCanvas.SetActive(false);

        Debug.Log("✅ LoseCanvas создан автоматически!");
    }

    #endregion

    #region Обработчики кнопок

    private void OnRestartClicked()
    {
        Debug.Log("📌 Полный рестарт уровня");
        ResetLose();
        
        if (GameLauncher.Instance != null)
        {
            GameLauncher.Instance.RestartLevel();
        }
    }

    private void OnMenuClicked()
    {
        Debug.Log("📌 Выход в меню");
        ResetLose();
        
        // Сбрасываем генераторы для новой игры
        if (obstacleGenerator != null)
        {
            obstacleGenerator.ResetForNewGame();
        }
        
        if (GameLauncher.Instance != null)
        {
            GameLauncher.Instance.ReturnToMenu();
        }
    }

    #endregion

    public void ResetLose()
    {
        isLose = false;

        if (loseCanvas != null)
            loseCanvas.SetActive(false);

        Time.timeScale = 1f;
        CancelInvoke(nameof(PauseGame));
    }
}
