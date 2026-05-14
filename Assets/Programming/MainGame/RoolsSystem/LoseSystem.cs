using System.Collections;
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
    [SerializeField] private PlayerController playerController;

    [Header("Тексты")]
    [SerializeField] private TextMeshProUGUI loseTitleText; // Заголовок "ПОРАЖЕНИЕ!"
    [SerializeField] private TextMeshProUGUI loseStatsText; // Статистика

    [Header("Настройки поражения")]
    [SerializeField] private string loseMessage = "ПОРАЖЕНИЕ!";
    [SerializeField] private Color loseColor = Color.red;
    [SerializeField] private float pauseDelay = 1.5f;

    [Header("Кнопки (опционально)")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    [Header("Rewarded Revive")]
    [SerializeField] private Button rewardedReviveButton;
    [SerializeField] private TextMeshProUGUI rewardedReviveCountdownText;
    [SerializeField] private Image rewardedReviveCountdownImage;
    [SerializeField] private float rewardedReviveOfferSeconds = 5f;
    [SerializeField] private bool hideStandardButtonsDuringRewardOffer = true;

    [Header("Rewarded Revive View")]
    [SerializeField] private string rewardedReviveCountdownFormat = "{0}";
    [SerializeField, Range(0f, 1f)] private float rewardedReviveImageStartFill = 1f;
    [SerializeField, Range(0f, 1f)] private float rewardedReviveImageEndFill = 0f;

    private bool isLose = false;
    private bool rewardedReviveUsed;
    private Coroutine rewardedReviveRoutine;
    private Coroutine pauseRoutine;

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

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        FireballRoundState.Reset();

        // Подписываемся на кнопки
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuClicked);

        if (rewardedReviveButton != null)
            rewardedReviveButton.onClick.AddListener(OnRewardedReviveClicked);
    }

    private void OnDestroy()
    {
        // Отписываемся от кнопок
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);

        if (menuButton != null)
            menuButton.onClick.RemoveListener(OnMenuClicked);

        if (rewardedReviveButton != null)
            rewardedReviveButton.onClick.RemoveListener(OnRewardedReviveClicked);
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
        if (!FireballRoundState.TryFinish()) return;

        isLose = true;
        SetPlayerControls(false);

        Debug.Log($"ПОРАЖЕНИЕ! Танк уничтожен!");

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
        StartRewardedReviveOffer();
        StartPauseRoutine();
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

    private void StartRewardedReviveOffer()
    {
        StopRewardedReviveRoutine();

        if (rewardedReviveUsed || rewardedReviveButton == null)
        {
            ShowStandardButtons(true);
            return;
        }

        ShowStandardButtons(!hideStandardButtonsDuringRewardOffer);
        ShowRewardedReviveButton(true);
        ShowRewardedReviveCountdown(true);
        rewardedReviveRoutine = StartCoroutine(RewardedReviveCountdownRoutine());
    }

    private IEnumerator RewardedReviveCountdownRoutine()
    {
        float remainingSeconds = Mathf.Max(0f, rewardedReviveOfferSeconds);

        while (remainingSeconds > 0f && isLose)
        {
            UpdateRewardedReviveCountdown(remainingSeconds);
            remainingSeconds -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (isLose)
            ShowStandardButtons(true);

        ShowRewardedReviveButton(false);
        ShowRewardedReviveCountdown(false);
        rewardedReviveRoutine = null;
    }

    private void OnRewardedReviveClicked()
    {
        if (!isLose || rewardedReviveUsed)
            return;

        StopRewardedReviveRoutine();
        ShowRewardedReviveButton(false);
        ShowRewardedReviveCountdown(false);
        StopPauseRoutine();
        Time.timeScale = 0f;

        YandexAdsService.EnsureInstance().ShowRewarded(
            ReviveAfterReward,
            ShowStandardButtonsAfterFailedReward);
    }

    private void ReviveAfterReward()
    {
        rewardedReviveUsed = true;
        ResetLose();
        FireballRoundState.Reset();

        if (tankHealth != null)
            tankHealth.ResetHealth();

        SetPlayerControls(true);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ShowStandardButtonsAfterFailedReward()
    {
        if (!isLose)
            return;

        ShowStandardButtons(true);
    }

    private void ShowStandardButtons(bool isVisible)
    {
        if (restartButton != null)
            restartButton.gameObject.SetActive(isVisible);

        if (menuButton != null)
            menuButton.gameObject.SetActive(isVisible);
    }

    private void ShowRewardedReviveButton(bool isVisible)
    {
        if (rewardedReviveButton == null)
            return;

        rewardedReviveButton.gameObject.SetActive(isVisible);
    }

    private void ShowRewardedReviveCountdown(bool isVisible)
    {
        if (rewardedReviveCountdownText != null)
            rewardedReviveCountdownText.gameObject.SetActive(isVisible);

        if (rewardedReviveCountdownImage != null)
        {
            rewardedReviveCountdownImage.gameObject.SetActive(isVisible);
            if (isVisible)
                rewardedReviveCountdownImage.fillAmount = rewardedReviveImageStartFill;
        }
    }

    private void UpdateRewardedReviveCountdown(float seconds)
    {
        float duration = Mathf.Max(0.01f, rewardedReviveOfferSeconds);
        float normalizedTimeLeft = Mathf.Clamp01(seconds / duration);

        if (rewardedReviveCountdownText != null)
            rewardedReviveCountdownText.text = FormatRewardedCountdown(seconds);

        if (rewardedReviveCountdownImage != null)
            rewardedReviveCountdownImage.fillAmount = Mathf.Lerp(
                rewardedReviveImageEndFill,
                rewardedReviveImageStartFill,
                normalizedTimeLeft);
    }

    private string FormatRewardedCountdown(float seconds)
    {
        int secondsCeil = Mathf.CeilToInt(seconds);
        if (string.IsNullOrWhiteSpace(rewardedReviveCountdownFormat))
            return secondsCeil.ToString();

        return rewardedReviveCountdownFormat
            .Replace("{seconds}", secondsCeil.ToString())
            .Replace("{0}", secondsCeil.ToString());
    }

    private void StopRewardedReviveRoutine()
    {
        if (rewardedReviveRoutine == null)
            return;

        StopCoroutine(rewardedReviveRoutine);
        rewardedReviveRoutine = null;
    }

    public void ResetLose()
    {
        isLose = false;
        StopRewardedReviveRoutine();
        StopPauseRoutine();
        ShowRewardedReviveButton(false);
        ShowRewardedReviveCountdown(false);

        if (loseCanvas != null)
            loseCanvas.SetActive(false);

        Time.timeScale = 1f;
        CancelInvoke(nameof(PauseGame));
    }
}
