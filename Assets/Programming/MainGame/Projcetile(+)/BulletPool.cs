using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Пул объектов для пуль. Оптимизирует создание и уничтожение снарядов.
/// </summary>
public class BulletPool : MonoBehaviour
{
    [Header("Префаб пули")]
    [Tooltip("Префаб пули для пула. Если не назначен, будет найден автоматически.")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("Настройки пула")]
    [Tooltip("Начальный размер пула")]
    [SerializeField] private int poolSize = 30;
    [Tooltip("Можно ли расширять пул")]
    [SerializeField] private bool expandable = true;
    [Tooltip("Максимальный размер пула")]
    [SerializeField] private int maxSize = 100;

    private Queue<Bullet> bulletPool = new Queue<Bullet>();
    private HashSet<Bullet> activeBullets = new HashSet<Bullet>();

    private void Awake()
    {
        // Если префаб не назначен, ищем его
        if (bulletPrefab == null)
        {
            // Ищем в Resources
            bulletPrefab = Resources.Load<GameObject>("Bullet");
            
            if (bulletPrefab == null)
            {
                // Ищем в сцене первую пулю
                Bullet[] bullets = FindObjectsOfType<Bullet>();
                if (bullets.Length > 0)
                {
                    bulletPrefab = bullets[0].gameObject;
                    Debug.Log($"BulletPool: Префаб найден через сцену: {bulletPrefab.name}");
                }
            }
        }

        InitializePool();
    }

    private void InitializePool()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletPool: bulletPrefab НЕ найден! Назначьте его в инспекторе или поместите в Resources/Bullet.prefab");
            return;
        }

        Debug.Log($"BulletPool: Инициализация с префабом {bulletPrefab.name}");

        for (int i = 0; i < poolSize; i++)
        {
            CreateBullet();
        }

        Debug.Log($"BulletPool: Создан пул на {poolSize} пуль");
    }

    private void CreateBullet()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletPool: bulletPrefab потерян при создании пули!");
            return;
        }

        GameObject bulletObj = Instantiate(bulletPrefab, transform);
        bulletObj.SetActive(false);
        bulletObj.name = "Bullet";

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet == null)
        {
            Debug.LogError("BulletPool: У префаба нет компонента Bullet!");
            bullet = bulletObj.AddComponent<Bullet>();
        }

        bulletPool.Enqueue(bullet);
    }

    /// <summary>
    /// Получить пулю из пула.
    /// </summary>
    public Bullet GetBullet()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletPool: bulletPrefab потерян! Невозможно выдать пулю.");
            return null;
        }

        Bullet bullet;

        if (bulletPool.Count > 0)
        {
            bullet = bulletPool.Dequeue();
            
            if (bullet == null || bullet.gameObject == null)
            {
                Debug.LogWarning("BulletPool: Пуля в пуле оказалась null, создаём новую");
                CreateBullet();
                bullet = bulletPool.Dequeue();
            }
        }
        else if (expandable && activeBullets.Count < maxSize)
        {
            Debug.LogWarning($"BulletPool: Пул пуст, создаём новую пулю (всего активно: {activeBullets.Count})");
            CreateBullet();
            bullet = bulletPool.Dequeue();
        }
        else
        {
            Debug.LogWarning($"BulletPool: Пул пуст и достигнут максимум ({maxSize})!");
            return null;
        }

        bullet.gameObject.SetActive(true);
        activeBullets.Add(bullet);
        
        Debug.Log($"BulletPool: Выдана пуля. Активно: {activeBullets.Count}, В пуле: {bulletPool.Count}");
        
        return bullet;
    }

    /// <summary>
    /// Вернуть пулю в пул.
    /// </summary>
    public void ReturnBullet(Bullet bullet)
    {
        if (bullet == null)
        {
            Debug.LogWarning("BulletPool: Попытка вернуть null пулю!");
            return;
        }

        if (bullet.gameObject == null)
        {
            Debug.LogWarning("BulletPool: Пуля имеет null gameObject!");
            activeBullets.Remove(bullet);
            return;
        }

        if (!activeBullets.Contains(bullet))
        {
            Debug.LogWarning($"BulletPool: Попытка вернуть уже возвращённую пулю!");
            return;
        }

        activeBullets.Remove(bullet);
        bullet.gameObject.SetActive(false);
        bulletPool.Enqueue(bullet);
        
        Debug.Log($"BulletPool: Пуля возвращена. Активно: {activeBullets.Count}, В пуле: {bulletPool.Count}");
    }

    /// <summary>
    /// Очистить весь пул.
    /// </summary>
    public void ReturnAllActiveBullets()
    {
        if (activeBullets.Count == 0)
            return;

        List<Bullet> bulletsToReturn = new List<Bullet>(activeBullets);
        for (int i = 0; i < bulletsToReturn.Count; i++)
        {
            Bullet bullet = bulletsToReturn[i];
            if (bullet != null)
                bullet.ReturnToPool();
        }
    }

    public void Clear()
    {
        foreach (var bullet in bulletPool)
        {
            if (bullet != null && bullet.gameObject != null)
                Destroy(bullet.gameObject);
        }
        bulletPool.Clear();
        activeBullets.Clear();
    }

    public int GetActiveCount() => activeBullets.Count;
    public int GetPoolCount() => bulletPool.Count;
    public int GetTotalCount() => activeBullets.Count + bulletPool.Count;
}
