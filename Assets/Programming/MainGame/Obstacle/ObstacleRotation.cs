using UnityEngine;

/// <summary>
/// Компонент вращения препятствия.
/// </summary>
public class ObstacleRotation : MonoBehaviour
{
    [Header("Параметры вращения")]
    [SerializeField] private bool rotateAlways = true;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private bool randomSpeed = true;
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(30f, 180f);

    [Header("Ось вращения")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private bool randomAxis = false;

    [Header("Направление")]
    [SerializeField] private bool invertRotation = false;
    [SerializeField] private bool randomDirection = true;

    [Header("Пульсация (опционально)")]
    [SerializeField] private bool pulsate = false;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;

    private Vector3 originalScale;
    private float pulsePhase;
    private float currentSpeed;
    private int direction = 1;

    private void Start()
    {
        originalScale = transform.localScale;
        pulsePhase = Random.Range(0f, 100f);
        Initialize();
    }

    private void Update()
    {
        if (!rotateAlways)
            return;

        float delta = direction * currentSpeed * Time.deltaTime;
        
        if (invertRotation)
            delta = -delta;

        transform.Rotate(rotationAxis * delta);

        if (pulsate)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed + pulsePhase) * pulseAmount;
            transform.localScale = originalScale * pulse;
        }
    }

    public void Initialize()
    {
        // Случайная скорость
        if (randomSpeed)
        {
            currentSpeed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y);
        }
        else
        {
            currentSpeed = rotationSpeed;
        }

        // Случайное направление
        if (randomDirection)
        {
            invertRotation = Random.value > 0.5f;
        }

        // Случайная ось
        if (randomAxis)
        {
            int axis = Random.Range(0, 3);
            switch (axis)
            {
                case 0: rotationAxis = Vector3.up; break;
                case 1: rotationAxis = Vector3.right; break;
                case 2: rotationAxis = Vector3.forward; break;
            }
        }

        direction = invertRotation ? -1 : 1;
    }

    public void SetRotationSpeed(float speed)
    {
        currentSpeed = speed;
        randomSpeed = false;
    }

    public void SetRotationSpeedRange(Vector2 range)
    {
        rotationSpeedRange = range;
        if (randomSpeed)
        {
            currentSpeed = Random.Range(range.x, range.y);
        }
    }

    public void SetRotationAxis(Vector3 axis)
    {
        rotationAxis = axis.normalized;
        randomAxis = false;
    }

    public void SetInverted(bool invert)
    {
        invertRotation = invert;
        direction = invert ? -1 : 1;
    }

    public void EnablePulsation(bool enable, float speed = 2f, float amount = 0.1f)
    {
        pulsate = enable;
        pulseSpeed = speed;
        pulseAmount = amount;
    }

    public void RandomizeAll()
    {
        SetRotationSpeedRange(rotationSpeedRange);
        
        // Случайная ось
        int axis = Random.Range(0, 3);
        switch (axis)
        {
            case 0: rotationAxis = Vector3.up; break;
            case 1: rotationAxis = Vector3.right; break;
            case 2: rotationAxis = Vector3.forward; break;
        }
        
        invertRotation = Random.value > 0.5f;
        direction = invertRotation ? -1 : 1;
    }

    public void StopRotation()
    {
        rotateAlways = false;
    }

    public void ResumeRotation()
    {
        rotateAlways = true;
    }
}
