using UnityEngine;

/// <summary>
/// Компонент блока башни.
/// Спавнит партикл на месте разрушения.
/// </summary>
public class TowerBlock : MonoBehaviour
{
    [Header("Настройки блока")]
    [SerializeField] private int health = 1;

    [Header("Эффект разрушения")]
    [Tooltip("Префаб партикла для эффекта разрушения (назначить в префабе!)")]
    [SerializeField] private ParticleSystem destroyEffect;

    private bool isDestroyed = false;
    private int currentHealth;

    // Событие для генератора
    public System.Action<GameObject> OnBlockDestroyed;

    private void Awake()
    {
        currentHealth = health;

        // Ищем партикл в префабе
        if (destroyEffect == null)
        {
            destroyEffect = GetComponent<ParticleSystem>();
            if (destroyEffect == null)
                destroyEffect = GetComponentInChildren<ParticleSystem>();
        }

        // ВАЖНО: Выключаем партикл при старте (он должен работать только при разрушении)
        if (destroyEffect != null)
        {
            destroyEffect.Stop();
            destroyEffect.gameObject.SetActive(false);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            DestroyBlock();
        }
    }

    public void DestroyBlock()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // Запоминаем позицию перед уничтожением
        Vector3 spawnPosition = transform.position;

        // Уведомляем генератор
        OnBlockDestroyed?.Invoke(gameObject);

        // СПАВНИМ ПАРТИКЛ НА МЕСТЕ РАЗРУШЕНИЯ
        SpawnDestroyEffect(spawnPosition);

        // Отключаем блок (визуально скрываем)
        gameObject.SetActive(false);
    }

    private void SpawnDestroyEffect(Vector3 position)
    {
        if (destroyEffect != null)
        {
            // Создаём копию префаба партикла на месте разрушения
            ParticleSystem effect = Instantiate(destroyEffect, position, Quaternion.identity);
            effect.gameObject.SetActive(true);
            effect.Play();

            // Уничтожаем через время жизни
            float duration = effect.main.duration;
            if (effect.main.loop)
                duration = effect.main.startLifetime.constantMax;

            Destroy(effect.gameObject, duration + 1f);
        }
        else
        {
            // Создаём простой эффект-заглушку
            GameObject tempEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tempEffect.transform.position = position;
            tempEffect.transform.localScale = Vector3.one * 0.5f;

            Renderer renderer = tempEffect.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.red;

            Destroy(tempEffect, 0.5f);
        }
    }

    public bool IsDestroyed() => isDestroyed;

    public void SetHealth(int newHealth)
    {
        health = newHealth;
        currentHealth = newHealth;
    }

    public void SetDestroyEffect(ParticleSystem effect)
    {
        destroyEffect = effect;
        if (destroyEffect != null && destroyEffect.transform.IsChildOf(transform))
        {
            destroyEffect.Stop();
            destroyEffect.gameObject.SetActive(false);
        }
    }
}
