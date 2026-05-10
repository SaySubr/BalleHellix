using Config;
using Data;
using UnityEngine;

namespace MainMenu.LevelMap
{
    public class Island : MonoBehaviour
    {
        [Header("Inline Level Config")]
        [SerializeField] private bool useInlineConfig = true;
        [SerializeField] private LevelData inlineConfig = new LevelData();

        [Header("Visual State")]
        [SerializeField] private bool applyProgressVisuals = true;
        [SerializeField] private bool addColliderIfMissing = true;
        [SerializeField] private float hoverBrightness = 1.15f;

        public int LevelNumber { get; private set; }
        public bool IsUnlocked { get; private set; }
        public LevelData LevelData => _levelData;

        private IslandSpawner _spawner;
        private LevelData _levelData;
        private Renderer[] _renderers;
        private Material[][] _defaultSharedMaterials;
        private bool _isHovered;
        private int _lastSelectFrame = -1;

        private void Awake()
        {
            CacheRenderers();
            EnsureCollider();
        }

        private void Start()
        {
            if (_levelData == null && useInlineConfig)
            {
                Setup(inlineConfig, null);
            }
            else
            {
                RefreshState();
            }
        }

        private void OnValidate()
        {
            if (inlineConfig != null)
            {
                inlineConfig.SyncLegacyFields();
            }
        }

        public void Setup(LevelData levelData, IslandSpawner spawner)
        {
            _levelData = levelData;
            _spawner = spawner;

            if (_levelData != null)
            {
                _levelData.SyncLegacyFields();
                LevelNumber = _levelData.levelNumber;
            }

            RefreshState();
        }

        public void Setup(int levelNumber, bool isUnlocked, IslandSpawner spawner)
        {
            inlineConfig.levelNumber = levelNumber;
            inlineConfig.isUnlocked = isUnlocked;
            Setup(inlineConfig, spawner);
        }

        public void Select()
        {
            if (_lastSelectFrame == Time.frameCount)
                return;

            _lastSelectFrame = Time.frameCount;
            RefreshState();

            if (_levelData == null)
            {
                Debug.LogWarning($"Island '{name}' has no level config.");
                return;
            }

            if (!IsUnlocked)
            {
                Debug.Log($"Level {LevelNumber} is locked.");
                return;
            }

            if (_spawner != null)
            {
                _spawner.OnIslandClicked(this);
                return;
            }

            if (GameLauncher.Instance != null)
            {
                GameLauncher.Instance.LaunchLevel(_levelData);
            }
            else
            {
                Debug.LogError("Island: GameLauncher.Instance was not found.");
            }
        }

        public void RefreshState()
        {
            LevelData data = _levelData ?? (useInlineConfig ? inlineConfig : null);
            if (data == null)
                return;

            data.SyncLegacyFields();
            LevelNumber = data.levelNumber;
            IsUnlocked = ResolveUnlockedState(data);

            if (applyProgressVisuals)
            {
                ApplyVisualState(data, IsUnlocked);
            }
        }

        private void OnMouseDown()
        {
            Select();
        }

        private void OnMouseEnter()
        {
            _isHovered = true;
            RefreshState();
        }

        private void OnMouseExit()
        {
            _isHovered = false;
            RefreshState();
        }

        private bool ResolveUnlockedState(LevelData data)
        {
            if (DataController.Instance != null)
            {
                return DataController.Instance.IsLevelUnlocked(data.levelNumber);
            }

            return data.isUnlocked;
        }

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _defaultSharedMaterials = new Material[_renderers.Length][];

            for (int i = 0; i < _renderers.Length; i++)
            {
                _defaultSharedMaterials[i] = _renderers[i].sharedMaterials;
            }
        }

        private void EnsureCollider()
        {
            if (!addColliderIfMissing)
                return;

            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }
        }

        private void ApplyVisualState(LevelData data, bool isUnlocked)
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                CacheRenderers();
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer targetRenderer = _renderers[i];
                if (targetRenderer == null)
                    continue;

                if (isUnlocked)
                {
                    ApplyUnlockedVisual(data, targetRenderer, i);
                }
                else
                {
                    ApplyLockedVisual(data, targetRenderer);
                }
            }
        }

        private void ApplyUnlockedVisual(LevelData data, Renderer targetRenderer, int rendererIndex)
        {
            if (data.islandMaterial != null)
            {
                targetRenderer.sharedMaterial = data.islandMaterial;
            }
            else if (_defaultSharedMaterials != null && rendererIndex < _defaultSharedMaterials.Length)
            {
                targetRenderer.sharedMaterials = _defaultSharedMaterials[rendererIndex];
            }

            if (_isHovered)
            {
                BrightenRenderer(targetRenderer, hoverBrightness);
            }
        }

        private void ApplyLockedVisual(LevelData data, Renderer targetRenderer)
        {
            if (data.lockedIslandMaterial != null)
            {
                targetRenderer.sharedMaterial = data.lockedIslandMaterial;
                return;
            }

            TintRenderer(targetRenderer, data.islandColor);
        }

        private void TintRenderer(Renderer targetRenderer, Color tint)
        {
            Material[] materials = targetRenderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                    continue;

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", tint);
                }
                else if (material.HasProperty("_Color"))
                {
                    material.color = tint;
                }
            }
        }

        private void BrightenRenderer(Renderer targetRenderer, float brightness)
        {
            Material[] materials = targetRenderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                    continue;

                if (material.HasProperty("_BaseColor"))
                {
                    Color color = material.GetColor("_BaseColor");
                    material.SetColor("_BaseColor", color * brightness);
                }
                else if (material.HasProperty("_Color"))
                {
                    material.color *= brightness;
                }
            }
        }
    }
}
