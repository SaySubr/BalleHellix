using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Component, IInitialized
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    internal static readonly string _logHeader = "--> Singleton:";

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"{_logHeader} Instance '{typeof(T)}' already destroyed. Returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();

                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        singletonObject.name = $"{typeof(T).Name}_Singleton";
                        _instance = singletonObject.AddComponent<T>();

                        _instance.Startup();
                        DontDestroyOnLoad(singletonObject);
                    }
                    else
                    {
                        _instance.Startup();
                    }
                }
                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        lock (_lock)
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                _instance.Startup();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"{_logHeader} Multiple instances of '{typeof(T)}' found. Destroying duplicate.");
                Destroy(gameObject);
            }
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _applicationIsQuitting = true;
        }
    }
}

public interface IInitialized
{
    void Startup();
}
