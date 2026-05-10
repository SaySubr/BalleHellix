using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Компонент препятствия - отражает пули по параболе к танку.
/// Препятствие НЕ разрушается при попадании.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Obstacle : MonoBehaviour
{
    [Header("Настройки отражения пули")]
    [Tooltip("Высота параболы при отражении")]
    [SerializeField] private float parabolaHeight = 1f;
    [SerializeField] private Transform tankTarget; // Ссылка на танк
    [SerializeField] private LayerMask tankLayerMask = -1; // Слой танка для Raycast

    [Header("Эффект попадания")]
    [SerializeField] private ParticleSystem hitEffect;
    [SerializeField] private AudioClip hitSound;

    [Header("Настройки коллайдера")]
    [SerializeField] private bool isTrigger = true;

    [Header("Отладка")]
    [SerializeField] private bool showDebugRay = false;

    private bool isActive = true;
    private Collider col;
    private ObstacleMovement movement;

    // Для предотвращения повторного срабатывания
    private HashSet<Bullet> hitBullets = new HashSet<Bullet>();

    // События
    public System.Action<Obstacle, Bullet> OnBulletDeflected;

    private void Awake()
    {
        col = GetComponent<Collider>();
        movement = GetComponent<ObstacleMovement>();

        if (col == null)
            col = gameObject.AddComponent<BoxCollider>();

        // Важно: препятствие остается триггером, чтобы пуля не отскакивала физически
        col.isTrigger = true;

        // Ищем эффект попадания
        if (hitEffect == null)
        {
            hitEffect = GetComponent<ParticleSystem>();
            if (hitEffect == null)
                hitEffect = GetComponentInChildren<ParticleSystem>();
        }

        // Ищем танк если не назначен
        if (tankTarget == null)
        {
            GameObject tank = GameObject.FindGameObjectWithTag("Player");
            if (tank != null)
                tankTarget = tank.transform;
        }
    }

    private void Start()
    {
        Initialize();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // Проверка на пулю
        Bullet bullet = other.GetComponent<Bullet>();
        if (bullet != null && !hitBullets.Contains(bullet))
        {
            Debug.Log($"🛡️ Пуля попала в препятствие!");
            hitBullets.Add(bullet);
            DeflectBullet(bullet);
        }
    }

    /// <summary>
    /// Отразить пулю по параболе к танку.
    /// Пуля разворачивается и летит ПРЯМО К ТАНКУ с достаточной скоростью.
    /// Использует Raycast для точного определения направления к танку.
    /// </summary>
    private void DeflectBullet(Bullet bullet)
    {
        // Эффект попадания
        PlayHitEffect();

        // Вычисляем направление к танку
        if (tankTarget == null)
        {
            GameObject tank = GameObject.FindGameObjectWithTag("Player");
            if (tank != null)
                tankTarget = tank.transform;
        }

        if (tankTarget != null)
        {
            // 1. Направление ПРЯМО К ТАНКУ (не обратно!)
            Vector3 directionToTank = (tankTarget.position - transform.position).normalized;
            float distanceToTank = Vector3.Distance(transform.position, tankTarget.position);

            if (showDebugRay)
            {
                Debug.DrawRay(transform.position, directionToTank * distanceToTank, Color.green, 2f);
                Debug.Log($"📏 Направление к танку: {directionToTank}, дистанция: {distanceToTank}");
            }

            // 2. Добавляем вертикальную составляющую для параболы (вверх)
            //    Чем дальше танк, тем выше парабола
            float heightMultiplier = Mathf.Clamp01(distanceToTank / 50f);
            Vector3 deflectDirection = new Vector3(
                directionToTank.x,
                parabolaHeight * heightMultiplier,
                directionToTank.z
            ).normalized;

            // 3. Устанавливаем ФИКСИРОВАННУЮ скорость (не замедляем!)
            //    Пуля должна быстро долететь до танка
            float deflectSpeed = 20f; // Достаточно быстро, чтобы долететь

            Debug.Log($"🎯 Пуля прилетела! Отражение К ТАНКУ. " +
                      $"Направление: {deflectDirection}, " +
                      $"Скорость: {deflectSpeed}");

            // Отправляем событие для изменения направления пули
            bullet.SetDeflected(deflectDirection, deflectSpeed);

            OnBulletDeflected?.Invoke(this, bullet);

            Debug.Log($"🔄 Пуля отражена К ТАНКУ по параболе! Скорость: {deflectSpeed}");
        }
        else
        {
            Debug.LogWarning("⚠️ Танк не найден для отражения пули!");
        }
    }

    private void PlayHitEffect()
    {
        if (hitEffect != null)
        {
            ParticleSystem effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration + 1f);
        }
        else
        {
            // Создаём простой эффект-заглушку
            GameObject tempEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tempEffect.transform.position = transform.position;
            tempEffect.transform.localScale = Vector3.one * 0.3f;

            Renderer renderer = tempEffect.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.orange;

            Destroy(tempEffect, 0.3f);
        }
    }

    public void Initialize()
    {
        isActive = true;
        hitBullets.Clear();

        // Ищем танк если не назначен
        if (tankTarget == null)
        {
            GameObject tank = GameObject.FindGameObjectWithTag("Player");
            if (tank != null)
                tankTarget = tank.transform;
        }
    }

    public void SetTankTarget(Transform tank)
    {
        tankTarget = tank;
    }

    public void SetParabolaHeight(float height)
    {
        parabolaHeight = Mathf.Max(0f, height);
    }

    public void SetHitEffect(ParticleSystem effect)
    {
        hitEffect = effect;
    }

    public bool IsActive() => isActive;
    
    public void Deactivate()
    {
        isActive = false;
        if (movement != null)
            movement.Stop();
    }
    
    public void Activate()
    {
        isActive = true;
        if (movement != null)
            movement.Resume();
    }
}
