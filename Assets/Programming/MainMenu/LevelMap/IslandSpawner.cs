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
                Destroy(child.gameObject);
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

            GameObject island = Instantiate(levelData.islandPrefab, levelData.position, Quaternion.identity, islandsParent);
            island.name = $"Island_{levelData.levelNumber}";

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
    }
}
