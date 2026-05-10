using System;
using Config;
using MainMenu;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FireballBackgroundSpawner : MonoBehaviour
{
    public static FireballBackgroundSpawner Instance { get; private set; }

    public static event Action<FireballBackgroundSpawner> InstanceReady;
    public static event Action<GameObject> BackgroundSpawnedGlobal;

    public event Action<FireballBackgroundSettings> SettingsApplied;
    public event Action<GameObject> BackgroundSpawned;
    public event Action<GameObject> BackgroundCleared;

    [Header("Spawn")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool clearBeforeSpawn = true;
    [SerializeField] private Transform targetPoint;
    [SerializeField] private Transform spawnedParent;

    [Header("Fallback For Direct Scene Test")]
    [SerializeField] private FireballBackgroundSettings fallbackSettings = new FireballBackgroundSettings
    {
        spawnBackground = false
    };

    public GameObject CurrentBackground { get; private set; }
    public FireballBackgroundSettings CurrentSettings { get; private set; }
    public Transform TargetPoint => targetPoint != null ? targetPoint : transform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InstanceReady?.Invoke(this);
    }

    private void Start()
    {
        if (spawnOnStart)
            SpawnFromCurrentLevel();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    [ContextMenu("Spawn From Current Level")]
    public void SpawnFromCurrentLevel()
    {
        FireballBackgroundSettings settings = ResolveSelectedSettings();
        Spawn(settings);
    }

    [ContextMenu("Clear Background")]
    public void ClearBackground()
    {
        if (CurrentBackground == null)
            return;

        GameObject cleared = CurrentBackground;
        CurrentBackground = null;

        if (Application.isPlaying)
            Destroy(cleared);
        else
            DestroyImmediate(cleared);

        BackgroundCleared?.Invoke(cleared);
    }

    public void Spawn(FireballBackgroundSettings settings)
    {
        CurrentSettings = settings;
        SettingsApplied?.Invoke(settings);

        if (settings == null || !settings.spawnBackground)
            return;

        if (settings.backgroundPrefab == null)
            return;

        if (clearBeforeSpawn)
            ClearBackground();

        Transform point = TargetPoint;
        Vector3 position = point.position + point.TransformDirection(settings.positionOffset);
        Quaternion rotation = point.rotation * Quaternion.Euler(settings.rotationOffset);
        Transform parent = settings.parentToSpawner ? ResolveParent() : null;

        CurrentBackground = Instantiate(settings.backgroundPrefab, position, rotation, parent);
        CurrentBackground.name = settings.backgroundPrefab.name + "_LevelBackground";

        Vector3 safeScale = settings.scale == Vector3.zero ? Vector3.one : settings.scale;
        CurrentBackground.transform.localScale = safeScale;

        ApplyMaterial(CurrentBackground, settings.backgroundMaterial);

        BackgroundSpawned?.Invoke(CurrentBackground);
        BackgroundSpawnedGlobal?.Invoke(CurrentBackground);
    }

    public void SetTargetPoint(Transform newTargetPoint)
    {
        targetPoint = newTargetPoint;
    }

    private FireballBackgroundSettings ResolveSelectedSettings()
    {
        if (GameLauncher.Instance == null)
            return fallbackSettings;

        LevelData levelData = GameLauncher.Instance.GetCurrentLevelData();
        if (levelData == null || levelData.EffectiveGameType != LevelGameType.Fireball)
            return fallbackSettings;

        return levelData.fireball.background;
    }

    private Transform ResolveParent()
    {
        return spawnedParent != null ? spawnedParent : transform;
    }

    private void ApplyMaterial(GameObject root, Material material)
    {
        if (root == null || material == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;

            renderer.sharedMaterials = materials;
        }
    }
}
