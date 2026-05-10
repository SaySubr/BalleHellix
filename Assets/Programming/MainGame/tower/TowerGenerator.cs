using System.Collections.Generic;
using System.Linq;
using Config;
using MainGame;
using MainMenu;
using UnityEngine;

/// <summary>
/// Генератор башни с оптимизацией через пул объектов.
/// Создаётся N блоков (например 70), отображаются только видимые (например 10).
/// </summary>
public class TowerGenerator : MonoBehaviour
{
    [Header("Префаб")]
    [Tooltip("Префаб блока башни")]
    [SerializeField] private GameObject blockPrefab;

    [Header("Основные настройки")]
    [SerializeField] private bool hideMainCube = true;

    [Header("Размеры башни")]
    [Tooltip("Минимальная высота башни")]
    [SerializeField] private int minHeight = 50;
    [Tooltip("Максимальная высота башни")]
    [SerializeField] private int maxHeight = 70;
    [SerializeField] private float blockHeight = 0.5f;

    [Header("Оптимизация (Пул)")]
    [Tooltip("Скрывать блоки за пределами видимости")]
    [SerializeField] private bool useVisibilityCulling = true;
    [Tooltip("Камера для проверки видимости")]
    [SerializeField] private Camera optimizationCamera;
    [Tooltip("Сколько блоков отображать сверху (видимая зона)")]
    [SerializeField] private int visibleBlocksCount = 10;

    [Header("Цвет")]
    [SerializeField] private Gradient towerGradient;
    [SerializeField] private bool randomColorPerBlock = true;

    [Header("Вращение")]
    [SerializeField] private bool randomizeRotation = true;
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(30f, 180f);
    [SerializeField] private bool rotateBlocks = true;
    [SerializeField] private Vector3 blockStartRotation = Vector3.zero;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private bool randomizeRotationDirection = true;
    [SerializeField] private bool invertRotation = false;
    [SerializeField] private bool pulsateBlocks = false;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;

    [Header("Эффекты")]
    [SerializeField] private ParticleSystem blockDestroyEffect;

    [Header("Масштаб")]
    [SerializeField] private bool randomizeScale = true;
    [SerializeField] private Vector2 scaleRange = new Vector2(0.5f, 1.5f);
    [SerializeField] private bool uniformScale = true;

    [Header("Счетчик")]
    [SerializeField] private int currentScore = 0;
    [SerializeField] private int blocksDestroyed = 0;

    [Header("Сид для рестарта")]
    [Tooltip("Сид для генерации башни (сохраняется для рестарта)")]
    [SerializeField] private int towerSeed = 0;

    // События
    public System.Action<int> OnScoreChanged;
    public System.Action<int> OnBlockDestroyed;
    public System.Action<int> OnTowerGenerated;
    public System.Action OnTowerDestroyed;

    private List<TowerBlock> allBlocks = new List<TowerBlock>(); // Все блоки (пул)
    private List<TowerBlock> activeBlocks = new List<TowerBlock>(); // Активные (не уничтоженные)
    private TowerBlockStack blockStack;
    private int currentHeight;

    private void Awake()
    {
        EnsureBlockStack();
    }

    private void Start()
    {
        ApplySelectedLevelSettings();

        if (hideMainCube)
            HideMainCube();

        // Находим камеру если не назначена
        if (optimizationCamera == null)
        {
            optimizationCamera = Camera.main;
        }

        GenerateTower();
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
        if (settings == null || !settings.overrideTower)
            return;

        if (settings.towerBlockPrefab != null)
            blockPrefab = settings.towerBlockPrefab;

        minHeight = Mathf.Max(1, Mathf.Min(settings.towerHeightRange.x, settings.towerHeightRange.y));
        maxHeight = Mathf.Max(minHeight, Mathf.Max(settings.towerHeightRange.x, settings.towerHeightRange.y));
        blockHeight = Mathf.Max(0.05f, settings.blockHeight);
        useVisibilityCulling = settings.useVisibilityCulling;
        visibleBlocksCount = Mathf.Max(1, settings.visibleBlocksCount);
        randomizeRotation = settings.randomizeTowerRotation;
        rotationSpeedRange = NormalizeRange(settings.towerRotationSpeedRange, 0f);
        rotateBlocks = settings.rotateTowerBlocks;
        blockStartRotation = settings.towerBlockStartRotation;
        rotationAxis = settings.towerRotationAxis == Vector3.zero ? Vector3.up : settings.towerRotationAxis.normalized;
        randomizeRotationDirection = settings.randomizeTowerRotationDirection;
        invertRotation = settings.invertTowerRotation;
        pulsateBlocks = settings.towerBlockPulsate;
        pulseSpeed = Mathf.Max(0f, settings.towerPulseSpeed);
        pulseAmount = Mathf.Max(0f, settings.towerPulseAmount);
        blockDestroyEffect = settings.towerBlockDestroyEffect;
        randomizeScale = settings.randomizeTowerScale;
        scaleRange = NormalizeRange(settings.towerScaleRange, 0.01f);
        uniformScale = settings.uniformTowerScale;
    }

