using System.Collections.Generic;
using Config;
using MainGame;
using MainMenu;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Генератор препятствий, движущихся по сплайну.
/// Использует пул объектов и распределяет препятствия между всеми сплайнами в сцене.
/// </summary>
public class ObstacleGenerator : MonoBehaviour
{
    [Header("Префаб препятствия")]
    [SerializeField] private GameObject obstaclePrefab;

    [Header("Настройки сплайна")]
    [SerializeField] private bool findSplinesAutomatically = true;
    [SerializeField] private SplineContainer[] splineContainers;

    [Header("Количество препятствий")]
    [SerializeField] private Vector2Int obstacleCountRange = new Vector2Int(5, 20);

    [Header("Расстановка на сплайне")]
    [SerializeField] private bool randomSpacing = true;
    [SerializeField] private Vector2 spacingRange = new Vector2(1f, 5f);

    [Header("Вращение (общие настройки)")]
    [SerializeField] private bool randomizeRotation = true;
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(30f, 180f);

    [Header("Скорость движения (общие настройки)")]
    [SerializeField] private bool randomizeSpeed = true;
    [SerializeField] private Vector2 speedRange = new Vector2(3f, 10f);

    [Header("Масштаб")]
    [SerializeField] private bool randomizeScale = true;
    [SerializeField] private Vector2 scaleRange = new Vector2(0.5f, 1.5f);
    [SerializeField] private bool uniformScale = true;

    [Header("Настройки отражения")]
    [Tooltip("Скорость отражённой пули (фиксированная, чтобы долетала до танка)")]
    [SerializeField] private float deflectSpeed = 20f;
    [Tooltip("Высота параболы при отражении")]
    [SerializeField] private float parabolaHeight = 1f;

    [Header("Пул объектов")]
    [SerializeField] private bool useObjectPool = true;
    [SerializeField] private int poolInitialSize = 30;
    [SerializeField] private int poolMaxSize = 100;

    [Header("Сид для рестарта")]
    [Tooltip("Сид для генерации препятствий (сохраняется для рестарта)")]
    [SerializeField] private int obstacleSeed = 0;

    private List<Obstacle> activeObstacles = new List<Obstacle>();
    private List<SplineContainer> availableSplines = new List<SplineContainer>();
    private Queue<GameObject> obstaclePool = new Queue<GameObject>();
    private Transform poolParent;

    // События
    public System.Action<int> OnObstaclesGenerated;
    public System.Action OnAllObstaclesReady;

    private void Start()
    {
        ApplySelectedLevelSettings();
        InitializeSplines();
        InitializePool();
        GenerateObstacles();
    }

    private void ApplySelectedLevelSettings()
    {
        if (GameLauncher.Instance == null)
            return;

        LevelData levelData = GameLauncher.Instance.GetCurrentLevelData();
        if (levelData == null || levelData.EffectiveGameType != LevelGameType.Fireball)
            return;

        ApplySettings(levelData.fireball);
    }

    public void ApplySettings(FireballLevelSettings settings)
    {
        if (settings == null || !settings.overrideObstacles)
            return;

        if (settings.obstaclePrefab != null)
            obstaclePrefab = settings.obstaclePrefab;

        obstacleCountRange = NormalizeRange(settings.obstacleCountRange, 0);
        randomSpacing = settings.randomSpacing;
        spacingRange = NormalizeRange(settings.spacingRange, 0f);
        randomizeRotation = settings.randomizeObstacleRotation;
        rotationSpeedRange = NormalizeRange(settings.obstacleRotationSpeedRange, 0f);
        randomizeSpeed = settings.randomizeObstacleSpeed;
        speedRange = NormalizeRange(settings.obstacleSpeedRange, 0f);
        randomizeScale = settings.randomizeObstacleScale;
        scaleRange = NormalizeRange(settings.obstacleScaleRange, 0.01f);
        uniformScale = settings.uniformObstacleScale;
        parabolaHeight = Mathf.Max(0f, settings.obstacleParabolaHeight);
        useObjectPool = settings.useObjectPool;
        poolMaxSize = Mathf.Max(1, settings.poolMaxSize);
        poolInitialSize = Mathf.Clamp(settings.poolInitialSize, 0, poolMaxSize);
    }

    private Vector2 NormalizeRange(Vector2 range, float minimum)
    {
        float min = Mathf.Max(minimum, Mathf.Min(range.x, range.y));
        float max = Mathf.Max(min, Mathf.Max(range.x, range.y));
        return new Vector2(min, max);
    }

    private Vector2Int NormalizeRange(Vector2Int range, int minimum)
    {
        int min = Mathf.Max(minimum, Mathf.Min(range.x, range.y));
        int max = Mathf.Max(min, Mathf.Max(range.x, range.y));
        return new Vector2Int(min, max);
    }

