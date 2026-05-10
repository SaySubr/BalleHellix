using System;
using UnityEngine;

namespace Config
{
    public enum LevelGameType
    {
        Fireball = 0,
        Helix = 1
    }

    [Serializable]
    public class FireballBackgroundSettings
    {
        [Header("Spawn")]
        public bool spawnBackground = true;

        [Tooltip("Optional background prefab for this FireBall level.")]
        public GameObject backgroundPrefab;

        [Tooltip("Optional material applied to every renderer in the spawned background prefab.")]
        public Material backgroundMaterial;

        [Header("Transform")]
        public Vector3 positionOffset = Vector3.zero;
        public Vector3 rotationOffset = Vector3.zero;
        public Vector3 scale = Vector3.one;
        public bool parentToSpawner = true;
    }

    [Serializable]
    public class FireballLevelSettings
    {
        [Header("Prefabs")]
        [Tooltip("Optional tower block skin for this level. Leave empty to use the prefab from the scene.")]
        public GameObject towerBlockPrefab;

        [Tooltip("Optional obstacle skin for this level. Leave empty to use the prefab from the scene.")]
        public GameObject obstaclePrefab;

        [Header("Background")]
        public FireballBackgroundSettings background = new FireballBackgroundSettings();

        [Header("Tower")]
        public bool overrideTower = true;
        public Vector2Int towerHeightRange = new Vector2Int(50, 70);
        [Min(0.05f)] public float blockHeight = 0.5f;
        public bool useVisibilityCulling = true;
        [Min(1)] public int visibleBlocksCount = 10;
        public bool randomizeTowerRotation = true;
        public Vector2 towerRotationSpeedRange = new Vector2(30f, 180f);
        public bool rotateTowerBlocks = true;
        public Vector3 towerBlockStartRotation = Vector3.zero;
        public Vector3 towerRotationAxis = Vector3.up;
        public bool randomizeTowerRotationDirection = true;
        public bool invertTowerRotation = false;
        public bool towerBlockPulsate = false;
        [Min(0f)] public float towerPulseSpeed = 2f;
        [Min(0f)] public float towerPulseAmount = 0.1f;
        public ParticleSystem towerBlockDestroyEffect;
        public bool randomizeTowerScale = true;
        public Vector2 towerScaleRange = new Vector2(0.5f, 1.5f);
        public bool uniformTowerScale = true;

        [Header("Obstacles")]
        public bool overrideObstacles = true;
        public Vector2Int obstacleCountRange = new Vector2Int(5, 20);
        public bool randomSpacing = true;
        public Vector2 spacingRange = new Vector2(1f, 5f);
        public bool randomizeObstacleRotation = true;
        public Vector2 obstacleRotationSpeedRange = new Vector2(30f, 180f);
        public bool randomizeObstacleSpeed = true;
        public Vector2 obstacleSpeedRange = new Vector2(3f, 10f);
        public bool randomizeObstacleScale = true;
        public Vector2 obstacleScaleRange = new Vector2(0.5f, 1.5f);
        public bool uniformObstacleScale = true;
        [Min(0f)] public float obstacleParabolaHeight = 1f;

        [Header("Obstacle Pool")]
        public bool useObjectPool = true;
        [Min(0)] public int poolInitialSize = 30;
        [Min(1)] public int poolMaxSize = 100;
    }

    [Serializable]
    public class HelixLevelSettings
    {
        [Header("Objective")]
        [Tooltip("How many generated Helix rounds must be finished before this map island is completed.")]
        [Min(1)] public int levelsToComplete = 1;

        [Tooltip("Extra floors added after each finished Helix round inside the same island.")]
        [Min(0)] public int additionalFloorsPerCompletedLevel = 2;

        [Header("Floors")]
        [Min(1)] public int numberOfLevels = 10;
        [Min(0.1f)] public float levelHeight = 4f;
        [Range(0.1f, 2f)] public float poleRadius = 0.5f;

        [Header("Mesh")]
        [Range(40, 120)] public int segments = 80;
        [Range(0f, 1f)] public float gapPercentage = 0.2f;

        [Header("Dimensions")]
        [Min(0.1f)] public float innerRadius = 0.5f;
        [Min(0.2f)] public float outerRadius = 2.8f;
        [Min(0.01f)] public float thickness = 0.2f;

        [Header("Danger")]
        [Range(1, 3)] public int maxDangerZones = 2;
        [Range(5, 30)] public int dangerZoneSize = 10;
    }

    [Serializable]
    public class LevelData
    {
        [Header("Main")]
        [Min(1)] public int levelNumber = 1;

        [Tooltip("Used by LevelMapConfig spawner. Manual islands can ignore this.")]
        public Vector3 position;

        [Header("Progress")]
        [Tooltip("Default editor state. Runtime save data has priority when DataController exists.")]
        public bool isUnlocked = true;

        [Range(0, 3)] public int starsEarned = 0;

        [Header("Game")]
        public LevelGameType gameType = LevelGameType.Fireball;

        [HideInInspector] public bool isBonusLevel = false;
        [HideInInspector] public bool legacyBonusMigrated = false;

        [Header("Fireball Generator")]
        public FireballLevelSettings fireball = new FireballLevelSettings();

        [Header("Helix Generator")]
        public HelixLevelSettings helix = new HelixLevelSettings();

        [Header("Island Visual")]
        [Tooltip("Used only by LevelMapConfig spawner. Manual islands use their own prefab/scene object.")]
        public GameObject islandPrefab;

        [Tooltip("Optional material for unlocked island. Leave empty to keep material already on the island.")]
        public Material islandMaterial;

        [Tooltip("Optional material for locked island. Leave empty to tint current material.")]
        public Material lockedIslandMaterial;

        [Tooltip("Tint for locked island when Locked Island Material is empty.")]
        public Color islandColor = Color.gray;

        [Header("Legacy Map Layout")]
        public float startX = -40f;
        public float startZ = -40f;
        public float spacing = 10f;
        public int columns = 10;

        public LevelGameType EffectiveGameType
        {
            get { return isBonusLevel ? LevelGameType.Helix : gameType; }
        }

        public bool IsHelix
        {
            get { return EffectiveGameType == LevelGameType.Helix; }
        }

        public void SyncLegacyFields()
        {
            if (!legacyBonusMigrated && isBonusLevel)
            {
                gameType = LevelGameType.Helix;
            }

            legacyBonusMigrated = true;
            isBonusLevel = gameType == LevelGameType.Helix;
        }
    }
}
