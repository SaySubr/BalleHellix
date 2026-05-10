using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Универсальный пул объектов для оптимизации создания/уничтожения.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 100;
        public bool autoExpand = true;
        public Transform parent;
    }

    [Header("Настройки пулов")]
    [SerializeField] private List<Pool> pools = new List<Pool>();

    [Header("Отладка")]
    [SerializeField] private bool showDebugInfo = false;

    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolSettings;
    private Dictionary<string, int> activeCounts;

    public static ObjectPool Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolSettings = new Dictionary<string, Pool>();
        activeCounts = new Dictionary<string, int>();

        foreach (Pool pool in pools)
        {
            if (pool.prefab == null)
            {
                Debug.LogError($"Пул '{pool.tag}' не имеет префаба!");
                continue;
            }

            // Создаем тег если не задан
            if (string.IsNullOrEmpty(pool.tag))
            {
                pool.tag = pool.prefab.name;
            }

            Queue<GameObject> objectPool = new Queue<GameObject>();
            poolSettings[pool.tag] = pool;
            activeCounts[pool.tag] = 0;

            // Создаем родительский объект
            if (pool.parent == null)
            {
                GameObject parentObj = new GameObject($"Pool_{pool.tag}");
                parentObj.transform.SetParent(transform);
                pool.parent = parentObj.transform;
            }

            // Заполняем пул
            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = CreateNewObject(pool);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    private GameObject CreateNewObject(Pool pool)
    {
        GameObject obj = Instantiate(pool.prefab, pool.parent);
        obj.SetActive(false);
        
        // Добавляем IPooledObject компонент если есть
        IPooledObject pooledObj = obj.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnCreate(pool.tag, this);
        }

        return obj;
    }

    /// <summary>
    /// Получить объект из пула
    /// </summary>
    public GameObject GetFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogError($"Пул '{tag}' не найден!");
            return null;
        }

        Queue<GameObject> objectPool = poolDictionary[tag];
        Pool settings = poolSettings[tag];

        GameObject objectToSpawn = null;

        if (objectPool.Count > 0)
        {
            objectToSpawn = objectPool.Dequeue();
        }
        else if (settings.autoExpand && activeCounts[tag] < settings.maxSize)
        {
            objectToSpawn = CreateNewObject(settings);
        }
        else
        {
            Debug.LogWarning($"Пул '{tag}' пуст и не может быть расширен!");
            return null;
        }

        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        activeCounts[tag]++;

        // Вызываем метод при активации
        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnSpawn();
        }

        return objectToSpawn;
    }

    /// <summary>
    /// Получить объект из пула (без позиции и вращения)
    /// </summary>
    public GameObject GetFromPool(string tag)
    {
        return GetFromPool(tag, Vector3.zero, Quaternion.identity);
    }

    /// <summary>
    /// Вернуть объект в пул
    /// </summary>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogError($"Пул '{tag}' не найден!");
            Destroy(obj);
            return;
        }

        // Вызываем метод при возврате
        IPooledObject pooledObj = obj.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnReturn();
        }

        obj.SetActive(false);
        poolDictionary[tag].Enqueue(obj);
        
        if (activeCounts.ContainsKey(tag))
            activeCounts[tag]--;
    }

    /// <summary>
    /// Вернуть объект в пул (автоматическое определение тега)
    /// </summary>
    public void ReturnToPool(GameObject obj)
    {
        IPooledObject pooledObj = obj.GetComponent<IPooledObject>();
        if (pooledObj != null && !string.IsNullOrEmpty(pooledObj.PoolTag))
        {
            ReturnToPool(pooledObj.PoolTag, obj);
        }
        else
        {
            Debug.LogWarning($"Не удалось определить тег пула для объекта {obj.name}");
            Destroy(obj);
        }
    }

    /// <summary>
    /// Получить количество активных объектов
    /// </summary>
    public int GetActiveCount(string tag)
    {
        return activeCounts.ContainsKey(tag) ? activeCounts[tag] : 0;
    }

    /// <summary>
    /// Получить количество объектов в пуле
    /// </summary>
    public int GetPoolSize(string tag)
    {
        return poolDictionary.ContainsKey(tag) ? poolDictionary[tag].Count : 0;
    }

    /// <summary>
    /// Очистить пул
    /// </summary>
    public void ClearPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
            return;

        Queue<GameObject> objectPool = poolDictionary[tag];
        while (objectPool.Count > 0)
        {
            Destroy(objectPool.Dequeue());
        }
    }

    /// <summary>
    /// Предзагрузить пул (создать дополнительные объекты)
    /// </summary>
    public void Preload(string tag, int count)
    {
        if (!poolSettings.ContainsKey(tag))
            return;

        Pool settings = poolSettings[tag];
        Queue<GameObject> objectPool = poolDictionary[tag];

        int toCreate = Mathf.Min(count, settings.maxSize - activeCounts[tag]);

        for (int i = 0; i < toCreate; i++)
        {
            GameObject obj = CreateNewObject(settings);
            objectPool.Enqueue(obj);
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 500));
        GUILayout.BeginVertical("box");
        GUILayout.Label("📦 Object Pool Debug");

        foreach (var kvp in poolDictionary)
        {
            GUILayout.Label($"{kvp.Key}: Active={activeCounts[kvp.Key]}, InPool={kvp.Value.Count}");
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}

/// <summary>
/// Интерфейс для объектов в пуле
/// </summary>
public interface IPooledObject
{
    string PoolTag { get; }
    void OnCreate(string tag, ObjectPool pool);
    void OnSpawn();
    void OnReturn();
}