    private void InitializeSplines()
    {
        availableSplines.Clear();

        if (findSplinesAutomatically)
        {
            // Находим все сплайны в сцене
            SplineContainer[] allSplines = FindObjectsOfType<SplineContainer>();
            foreach (var spline in allSplines)
            {
                if (spline != null && spline.Spline.Count >= 2)
                {
                    availableSplines.Add(spline);
                }
            }
            Debug.Log($"🔍 Найдено сплайнов: {availableSplines.Count}");
        }
        else if (splineContainers != null)
        {
            foreach (var spline in splineContainers)
            {
                if (spline != null && spline.Spline.Count >= 2)
                {
                    availableSplines.Add(spline);
                }
            }
        }

        if (availableSplines.Count == 0)
        {
            Debug.LogError("❌ Не найдено ни одного valid сплайна!");
        }
    }

    private void InitializePool()
    {
        if (!useObjectPool) return;

        // Создаем родительский объект для пула
        GameObject poolObj = new GameObject("ObstaclePool");
        poolObj.transform.SetParent(transform);
        poolParent = poolObj.transform;

        // Предзаполняем пул
        for (int i = 0; i < poolInitialSize; i++)
        {
            GameObject obj = CreatePooledObstacle();
            obstaclePool.Enqueue(obj);
        }

        Debug.Log($"📦 Пул объектов создан: {poolInitialSize} объектов");
    }

    private GameObject CreatePooledObstacle()
    {
        GameObject obj = Instantiate(obstaclePrefab, poolParent);
        obj.SetActive(false);
        return obj;
    }

    private GameObject GetFromPool()
    {
        if (obstaclePool.Count > 0)
        {
            GameObject obj = obstaclePool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        // Если пул пуст и можем расширять
        if (activeObstacles.Count < poolMaxSize)
        {
            return CreatePooledObstacle();
        }

        Debug.LogWarning("⚠️ Пул объектов пуст и достигнут максимум!");
        return null;
    }

    private void ReturnToPool(GameObject obj)
    {
        if (!useObjectPool)
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        obstaclePool.Enqueue(obj);
    }

    [ContextMenu("Сгенерировать новые препятствия")]
    public void GenerateObstacles()
    {
        ClearObstacles();

        if (obstaclePrefab == null)
        {
            Debug.LogError("Obstacle Prefab не назначен!");
            return;
        }

        if (availableSplines.Count == 0)
        {
            Debug.LogError("SplineContainer не найден!");
            return;
        }

        // Проверяем SessionManager (рестарт или новая игра)
        if (SessionManager.Instance != null && !SessionManager.Instance.IsFirstLaunch)
        {
            // Рестарт — используем сохранённые данные
            int count = SessionManager.Instance.obstacleCount;
            obstacleSeed = SessionManager.Instance.obstacleSeed;
            Debug.Log($"🔄 Рестарт: препятствий={count}, seed={obstacleSeed}");
            GenerateObstaclesWithCount(count);
        }
        else
        {
            // Новая игра — генерируем
            int count = Random.Range(obstacleCountRange.x, obstacleCountRange.y + 1);
            obstacleSeed = Random.Range(0, 10000);
            
            // Сохраняем в SessionManager
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.SaveObstacleData(count, obstacleSeed);
            }
            
            Debug.Log($"🎲 Новая генерация: {count} препятствий, seed={obstacleSeed}");
            GenerateObstaclesWithCount(count);
        }
    }

    /// <summary>
    /// Генерация препятствий с конкретным количеством
    /// </summary>
    private void GenerateObstaclesWithCount(int count)
    {
        // Устанавливаем seed для рандома
        Random.InitState(obstacleSeed);

        Debug.Log($"🎯 Генерация {count} препятствий на {availableSplines.Count} сплайнах");

        // Распределяем препятствия равномерно по сплайнам
        int obstaclesPerSpline = count / availableSplines.Count;
        int extraObstacles = count % availableSplines.Count;

        int obstacleIndex = 0;

        for (int s = 0; s < availableSplines.Count; s++)
        {
            SplineContainer spline = availableSplines[s];
            int splineObstacleCount = obstaclesPerSpline + (s < extraObstacles ? 1 : 0);

            for (int i = 0; i < splineObstacleCount; i++)
            {
                CreateObstacle(spline, obstacleIndex, count);
                obstacleIndex++;
            }
        }

        OnObstaclesGenerated?.Invoke(count);

        // Небольшая задержка перед сигналом готовности
        Invoke(nameof(InvokeReady), 0.1f);
    }

    private void InvokeReady()
    {
        OnAllObstaclesReady?.Invoke();
    }

