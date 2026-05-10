using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Компонент движения препятствия по сплайну.
/// </summary>
[RequireComponent(typeof(Transform))]
public class ObstacleMovement : MonoBehaviour
{
    [Header("Настройки сплайна")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private bool loopSpline = true;

    [Header("Параметры движения")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool randomSpeed = true;
    [SerializeField] private Vector2 speedRange = new Vector2(3f, 10f);

    [Header("Направление")]
    [SerializeField] private bool reverseDirection = false;
    [SerializeField] private bool randomDirection = true;

    [Header("Смена направления")]
    [SerializeField] private bool changeDirectionPeriodically = false;
    [SerializeField] private float minDirectionChangeTime = 3f;
    [SerializeField] private float maxDirectionChangeTime = 8f;

    private float splinePosition;
    private float currentSpeed;
    private int direction = 1;
    private float directionChangeTimer;
    private float nextDirectionChangeTime;
    private bool isActive = true;

    private SplineContainer currentSpline;

    public System.Action<ObstacleMovement> OnDirectionChanged;
    public System.Action<ObstacleMovement> OnLoopCompleted;

    private void Awake()
    {
        if (!splineContainer)
            splineContainer = GetComponentInParent<SplineContainer>();

        currentSpline = splineContainer;
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (!isActive || currentSpline == null || currentSpline.Spline.Count < 2)
            return;

        MoveAlongSpline();
        HandleDirectionChange();
    }

    private void MoveAlongSpline()
    {
        float delta = direction * currentSpeed * Time.deltaTime;
        
        if (reverseDirection)
            delta = -delta;

        splinePosition += delta;

        // Нормализация позиции сплайна
        if (loopSpline)
        {
            if (splinePosition >= 1f)
            {
                splinePosition -= 1f;
                OnLoopCompleted?.Invoke(this);
            }
            else if (splinePosition < 0f)
            {
                splinePosition += 1f;
                OnLoopCompleted?.Invoke(this);
            }
        }
        else
        {
            if (splinePosition >= 1f || splinePosition < 0f)
            {
                isActive = false;
                return;
            }
        }

        // Получаем позицию и направление на сплайне
        Vector3 position = currentSpline.EvaluatePosition(splinePosition);
        Vector3 tangent = currentSpline.EvaluateTangent(splinePosition);

        if (tangent != Vector3.zero)
        {
            transform.position = position;
            transform.rotation = Quaternion.LookRotation(tangent);
        }
    }

    private void HandleDirectionChange()
    {
        if (!changeDirectionPeriodically || !randomDirection)
            return;

        directionChangeTimer += Time.deltaTime;

        if (directionChangeTimer >= nextDirectionChangeTime)
        {
            direction = -direction;
            OnDirectionChanged?.Invoke(this);
            directionChangeTimer = 0f;
            SetNextDirectionChangeTime();
        }
    }

    private void SetNextDirectionChangeTime()
    {
        nextDirectionChangeTime = Random.Range(minDirectionChangeTime, maxDirectionChangeTime);
    }

    public void Initialize()
    {
        if (randomSpeed)
        {
            currentSpeed = Random.Range(speedRange.x, speedRange.y);
        }
        else
        {
            currentSpeed = moveSpeed;
        }

        if (randomDirection)
        {
            direction = Random.value > 0.5f ? 1 : -1;
        }

        if (changeDirectionPeriodically)
        {
            SetNextDirectionChangeTime();
            directionChangeTimer = 0f;
        }

        // Начальная позиция на сплайне
        if (currentSpline != null && currentSpline.Spline.Count >= 2)
        {
            splinePosition = Random.Range(0f, 1f);
            Vector3 startPosition = currentSpline.EvaluatePosition(splinePosition);
            Vector3 tangent = currentSpline.EvaluateTangent(splinePosition);
            
            if (tangent != Vector3.zero)
            {
                transform.position = startPosition;
                transform.rotation = Quaternion.LookRotation(tangent);
            }
        }

        isActive = true;
    }

    public void SetSpline(SplineContainer newSpline)
    {
        currentSpline = newSpline;
        splinePosition = 0f;
    }

    public void SetSpeed(float speed)
    {
        currentSpeed = speed;
        randomSpeed = false;
    }

    public void SetSpeedRange(Vector2 range)
    {
        speedRange = range;
        if (randomSpeed)
        {
            currentSpeed = Random.Range(range.x, range.y);
        }
    }

    public void Stop()
    {
        isActive = false;
    }

    public void Resume()
    {
        isActive = true;
    }

    public float GetCurrentSpeed() => currentSpeed;
    public bool IsActive() => isActive;
}
