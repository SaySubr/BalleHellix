using System.Collections.Generic;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "LevelMapConfig", menuName = "Data/Level Map Config", order = 52)]
    public class LevelMapConfig : ScriptableObject
    {
        [Header("Levels")]
        public List<LevelData> levels = new List<LevelData>();

        private void OnValidate()
        {
            SyncLegacyLevelTypes();
        }

        public LevelData GetLevelData(int levelNumber)
        {
            return levels.Find(l => l != null && l.levelNumber == levelNumber);
        }

        public int GetLevelIndex(int levelNumber)
        {
            return levels.FindIndex(l => l != null && l.levelNumber == levelNumber);
        }

        public int GetNextLevelNumber(int currentLevel)
        {
            int currentIndex = GetLevelIndex(currentLevel);
            if (currentIndex >= 0 && currentIndex < levels.Count - 1)
            {
                return levels[currentIndex + 1].levelNumber;
            }

            return -1;
        }

        public bool IsLevelUnlocked(int levelNumber)
        {
            LevelData data = GetLevelData(levelNumber);
            return data != null && data.isUnlocked;
        }

        public void SyncLegacyLevelTypes()
        {
            foreach (LevelData level in levels)
            {
                if (level != null)
                {
                    level.SyncLegacyFields();
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Generate Snake Positions")]
        public void GenerateSnakePositions()
        {
            if (levels.Count == 0)
            {
                Debug.LogWarning("LevelMapConfig: level list is empty.");
                return;
            }

            float startX = levels[0].startX;
            float startZ = levels[0].startZ;
            float spacing = levels[0].spacing;
            int columns = Mathf.Max(1, levels[0].columns);

            for (int i = 0; i < levels.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;

                float x = row % 2 == 0
                    ? startX + col * spacing
                    : startX + (columns - 1 - col) * spacing;

                float z = startZ + row * spacing;
                LevelData data = levels[i];

                data.levelNumber = i + 1;
                data.position = new Vector3(x, 0f, z);
                data.isUnlocked = i == 0;
                data.starsEarned = 0;
                data.SyncLegacyFields();

                levels[i] = data;
            }

            Debug.Log($"LevelMapConfig: generated {levels.Count} snake positions.");
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Add New Fireball Level")]
        public void AddNewLevel()
        {
            AddLevel(LevelGameType.Fireball);
        }

        [ContextMenu("Add New Helix Level")]
        public void AddBonusLevel()
        {
            AddLevel(LevelGameType.Helix);
        }

        private void AddLevel(LevelGameType gameType)
        {
            int newNumber = levels.Count + 1;

            LevelData data = new LevelData
            {
                levelNumber = newNumber,
                position = Vector3.zero,
                isUnlocked = newNumber == 1,
                starsEarned = 0,
                gameType = gameType,
                isBonusLevel = gameType == LevelGameType.Helix,
                legacyBonusMigrated = true
            };

            levels.Add(data);
            Debug.Log($"LevelMapConfig: added {gameType} level #{newNumber}.");
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
