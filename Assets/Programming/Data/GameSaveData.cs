using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class GameSaveData
    {
        [Header("Progress")]
        public int highestUnlockedLevel = 1;

        [Header("Levels")]
        public List<LevelSaveData> levels = new List<LevelSaveData>();

        [Header("Currency")]
        public int coins = 0;

        [Header("Legacy Skins")]
        public List<int> purchasedSkins = new List<int>();
        public int selectedSkinId = 1;

        [Header("Skins")]
        public List<SkinSaveData> skinSaves = new List<SkinSaveData>();

        [Header("Meta")]
        public string lastSaveDate = "";
        public int totalPlayTime = 0;
    }

    [Serializable]
    public class SkinSaveData
    {
        public int target;
        public int selectedSkinId;
        public List<int> purchasedSkinIds = new List<int>();

        public SkinSaveData(int target, int defaultSkinId)
        {
            this.target = target;
            selectedSkinId = defaultSkinId;
            purchasedSkinIds.Add(defaultSkinId);
        }
    }

    [Serializable]
    public class LevelSaveData
    {
        public int levelNumber;
        public bool isUnlocked;
        public int starsEarned;
        public int bestScore;
        public string completedDate;

        public LevelSaveData(int levelNumber)
        {
            this.levelNumber = levelNumber;
            isUnlocked = levelNumber == 1;
            starsEarned = 0;
            bestScore = 0;
            completedDate = "";
        }
    }
}
