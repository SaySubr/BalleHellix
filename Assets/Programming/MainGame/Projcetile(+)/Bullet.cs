using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Компонент пули. Обрабатывает движение, коллизии и возврат в пул.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float speed = 30f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private GameObject hitEffect;

    [Header("Настройки параболы")]
    [SerializeField] private float gravity = 9.8f;

    private float damage;
    private Vector3 direction;
    private Vector3 currentVelocity;
    private BulletPool pool;
    private float currentLifetime;
    private bool isActive;
    private bool isDeflected;
    private bool isReturningToPool = false;

    // Игнорировать башню после рикошета
    private bool ignoreTowerBlocks = false;

    private Rigidbody rb;
    private Collider col;

    // Для защиты от повторных срабатываний
    private HashSet<Collider> hitColliders = new HashSet<Collider>();

    public float CurrentSpeed => isDeflected ? currentVelocity.magnitude : speed;
    public bool IsDeflected => isDeflected;

    private void Awake()
    {
        // Получаем компоненты
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // Настройка Rigidbody
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        // Настройка Collider
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Проверяем hitEffect
        if (hitEffect == null)
        {
            Debug.LogWarning($"[Bullet] hitEffect не назначен на {gameObject.name}!");
        }
    }

    private void OnEnable()
    {
        currentLifetime = 0;
        isActive = true;
        isDeflected = false;
        isReturningToPool = false;
        ignoreTowerBlocks = false;
        hitColliders.Clear();

        // Сбрасываем velocity
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        // Включаем коллайдер
        if (col != null)
        {
            col.enabled = true;
        }
    }

    private void OnDisable()
    {
        isActive = false;
        isReturningToPool = false;
        hitColliders.Clear();
    }

    private void Update()
    {
        if (!isActive || isReturningToPool) return;

        if (isDeflected)
        {
            currentVelocity.y -= gravity * Time.deltaTime;
            transform.position += currentVelocity * Time.deltaTime;

            if (currentVelocity != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(currentVelocity);
            }
        }
        else
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        currentLifetime += Time.deltaTime;
        if (currentLifetime >= lifetime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || isReturningToPool) return;
        if (hitColliders.Contains(other)) return;

        Debug.Log($"[Bullet] Попала в: {other.gameObject.name}, Tag: {other.tag}");

        // После рикошета игнорируем башню
        if (isDeflected && ignoreTowerBlocks)
        {
            if (other.CompareTag("TowerBlock") || other.CompareTag("Obstacle"))
            {
                return;
            }
        }

        // Попадание в блок башни
        if (other.CompareTag("TowerBlock"))
        {
            hitColliders.Add(other);
            TowerBlock block = other.GetComponent<TowerBlock>();
            if (block != null)
            {
                block.TakeDamage(Mathf.RoundToInt(damage));
            }

            PlayHitEffect(other.ClosestPoint(transform.position));
            ReturnToPool();
        }
        // Попадание в препятствие
        else if (other.CompareTag("Obstacle"))
        {
            hitColliders.Add(other);
        }
        // Попадание в танк
        else if (other.CompareTag("Player"))
        {
            hitColliders.Add(other);
            TankHealth tank = other.GetComponent<TankHealth>();
            if (tank != null)
            {
                tank.TakeDamage(Mathf.RoundToInt(damage));
            }

            PlayHitEffect(other.ClosestPoint(transform.position));
            ReturnToPool();
        }
    }

    private void PlayHitEffect(Vector3 position)
    {
        if (hitEffect != null)
        {
            Instantiate(hitEffect, position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("[Bullet] hitEffect не назначен!");
        }
    }

    /// <summary>
    /// Инициализировать пулю перед выстрелом.
    /// </summary>
    public void Init(float damage, Vector3 direction, BulletPool pool)
    {
        this.damage = damage;
        this.direction = direction.normalized;
        this.pool = pool;
        this.isDeflected = false;
        this.isReturningToPool = false;
        this.ignoreTowerBlocks = false;

        transform.rotation = Quaternion.LookRotation(this.direction);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        Debug.Log($"[Bullet] Инициализирована: урон={damage}, скорость={speed}");
    }

    /// <summary>
    /// Установить отражённое направление.
    /// </summary>
    public void SetDeflected(Vector3 newDirection, float newSpeed)
    {
        isDeflected = true;
        isReturningToPool = false;
        ignoreTowerBlocks = true;

        currentVelocity = newDirection.normalized * newSpeed;
        transform.rotation = Quaternion.LookRotation(newDirection);
        transform.position += newDirection.normalized * 0.5f;

        Debug.Log($"[Bullet] Отражена! Направление: {newDirection}, Скорость: {newSpeed}");
    }

    /// <summary>
    /// Вернуть пулю в пул.
    /// </summary>
    public void ReturnToPool()
    {
        if (isReturningToPool) return;
        isReturningToPool = true;

        if (col != null)
            col.enabled = false;

        if (pool != null)
        {
            isActive = false;
            gameObject.SetActive(false);
            pool.ReturnBullet(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public float GetCurrentSpeed()
    {
        return isDeflected ? currentVelocity.magnitude : speed;
    }
}
