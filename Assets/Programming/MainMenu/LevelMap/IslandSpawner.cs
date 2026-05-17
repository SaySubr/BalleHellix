using Config;
using Data;
using UnityEngine;

namespace MainMenu.LevelMap
{
    public class IslandSpawner : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private LevelMapConfig levelMapConfig;

        [Header("Spawn")]
        [SerializeField] private Transform islandsParent;

        private void Start()
        {
            if (levelMapConfig == null)
            {
                Debug.LogError("IslandSpawner: LevelMapConfig is not assigned.");
                enabled = false;
                return;
            }

            levelMapConfig.SyncLegacyLevelTypes();

            if (islandsParent == null)
            {
                GameObject parentObj = new GameObject("IslandsParent");
                parentObj.transform.SetParent(transform);
                islandsParent = parentObj.transform;
            }

            SpawnAllIslands();
        }

        public void SpawnAllIslands()
        {
            if (levelMapConfig.levels.Count == 0)
            {
                Debug.LogError("IslandSpawner: LevelMapConfig has no levels.");
                return;
            }

            foreach (Transform child in islandsParent)
            {
                if (child.GetComponent<Island>() != null)
                {
                    Destroy(child.gameObject);
                }
            }

            foreach (LevelData levelData in levelMapConfig.levels)
            {
                SpawnIsland(levelData);
            }
        }

        private void SpawnIsland(LevelData levelData)
        {
            if (levelData == null)
                return;

            if (levelData.islandPrefab == null)
            {
                Debug.LogError($"Level {levelData.levelNumber}: islandPrefab is not assigned.");
                return;
            }

            IslandPlacement placement = ResolvePlacement(levelData);
            GameObject island = Instantiate(levelData.islandPrefab, placement.Position, placement.Rotation, islandsParent);
            island.name = $"Island_{levelData.levelNumber}";

            if (placement.OverrideScale)
            {
                ApplyWorldScale(island.transform, placement.WorldScale);
            }

            Island islandComponent = island.GetComponent<Island>();
            if (islandComponent == null)
            {
                islandComponent = island.AddComponent<Island>();
            }

            islandComponent.Setup(levelData, this);
        }

        public void OnIslandClicked(Island island)
        {
            if (island == null)
                return;

            OnIslandClicked(island.LevelData);
        }

        public void OnIslandClicked(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogWarning("IslandSpawner: clicked island has no level data.");
                return;
            }

            bool isUnlocked = ResolveUnlockedState(levelData);
            if (!isUnlocked)
            {
                Debug.Log($"Level {levelData.levelNumber} is locked.");
                return;
            }

            if (GameLauncher.Instance != null)
            {
                GameLauncher.Instance.LaunchLevel(levelData);
            }
            else
            {
                Debug.LogError("IslandSpawner: GameLauncher.Instance was not found.");
            }
        }

        public void OnIslandClicked(int levelNumber, bool isUnlocked)
        {
            if (!isUnlocked)
            {
                Debug.Log($"Level {levelNumber} is locked.");
                return;
            }

            LevelData levelData = levelMapConfig != null ? levelMapConfig.GetLevelData(levelNumber) : null;
            if (levelData != null)
            {
                OnIslandClicked(levelData);
                return;
            }

            if (GameLauncher.Instance != null)
            {
                GameLauncher.Instance.LaunchLevel(levelNumber);
            }
            else
            {
                Debug.LogError("IslandSpawner: GameLauncher.Instance was not found.");
            }
        }

        private bool ResolveUnlockedState(LevelData levelData)
        {
            if (DataController.Instance != null)
            {
                return DataController.Instance.IsLevelUnlocked(levelData.levelNumber);
            }

            return levelData.isUnlocked;
        }

        private IslandPlacement ResolvePlacement(LevelData levelData)
        {
            Transform targetPoint = FindTargetPoint(levelData, out bool useTargetScale);
            if (targetPoint != null)
            {
                return new IslandPlacement(
                    targetPoint.position,
                    targetPoint.rotation,
                    useTargetScale,
                    targetPoint.lossyScale);
            }

            return new IslandPlacement(
                levelData.position,
                Quaternion.Euler(levelData.islandRotation),
                levelData.overrideIslandScale,
                levelData.islandScale);
        }

        private Transform FindTargetPoint(LevelData levelData, out bool useTargetScale)
        {
            useTargetScale = false;
            if (levelData == null)
                return null;

            if (levelData.islandTargetPoint != null && levelData.islandTargetPoint.HasValue)
            {
                Transform targetPoint = levelData.islandTargetPoint.Resolve();
                if (targetPoint == null)
                {
                    Debug.LogWarning($"Level {levelData.levelNumber}: island target point '{levelData.islandTargetPoint.ScenePath}' was not found. Config position will be used.");
                    return null;
                }

                useTargetScale = levelData.useIslandTargetScale;
                return targetPoint;
            }

            return null;
        }

        private static void ApplyWorldScale(Transform target, Vector3 worldScale)
        {
            Transform parent = target.parent;
            if (parent == null)
            {
                target.localScale = worldScale;
                return;
            }

            Vector3 parentScale = parent.lossyScale;
            target.localScale = new Vector3(
                DivideScale(worldScale.x, parentScale.x),
                DivideScale(worldScale.y, parentScale.y),
                DivideScale(worldScale.z, parentScale.z));
        }

        private static float DivideScale(float value, float parentScale)
        {
            return Mathf.Approximately(parentScale, 0f) ? value : value / parentScale;
        }

        private readonly struct IslandPlacement
        {
            public IslandPlacement(Vector3 position, Quaternion rotation, bool overrideScale, Vector3 worldScale)
            {
                Position = position;
                Rotation = rotation;
                OverrideScale = overrideScale;
                WorldScale = worldScale;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public bool OverrideScale { get; }
            public Vector3 WorldScale { get; }
        }
    }
}