    private void CreateObstacle(SplineContainer spline, int index, int total)
    {
        GameObject obstacleObj;

        if (useObjectPool)
        {
            obstacleObj = GetFromPool();
            if (obstacleObj == null) return;
        }
        else
        {
            obstacleObj = Instantiate(obstaclePrefab, transform);
        }

        obstacleObj.name = $"Obstacle_{index}";
        obstacleObj.tag = "Obstacle";

        // Вычисляем позицию на сплайне
        float t = (float)index / total;
        Vector3 position = spline.EvaluatePosition(t);
        Vector3 tangent = spline.EvaluateTangent(t);

        obstacleObj.transform.position = position;
        if (tangent != Vector3.zero)
        {
            obstacleObj.transform.rotation = Quaternion.LookRotation(tangent);
        }

        // Настраиваем компоненты
        SetupObstacle(obstacleObj, spline, index, total);

        Obstacle obstacleComponent = obstacleObj.GetComponent<Obstacle>();
        if (obstacleComponent != null)
        {
            activeObstacles.Add(obstacleComponent);
        }
    }

    private void SetupObstacle(GameObject obstacle, SplineContainer spline, int index, int total)
    {
        // === ObstacleMovement ===
        ObstacleMovement movement = obstacle.GetComponent<ObstacleMovement>();
        if (movement == null)
            movement = obstacle.AddComponent<ObstacleMovement>();

        movement.SetSpline(spline);
        if (randomizeSpeed)
        {
            movement.SetSpeedRange(speedRange);
        }
        movement.Initialize();

        // === ObstacleRotation ===
        ObstacleRotation rotation = obstacle.GetComponent<ObstacleRotation>();
        if (rotation == null)
            rotation = obstacle.AddComponent<ObstacleRotation>();

        if (randomizeRotation)
        {
            rotation.SetRotationSpeedRange(rotationSpeedRange);
            rotation.RandomizeAll();
        }
        rotation.Initialize();

        // === ObstacleColor ===
        ObstacleColor color = obstacle.GetComponent<ObstacleColor>();
        if (color == null)
            color = obstacle.AddComponent<ObstacleColor>();

        // Применяем случайный цвет сразу (как в TowerGenerator)
        color.ApplyRandomColor();

        // === Obstacle (отражение пуль) ===
        Obstacle obstacleHealth = obstacle.GetComponent<Obstacle>();
        if (obstacleHealth == null)
            obstacleHealth = obstacle.AddComponent<Obstacle>();

        obstacleHealth.SetParabolaHeight(parabolaHeight);
        obstacleHealth.Initialize();

        // === Масштаб ===
        if (randomizeScale)
        {
            if (uniformScale)
            {
                float scale = Random.Range(scaleRange.x, scaleRange.y);
                obstacle.transform.localScale = Vector3.one * scale;
            }
            else
            {
                float scaleX = Random.Range(scaleRange.x, scaleRange.y);
                float scaleY = Random.Range(scaleRange.x, scaleRange.y);
                float scaleZ = Random.Range(scaleRange.x, scaleRange.y);
                obstacle.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            }
        }
    }

    public void ClearObstacles()
    {
        foreach (var obstacle in activeObstacles)
        {
            if (obstacle != null)
            {
                ReturnToPool(obstacle.gameObject);
            }
        }

        activeObstacles.Clear();
    }

    /// <summary>
    /// Полный сброс для новой игры (сбрасывает SessionManager)
    /// </summary>
    public void ResetForNewGame()
    {
        ClearObstacles();
        
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.ResetForNewGame();
        }
        
        Debug.Log("🔄 Препятствия сброшены для новой игры");
    }

    // Публичные методы
    public void AddObstacle()
    {
        if (obstaclePrefab == null || availableSplines.Count == 0)
            return;

        SplineContainer spline = availableSplines[Random.Range(0, availableSplines.Count)];
        CreateObstacle(spline, activeObstacles.Count, activeObstacles.Count + 1);
    }

    public int GetRemainingObstacles() => activeObstacles.Count;

    /// <summary>
    /// Установить ссылку на танк для всех препятствий
    /// </summary>
    public void SetTankTarget(Transform tank)
    {
        foreach (var obstacle in activeObstacles)
        {
            if (obstacle != null)
            {
                obstacle.SetTankTarget(tank);
            }
        }
    }

    // Для отладки в редакторе
    private void OnDrawGizmos()
    {
        if (availableSplines.Count == 0)
            return;

        Gizmos.color = Color.yellow;

        foreach (var spline in availableSplines)
        {
            if (spline == null) continue;

            // Рисуем сплайн
            for (int i = 0; i < spline.Spline.Count; i++)
            {
                Vector3 pos = spline.transform.TransformPoint(spline.Spline[i].Position);
                Gizmos.DrawWireSphere(pos, 0.3f);
            }
        }

        if (Application.isPlaying)
        {
            foreach (var obstacle in activeObstacles)
            {
                if (obstacle != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(obstacle.transform.position, 0.5f);
                }
            }
        }
    }
}
