using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// Менеджер сессии. Хранит данные уровня между рестартами.
    /// Не уничтожается при загрузке сцены.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        private static SessionManager _instance;
        public static SessionManager Instance => _instance;

        // Данные башни
        public int towerHeight = 0;
        public int towerSeed = 0;

        // Данные препятствий
        public int obstacleCount = 0;
        public int obstacleSeed = 0;

        // Флаг: первый запуск или рестарт
        public bool IsFirstLaunch => towerHeight == 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// Сохранить данные башни
        /// </summary>
        public void SaveTowerData(int height, int seed)
        {
            towerHeight = height;
            towerSeed = seed;
            Debug.Log($"💾 SessionManager: сохранена башня (высота={height}, seed={seed})");
        }

        /// <summary>
        /// Сохранить данные препятствий
        /// </summary>
        public void SaveObstacleData(int count, int seed)
        {
            obstacleCount = count;
            obstacleSeed = seed;
            Debug.Log($"💾 SessionManager: сохранены препятствия (кол-во={count}, seed={seed})");
        }

        /// <summary>
        /// Сбросить для новой игры
        /// </summary>
        public void ResetForNewGame()
        {
            towerHeight = 0;
            towerSeed = 0;
            obstacleCount = 0;
            obstacleSeed = 0;
            Debug.Log("🔄 SessionManager: сброшено для новой игры");
        }

        /// <summary>
        /// Очистить данные (для рестарта)
        /// </summary>
        public void Clear()
        {
            towerHeight = 0;
            towerSeed = 0;
            obstacleCount = 0;
            obstacleSeed = 0;
        }
    }
}