    private Vector2 NormalizeRange(Vector2 range, float minimum)
    {
        float min = Mathf.Max(minimum, Mathf.Min(range.x, range.y));
        float max = Mathf.Max(min, Mathf.Max(range.x, range.y));
        return new Vector2(min, max);
    }

    private void HideMainCube()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    [ContextMenu("Сгенерировать новую башню")]
    public void GenerateTower()
    {
        ClearTower();
        EnsureBlockStack().Clear();

        if (blockPrefab == null)
        {
            Debug.LogError("Block Prefab не назначен!");
            return;
        }

        // Проверяем SessionManager (рестарт или новая игра)
        if (SessionManager.Instance != null && !SessionManager.Instance.IsFirstLaunch)
        {
            // Рестарт — используем сохранённые данные
            currentHeight = SessionManager.Instance.towerHeight;
            towerSeed = SessionManager.Instance.towerSeed;
            Debug.Log($"🔄 Рестарт: высота={currentHeight}, seed={towerSeed}");
        }
        else
        {
            // Новая игра — генерируем
            currentHeight = Random.Range(minHeight, maxHeight + 1);
            towerSeed = Random.Range(0, 10000);
            
            // Сохраняем в SessionManager
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.SaveTowerData(currentHeight, towerSeed);
            }
            
            Debug.Log($"🎲 Новая генерация: высота={currentHeight}, seed={towerSeed}");
        }

        // Устанавливаем seed для рандома
        Random.InitState(towerSeed);

        // Сброс счета
        currentScore = 0;
        blocksDestroyed = 0;
        OnScoreChanged?.Invoke(currentScore);

        Debug.Log($"Генерация башни: {currentHeight} блоков (видимых: {visibleBlocksCount})");

        // Создаём ВСЕ блоки сразу (пул)
        for (int i = 0; i < currentHeight; i++)
        {
            Vector3 localSlotPosition = EnsureBlockStack().RegisterSlot(i, blockHeight);
            Vector3 position = transform.TransformPoint(localSlotPosition);
            TowerBlock towerBlock = CreateBlock(position, i);

            if (towerBlock != null)
            {
                EnsureBlockStack().RegisterBlock(towerBlock.gameObject, i, blockHeight);
                towerBlock.OnBlockDestroyed += OnBlockDestroyedHandler;
                allBlocks.Add(towerBlock);
                activeBlocks.Add(towerBlock);
            }
        }

        // Обновляем видимость блоков
        UpdateBlocksVisibility();

