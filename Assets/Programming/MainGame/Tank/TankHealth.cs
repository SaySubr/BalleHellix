using UnityEngine;

/// <summary>
/// Компонент здоровья танка (игрока).
/// При получении урона от отражённой пули вызывает Game Over.
/// </summary>
public class TankHealth : MonoBehaviour
{
    [Header("Настройки здоровья")]
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int currentHealth;

    [Header("Ссылки")]
    [SerializeField] private LoseSystem loseSystem;

    [Header("Эффекты")]
    [SerializeField] private ParticleSystem damageEffect;
    [SerializeField] private AudioClip damageSound;

    [Header("Skin")]
    [SerializeField] private SkinConfig skinConfig;
    [SerializeField] private Transform skinVisualRoot;
    [SerializeField] private bool hideOriginalRenderersWhenSkinPrefabExists = true;

    private bool isDead = false;

    // События
    public System.Action<int> OnHealthChanged;
    public System.Action OnTankDestroyed;

    private void Awake()
    {
        currentHealth = maxHealth;

        // Ищем LoseSystem если не назначен
        if (loseSystem == null)
        {
            loseSystem = GetComponent<LoseSystem>();
        }

        // Тегаем объект как Player
        if (!CompareTag("Player"))
        {
            gameObject.tag = "Player";
        }
    }

    private void Start()
    {
        SkinRuntimeApplier.ApplySelectedSkinTo(gameObject, SkinTarget.FireballTank, skinConfig, skinVisualRoot, hideOriginalRenderersWhenSkinPrefabExists);

        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"💥 Танк получил урон: {damage}. Осталось здоровья: {currentHealth}/{maxHealth}");

        // Эффект получения урона
        PlayDamageEffect();

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            DestroyTank();
        }
    }

    private void PlayDamageEffect()
    {
        if (damageEffect != null)
        {
            ParticleSystem effect = Instantiate(damageEffect, transform.position, transform.rotation);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration + 1f);
        }

        if (damageSound != null)
        {
            AudioSource.PlayClipAtPoint(damageSound, transform.position);
        }
    }

    private void DestroyTank()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"💀 ТАНК УНИЧТОЖЕН! Game Over!");

        OnTankDestroyed?.Invoke();

        // Вызываем Game Over
        if (loseSystem != null)
        {
            loseSystem.ShowLoseScreen();
        }
        else
        {
            // Если LoseSystem не найден, ищем через FindObjectOfType
            LoseSystem system = GetComponent<LoseSystem>();
            if (system != null)
            {
                system.ShowLoseScreen();
            }
            else
            {
                Debug.LogWarning("⚠️ LoseSystem не найден! Останавливаем игру...");
                Time.timeScale = 0f;
            }
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void SetHealth(int health)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(health, 1, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercent() => (float)currentHealth / maxHealth;
    public bool IsAlive() => !isDead && currentHealth > 0;

    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }
}
