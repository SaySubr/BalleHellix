using UnityEngine;

/// <summary>
/// Компонент управления цветом препятствия.
/// Красит напрямую через material.color (как в TowerGenerator).
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ObstacleColor : MonoBehaviour
{
    [Header("Настройки цвета")]
    [SerializeField] private bool randomColor = true;
    [SerializeField] private Color startColor = Color.white;

    [Header("HSV диапазон для случайного цвета")]
    [Range(0f, 1f)] [SerializeField] private float hueMin = 0f;
    [Range(0f, 1f)] [SerializeField] private float hueMax = 1f;
    [Range(0f, 1f)] [SerializeField] private float saturationMin = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float saturationMax = 1f;
    [Range(0f, 1f)] [SerializeField] private float valueMin = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float valueMax = 1f;

    [Header("Градиент (опционально)")]
    [SerializeField] private bool useGradient = false;
    [SerializeField] private Gradient colorGradient;

    [Header("Анимация цвета")]
    [SerializeField] private bool animateColor = false;
    [SerializeField] private float colorChangeSpeed = 1f;
    [SerializeField] private bool loopColorAnimation = true;

    private Renderer objectRenderer;
    private Color currentColor;
    private float colorTimer;
    private float hueOffset;

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
            objectRenderer = gameObject.AddComponent<Renderer>();
    }

    private void Start()
    {
        // Применяем цвет сразу при старте
        ApplyRandomColor();
    }

    private void OnEnable()
    {
        // Применяем новый случайный цвет при активации (для пула объектов)
        ApplyRandomColor();
    }

    private void Update()
    {
        if (animateColor)
        {
            AnimateColor();
        }
    }

    private void AnimateColor()
    {
        colorTimer += Time.deltaTime * colorChangeSpeed;

        float t = colorTimer;
        if (loopColorAnimation)
        {
            t = Mathf.PingPong(colorTimer, 1f);
        }
        else
        {
            t = Mathf.Clamp01(t);
        }

        if (useGradient && colorGradient != null)
        {
            SetColor(colorGradient.Evaluate(t));
        }
        else
        {
            // Анимация по HSV
            Color.RGBToHSV(currentColor, out float h, out float s, out float v);
            h = (h + hueOffset + Time.deltaTime * colorChangeSpeed) % 1f;
            SetColor(Color.HSVToRGB(h, s, v));
        }
    }

    public void Initialize()
    {
        if (randomColor)
        {
            float hue = Random.Range(hueMin, hueMax);
            float saturation = Random.Range(saturationMin, saturationMax);
            float value = Random.Range(valueMin, valueMax);

            currentColor = Color.HSVToRGB(hue, saturation, value);
        }
        else
        {
            currentColor = startColor;
        }

        ApplyColor();
        hueOffset = Random.Range(0f, 1f);
        colorTimer = 0f;
    }

    private void ApplyColor()
    {
        if (objectRenderer != null)
        {
            // Красим напрямую через material.color (как в TowerGenerator)
            objectRenderer.material.color = currentColor;
           
        }
        else
        {
            Debug.LogWarning($"⚠️ ObstacleColor: Renderer null на {gameObject.name}");
        }
    }

    public void SetColor(Color color)
    {
        currentColor = color;
        randomColor = false;
        ApplyColor();
    }

    public void SetColorRange(float hueMin, float hueMax, float satMin, float satMax, float valMin, float valMax)
    {
        this.hueMin = hueMin;
        this.hueMax = hueMax;
        this.saturationMin = satMin;
        this.saturationMax = satMax;
        this.valueMin = valMin;
        this.valueMax = valMax;
        
        if (randomColor)
        {
            Initialize();
        }
    }

    public void SetGradient(Gradient gradient)
    {
        colorGradient = gradient;
        useGradient = true;
        randomColor = false;
    }

    public void EnableColorAnimation(bool enable, float speed = 1f, bool loop = true)
    {
        animateColor = enable;
        colorChangeSpeed = speed;
        loopColorAnimation = loop;
    }

    public Color GetCurrentColor() => currentColor;

    /// <summary>
    /// Применить случайный цвет немедленно (для использования с пулом объектов)
    /// </summary>
    public void ApplyRandomColor()
    {
        // Проверяем renderer
        if (objectRenderer == null)
        {
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer == null)
            {
                Debug.LogWarning($"⚠️ ObstacleColor: Renderer не найден на {gameObject.name}");
                return;
            }
        }

        // Генерируем случайный цвет
        float hue = Random.Range(hueMin, hueMax);
        float saturation = Random.Range(saturationMin, saturationMax);
        float value = Random.Range(valueMin, valueMax);
        currentColor = Color.HSVToRGB(hue, saturation, value);

        // Красим напрямую
        ApplyColor();

        Debug.Log($"🎨 ObstacleColor: Применён цвет {currentColor} (HSV: {hue:F2}, {saturation:F2}, {value:F2}) на {gameObject.name}");
    }
}
