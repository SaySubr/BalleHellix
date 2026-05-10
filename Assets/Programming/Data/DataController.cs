using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Data
{
    public class DataController : Singleton<DataController>, IInitialized
    {
        [Header("Save Settings")]
        [SerializeField] private string saveFileName = "savegame.json";
        [SerializeField] private string saveFolder = "Saves";

        private string SavePath => Path.Combine(Application.persistentDataPath, saveFolder, saveFileName);
        private GameSaveData _currentSave;

        public GameSaveData CurrentSave => _currentSave;
        public int Coins => _currentSave?.coins ?? 0;
        public int HighestUnlockedLevel => _currentSave?.highestUnlockedLevel ?? 1;

        public void Startup()
        {
            string folderPath = Path.Combine(Application.persistentDataPath, saveFolder);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            Debug.Log($"Save path: {SavePath}");
            LoadGame();
        }

        public void LoadGame()
        {
            if (File.Exists(SavePath))
            {
                try
                {
                    string json = File.ReadAllText(SavePath);
                    _currentSave = JsonUtility.FromJson<GameSaveData>(json);
                    NormalizeSave();
                    Debug.Log($"Save loaded: level={_currentSave.highestUnlockedLevel}, coins={_currentSave.coins}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Save load error: {e.Message}");
                    CreateNewSave();
                }
            }
            else
            {
                CreateNewSave();
            }
        }

        public void SaveGame()
        {
            if (_currentSave == null)
                return;

            try
            {
                _currentSave.lastSaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string json = JsonUtility.ToJson(_currentSave, true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Save error: {e.Message}");
            }
        }

        private void CreateNewSave()
        {
            _currentSave = new GameSaveData
            {
                highestUnlockedLevel = 1,
                coins = 0,
                purchasedSkins = new List<int> { 1 },
                selectedSkinId = 1,
                skinSaves = CreateDefaultSkinSaves(1),
                lastSaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                totalPlayTime = 0
            };

            _currentSave.levels.Add(new LevelSaveData(1));
            SaveGame();
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            CreateNewSave();
        }

        public bool IsLevelUnlocked(int levelNumber)
        {
            if (_currentSave == null)
                return levelNumber == 1;

            return levelNumber <= _currentSave.highestUnlockedLevel;
        }

        public void UnlockLevel(int levelNumber)
        {
            if (_currentSave == null)
                return;

            if (levelNumber <= _currentSave.highestUnlockedLevel)
                return;

            _currentSave.highestUnlockedLevel = levelNumber;
            AddLevelData(levelNumber);
            SaveGame();
        }

        public void CompleteLevel(int levelNumber, int stars, int score = 0)
        {
            if (_currentSave == null)
                return;

            LevelSaveData levelData = GetLevelData(levelNumber) ?? AddLevelData(levelNumber);

            if (stars > levelData.starsEarned)
                levelData.starsEarned = stars;

            if (score > levelData.bestScore)
                levelData.bestScore = score;

            levelData.completedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            UnlockLevel(levelNumber + 1);
            SaveGame();
        }

        public LevelSaveData GetLevelData(int levelNumber)
        {
            if (_currentSave == null)
                return null;

            return _currentSave.levels.Find(l => l.levelNumber == levelNumber);
        }

        private LevelSaveData AddLevelData(int levelNumber)
        {
            if (_currentSave == null)
                return null;

            LevelSaveData existing = GetLevelData(levelNumber);
            if (existing != null)
                return existing;

            LevelSaveData data = new LevelSaveData(levelNumber)
            {
                isUnlocked = levelNumber <= _currentSave.highestUnlockedLevel
            };

            _currentSave.levels.Add(data);
            return data;
        }

        public int GetLevelStars(int levelNumber)
        {
            LevelSaveData data = GetLevelData(levelNumber);
            return data?.starsEarned ?? 0;
        }

        public int GetLevelBestScore(int levelNumber)
        {
            LevelSaveData data = GetLevelData(levelNumber);
            return data?.bestScore ?? 0;
        }

        public void AddCoins(int amount)
        {
            if (_currentSave == null || amount <= 0)
                return;

            _currentSave.coins += amount;
            Debug.Log($"+{amount} coins. Total: {_currentSave.coins}");
            SaveGame();
        }

        public bool SpendCoins(int amount)
        {
            if (_currentSave == null || amount < 0)
                return false;

            if (_currentSave.coins < amount)
                return false;

            _currentSave.coins -= amount;
            SaveGame();
            return true;
        }

        public bool PurchaseSkin(int skinId)
        {
            return PurchaseSkin(SkinTarget.HelixBall, skinId);
        }

        public bool PurchaseSkin(SkinTarget target, int skinId)
        {
            if (_currentSave == null)
                return false;

            SkinSaveData saveData = GetOrCreateSkinSave(target, skinId);
            if (saveData.purchasedSkinIds.Contains(skinId))
                return false;

            saveData.purchasedSkinIds.Add(skinId);
            SaveLegacySkinFields(target, saveData);
            SaveGame();
            return true;
        }

        public void SelectSkin(int skinId)
        {
            SelectSkin(SkinTarget.HelixBall, skinId);
        }

        public void SelectSkin(SkinTarget target, int skinId)
        {
            if (_currentSave == null)
                return;

            SkinSaveData saveData = GetOrCreateSkinSave(target, skinId);
            if (!saveData.purchasedSkinIds.Contains(skinId))
                return;

            saveData.selectedSkinId = skinId;
            SaveLegacySkinFields(target, saveData);
            SaveGame();
        }

        public bool IsSkinPurchased(int skinId)
        {
            return IsSkinPurchased(SkinTarget.HelixBall, skinId);
        }

        public bool IsSkinPurchased(SkinTarget target, int skinId)
        {
            if (_currentSave == null)
                return false;

            SkinSaveData saveData = GetOrCreateSkinSave(target, skinId);
            return saveData.purchasedSkinIds.Contains(skinId);
        }

        public int GetSelectedSkin()
        {
            return GetSelectedSkin(SkinTarget.HelixBall);
        }

        public int GetSelectedSkin(SkinTarget target)
        {
            if (_currentSave == null)
                return 1;

            return GetOrCreateSkinSave(target, 1).selectedSkinId;
        }

        public void EnsureSkinPurchased(SkinTarget target, int skinId, bool select = false)
        {
            if (_currentSave == null)
                return;

            SkinSaveData saveData = GetOrCreateSkinSave(target, skinId);
            if (!saveData.purchasedSkinIds.Contains(skinId))
                saveData.purchasedSkinIds.Add(skinId);

            if (select || saveData.selectedSkinId < 0)
                saveData.selectedSkinId = skinId;

            SaveLegacySkinFields(target, saveData);
            SaveGame();
        }

        public string GetSavePath()
        {
            return SavePath;
        }

        public bool SaveExists()
        {
            return File.Exists(SavePath);
        }

        private void NormalizeSave()
        {
            if (_currentSave == null)
                return;

            if (_currentSave.levels == null)
                _currentSave.levels = new List<LevelSaveData>();

            if (_currentSave.purchasedSkins == null)
                _currentSave.purchasedSkins = new List<int>();

            if (_currentSave.skinSaves == null)
                _currentSave.skinSaves = new List<SkinSaveData>();

            EnsureSkinSaveExists(SkinTarget.HelixBall, _currentSave.selectedSkinId > 0 ? _currentSave.selectedSkinId : 1);
            EnsureSkinSaveExists(SkinTarget.FireballTank, 1);
        }

        private SkinSaveData GetOrCreateSkinSave(SkinTarget target, int defaultSkinId)
        {
            NormalizeSave();
            int targetValue = (int)target;
            SkinSaveData saveData = _currentSave.skinSaves.Find(s => s.target == targetValue);
            if (saveData != null)
                return saveData;

            saveData = new SkinSaveData(targetValue, defaultSkinId);
            _currentSave.skinSaves.Add(saveData);
            return saveData;
        }

        private void EnsureSkinSaveExists(SkinTarget target, int defaultSkinId)
        {
            int targetValue = (int)target;
            SkinSaveData saveData = _currentSave.skinSaves.Find(s => s.target == targetValue);
            if (saveData == null)
            {
                saveData = new SkinSaveData(targetValue, defaultSkinId);
                _currentSave.skinSaves.Add(saveData);
            }

            if (saveData.purchasedSkinIds == null)
                saveData.purchasedSkinIds = new List<int>();

            if (target == SkinTarget.HelixBall)
            {
                for (int i = 0; i < _currentSave.purchasedSkins.Count; i++)
                {
                    int legacySkinId = _currentSave.purchasedSkins[i];
                    if (!saveData.purchasedSkinIds.Contains(legacySkinId))
                        saveData.purchasedSkinIds.Add(legacySkinId);
                }
            }

            if (saveData.purchasedSkinIds.Count == 0)
                saveData.purchasedSkinIds.Add(defaultSkinId);

            if (!saveData.purchasedSkinIds.Contains(saveData.selectedSkinId))
                saveData.selectedSkinId = saveData.purchasedSkinIds[0];
        }

        private List<SkinSaveData> CreateDefaultSkinSaves(int defaultSkinId)
        {
            return new List<SkinSaveData>
            {
                new SkinSaveData((int)SkinTarget.HelixBall, defaultSkinId),
                new SkinSaveData((int)SkinTarget.FireballTank, defaultSkinId)
            };
        }

        private void SaveLegacySkinFields(SkinTarget target, SkinSaveData saveData)
        {
            if (target != SkinTarget.HelixBall)
                return;

            _currentSave.selectedSkinId = saveData.selectedSkinId;
            _currentSave.purchasedSkins = new List<int>(saveData.purchasedSkinIds);
        }
    }
}
