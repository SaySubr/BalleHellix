using System;
using Config;
using Data;
using MainGame;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MainMenu
{
    public class GameLauncher : MonoBehaviour
    {
        [Header("Scenes")]
        [SerializeField] private int gameSceneIndex = 1;
        [SerializeField] private int bonusSceneIndex = 2;
        [SerializeField] private int storeSceneIndex = 3;

        [Header("Optional Map Config")]
        [SerializeField] private LevelMapConfig levelMapConfig;

        private static GameLauncher _instance;
        public static GameLauncher Instance => _instance;
        public static event Action<LevelData> LevelLaunchRequested;

        private int _currentLevelNumber;
        private bool _isBonusMode;
        private LevelData _currentLevelData;
        private bool _currentLevelCompleted;

        public int CurrentLevelNumber => _currentLevelNumber;
        public bool IsBonusMode => _isBonusMode;
        public LevelData CurrentLevelData => _currentLevelData;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (SceneManager.GetActiveScene().buildIndex == 0)
                {
                    Destroy(_instance.gameObject);
                    _instance = this;
                    DontDestroyOnLoad(gameObject);

                    if (levelMapConfig != null)
                        levelMapConfig.SyncLegacyLevelTypes();

                    return;
                }

                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (levelMapConfig != null)
                levelMapConfig.SyncLegacyLevelTypes();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public void LaunchLastUnlockedLevel()
        {
            int lastUnlockedLevel = GetLastUnlockedLevel();
            LaunchLevel(lastUnlockedLevel);
        }

        public void LaunchLevel(int levelNumber)
        {
            LevelData data = levelMapConfig != null ? levelMapConfig.GetLevelData(levelNumber) : null;
            if (data == null)
            {
                Debug.LogError($"GameLauncher: level {levelNumber} was not found in LevelMapConfig.");
                return;
            }

            LaunchLevel(data);
        }

        public void LaunchLevel(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError("GameLauncher: level data is null.");
                return;
            }

            levelData.SyncLegacyFields();

            if (!ResolveUnlockedState(levelData))
            {
                Debug.LogWarning($"GameLauncher: level {levelData.levelNumber} is locked.");
                return;
            }

            _currentLevelNumber = levelData.levelNumber;
            _currentLevelData = levelData;
            _isBonusMode = levelData.EffectiveGameType == LevelGameType.Helix;
            _currentLevelCompleted = false;

            ResetGeneratedSession();

            int sceneIndex = _isBonusMode ? bonusSceneIndex : gameSceneIndex;
            Debug.Log($"GameLauncher: launch level {_currentLevelNumber} ({levelData.EffectiveGameType}) in scene {sceneIndex}.");
            LevelLaunchRequested?.Invoke(levelData);
            SceneManager.LoadScene(sceneIndex);
        }

        public void LaunchBonusLevel(int levelNumber)
        {
            LevelData data = levelMapConfig != null ? levelMapConfig.GetLevelData(levelNumber) : null;
            if (data == null)
            {
                Debug.LogError($"GameLauncher: bonus level {levelNumber} was not found.");
                return;
            }

            data.gameType = LevelGameType.Helix;
            data.isBonusLevel = true;
            LaunchLevel(data);
        }

        public void ReturnToMenu()
        {
            ReturnToMainMenu();
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }

        public void LaunchStore()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(storeSceneIndex);
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LaunchNextLevel()
        {
            if (_currentLevelNumber <= 0)
            {
                ReturnToMenu();
                return;
            }

            int nextLevel = levelMapConfig != null
                ? levelMapConfig.GetNextLevelNumber(_currentLevelNumber)
                : -1;

            if (nextLevel > 0)
            {
                LaunchLevel(nextLevel);
            }
            else
            {
                ReturnToMenu();
            }
        }

        public void CompleteLevel(int stars)
        {
            CompleteLevel(stars, 0, 0);
        }

        public void CompleteLevel(int stars, int score, int coinReward)
        {
            if (_currentLevelNumber <= 0)
                return;

            if (!_currentLevelCompleted && DataController.Instance != null)
            {
                DataController.Instance.CompleteLevel(_currentLevelNumber, stars, score);
                DataController.Instance.AddCoins(coinReward);
                _currentLevelCompleted = true;
            }

            LevelData data = GetCurrentLevelData();
            if (data != null && stars > data.starsEarned)
            {
                data.starsEarned = stars;
            }

            if (levelMapConfig == null)
                return;

            int nextLevel = levelMapConfig.GetNextLevelNumber(_currentLevelNumber);
            if (nextLevel <= 0)
                return;

            LevelData nextData = levelMapConfig.GetLevelData(nextLevel);
            if (nextData != null)
                nextData.isUnlocked = true;
        }

        public LevelData GetCurrentLevelData()
        {
            if (_currentLevelData != null)
                return _currentLevelData;

            if (levelMapConfig == null || _currentLevelNumber <= 0)
                return null;

            return levelMapConfig.GetLevelData(_currentLevelNumber);
        }

        public bool IsLevelUnlocked(int levelNumber)
        {
            LevelData data = levelMapConfig != null ? levelMapConfig.GetLevelData(levelNumber) : null;
            if (data == null)
            {
                return DataController.Instance != null && DataController.Instance.IsLevelUnlocked(levelNumber);
            }

            return ResolveUnlockedState(data);
        }

        public int GetLastUnlockedLevel()
        {
            if (levelMapConfig == null || levelMapConfig.levels.Count == 0)
                return 1;

            int highestUnlocked = DataController.Instance != null
                ? DataController.Instance.HighestUnlockedLevel
                : 1;

            for (int i = levelMapConfig.levels.Count - 1; i >= 0; i--)
            {
                LevelData level = levelMapConfig.levels[i];
                if (level == null)
                    continue;

                if (DataController.Instance != null)
                {
                    if (level.levelNumber <= highestUnlocked)
                        return level.levelNumber;
                }
                else if (level.isUnlocked)
                {
                    return level.levelNumber;
                }
            }

            return levelMapConfig.levels[0].levelNumber;
        }

        private bool ResolveUnlockedState(LevelData levelData)
        {
            if (DataController.Instance != null)
            {
                return DataController.Instance.IsLevelUnlocked(levelData.levelNumber);
            }

            return levelData.isUnlocked;
        }

        private void ResetGeneratedSession()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.ResetForNewGame();
            }
        }
    }
}