        OnTowerGenerated?.Invoke(currentHeight);
        Debug.Log($"Создана башня высотой {currentHeight} блоков. В пуле: {allBlocks.Count}");
    }

    private TowerBlock CreateBlock(Vector3 position, int index)
    {
        GameObject block = Instantiate(blockPrefab, position, Quaternion.identity, transform);
        block.name = $"TowerBlock_{index}";
        block.tag = "TowerBlock";

        TowerBlock towerBlock = block.GetComponent<TowerBlock>();
        if (towerBlock == null)
            towerBlock = block.AddComponent<TowerBlock>();

        if (blockDestroyEffect != null)
            towerBlock.SetDestroyEffect(blockDestroyEffect);

        SetupBlock(block, towerBlock, index);

        // Настройка коллайдера
        Collider col = block.GetComponent<Collider>();
        if (col == null)
            col = block.AddComponent<BoxCollider>();
        col.isTrigger = true;

        // Настройка Rigidbody
        Rigidbody rb = block.GetComponent<Rigidbody>();
        if (rb == null)
            rb = block.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        return towerBlock;
    }

    private void SetupBlock(GameObject block, TowerBlock towerBlock, int index)
    {
        // Настройка цвета
        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (randomColorPerBlock)
            {
                renderer.material.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
            }
            else if (towerGradient != null)
            {
                float t = (float)index / Mathf.Max(1, currentHeight - 1);
                renderer.material.color = towerGradient.Evaluate(t);
            }
        }

        // Настройка вращения
        TowerRotation rotation = block.GetComponent<TowerRotation>();
        if (rotation == null)
            rotation = block.AddComponent<TowerRotation>();

        rotation.SetRotateAlways(rotateBlocks);
        rotation.SetRotationAxis(rotationAxis == Vector3.zero ? Vector3.up : rotationAxis);
        rotation.EnablePulsation(pulsateBlocks, pulseSpeed, pulseAmount);

        if (randomizeRotation)
        {
            rotation.SetStartRotation(new Vector3(
                Random.Range(0f, 360f),
                Random.Range(0f, 360f),
                Random.Range(0f, 360f)
            ));

            float speed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);
            rotation.SetRotationSpeed(speed);
            rotation.SetInverted(randomizeRotationDirection ? Random.value > 0.5f : invertRotation);
        }
        else
        {
            rotation.SetStartRotation(blockStartRotation);
            rotation.SetRotationSpeed(rotationSpeedRange.x);
            rotation.SetInverted(invertRotation);
        }

        // Настройка масштаба
        if (randomizeScale)
        {
            if (uniformScale)
            {
                float scale = Random.Range(scaleRange.x, scaleRange.y);
                block.transform.localScale = Vector3.one * scale;
            }
            else
            {
                float scaleX = Random.Range(scaleRange.x, scaleRange.y);
                float scaleY = Random.Range(scaleRange.x, scaleRange.y);
                float scaleZ = Random.Range(scaleRange.x, scaleRange.y);
                block.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            }
        }
    }

    /// <summary>
    /// Обновить видимость блоков: показываем только нижние N блоков.
    /// </summary>
    private void UpdateBlocksVisibility()
    {
        if (!useVisibilityCulling) return;

        // Сортируем активные блоки по Y (от низшего к высшему)
        var sortedBlocks = activeBlocks
            .Where(b => b != null && !b.IsDestroyed())
            .OrderBy(GetBlockSortIndex)
            .ToList();

        // Показываем только нижние visibleBlocksCount блоков
        for (int i = 0; i < sortedBlocks.Count; i++)
        {
            TowerBlock block = sortedBlocks[i];
            if (block == null) continue;

            if (i < visibleBlocksCount)
            {
                // Блок в видимой зоне (снизу) - показываем
                block.gameObject.SetActive(true);
            }
            else
            {
                // Блок вне видимой зоны (выше) - скрываем (возвращаем в "пул")
                block.gameObject.SetActive(false);
            }
        }

        Debug.Log($"Отображается блоков: {Mathf.Min(visibleBlocksCount, sortedBlocks.Count)} из {activeBlocks.Count}");
    }

    private void OnBlockDestroyedHandler(GameObject block)
    {
        blocksDestroyed++;
        currentScore += 10;

        OnScoreChanged?.Invoke(currentScore);
        OnBlockDestroyed?.Invoke(blocksDestroyed);

        // Удаляем из активных
        activeBlocks.Remove(block.GetComponent<TowerBlock>());

        // Сдвигаем блоки вниз
        ShiftBlocksDown();

        // Обновляем видимость (теперь другие блоки станут видимыми)
        UpdateBlocksVisibility();

        CheckTowerDestroyed();
    }

    private void ShiftBlocksDown()
    {
        EnsureBlockStack().Compact(activeBlocks);
    }

    private void CheckTowerDestroyed()
    {
        if (activeBlocks.Count == 0)
        {
            OnTowerDestroyed?.Invoke();
            Debug.Log($"Башня уничтожена! Счет: {currentScore}");
        }
    }

    public void ClearTower()
    {
        foreach (var block in allBlocks)
        {
            if (block != null)
            {
                block.OnBlockDestroyed -= OnBlockDestroyedHandler;

                if (Application.isPlaying)
                    Destroy(block.gameObject);
                else
                    DestroyImmediate(block.gameObject);
            }
        }

        allBlocks.Clear();
        activeBlocks.Clear();
        if (blockStack != null)
            blockStack.Clear();

        currentHeight = 0;
        currentScore = 0;
        blocksDestroyed = 0;
    }

    /// <summary>
    /// Полный сброс для новой игры (сбрасывает SessionManager)
    /// </summary>
    public void ResetForNewGame()
    {
        ClearTower();
        
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.ResetForNewGame();
        }
        
        Debug.Log("🔄 Башня сброшена для новой игры");
    }

    public void DestroyBottomBlock()
    {
        if (activeBlocks.Count == 0) return;

        var bottom = activeBlocks
            .Where(b => b != null && !b.IsDestroyed())
            .OrderBy(GetBlockSortIndex)
            .FirstOrDefault();

        bottom?.TakeDamage(1);
    }

    public int GetCurrentScore() => currentScore;
    
    public int GetRemainingBlocks()
    {
        int count = 0;
        foreach (var block in activeBlocks)
        {
            if (block != null && !block.IsDestroyed())
            {
                count++;
            }
        }
        return count;
    }

    public int GetTotalBlocks() => allBlocks.Count;

    private TowerBlockStack EnsureBlockStack()
    {
        if (blockStack == null)
            blockStack = new TowerBlockStack(transform);

        return blockStack;
    }

    private int GetBlockSortIndex(TowerBlock block)
    {
        TowerBlockSlot slot = block != null ? block.GetComponent<TowerBlockSlot>() : null;
        if (slot != null)
            return slot.CurrentSlotIndex;

        return block != null ? Mathf.RoundToInt((block.transform.position.y - transform.position.y) / blockHeight) : int.MaxValue;
    }
}
