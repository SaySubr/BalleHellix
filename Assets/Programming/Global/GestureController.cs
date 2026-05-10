using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Универсальный контроллер жестов: свайпы, тапы, двойные тапы, долгие нажатия, pinch, rotate, drag.
/// </summary>
public class GestureController : Singleton<GestureController>, IInitialized
{
    [Header("Swipe Settings")]
    [SerializeField] private float deadZoneX = 125f;
    [SerializeField] private float deadZoneY = 125f;

    [Header("Tap Settings")]
    [SerializeField] private float doubleTapTime = 0.3f;
    [SerializeField] private float longPressTime = 0.5f;

    /// <summary>Минимальная дистанция свайпа по оси X.</summary>
    public float DeadZoneX { get => deadZoneX; set => deadZoneX = Mathf.Max(0, value); }
    /// <summary>Минимальная дистанция свайпа по оси Y.</summary>
    public float DeadZoneY { get => deadZoneY; set => deadZoneY = Mathf.Max(0, value); }
    /// <summary>Максимальное время между двумя тапами для DoubleTap.</summary>
    public float DoubleTapTime { get => doubleTapTime; set => doubleTapTime = Mathf.Max(0.05f, value); }
    /// <summary>Время удержания для срабатывания LongPress.</summary>
    public float LongPressTime { get => longPressTime; set => longPressTime = Mathf.Max(0.1f, value); }

    private bool _tap, _swipeLeft, _swipeRight, _swipeDown, _swipeUp;
    private bool _isDragging;
    private Vector2 _startTouch, _swipeDelta;

    private float _lastPinchDistance;
    private float _lastRotationAngle;
    private bool _isPinchingOrRotating;

    private float _lastTapTime = -1f;
    private float _pressStartTime = -1f;
    private bool _longPressTriggered = false;

    /// <summary>Дельта свайпа.</summary>
    public Vector2 SwipeDelta => _swipeDelta;
    /// <summary>Флаг свайпа влево.</summary>
    public bool SwipeLeft => _swipeLeft;
    /// <summary>Флаг свайпа вправо.</summary>
    public bool SwipeRight => _swipeRight;
    /// <summary>Флаг свайпа вверх.</summary>
    public bool SwipeUp => _swipeUp;
    /// <summary>Флаг свайпа вниз.</summary>
    public bool SwipeDown => _swipeDown;
    /// <summary>Флаг одиночного тапа.</summary>
    public bool Tap => _tap;

    /// <summary>Событие одиночного тапа.</summary>
    public event Action OnTap;
    /// <summary>Событие двойного тапа.</summary>
    public event Action OnDoubleTap;
    /// <summary>Событие долгого удержания.</summary>
    public event Action OnLongPress;
    /// <summary>Событие свайпа влево.</summary>
    public event Action OnSwipeLeft;
    /// <summary>Событие свайпа вправо.</summary>
    public event Action OnSwipeRight;
    /// <summary>Событие свайпа вверх.</summary>
    public event Action OnSwipeUp;
    /// <summary>Событие свайпа вниз.</summary>
    public event Action OnSwipeDown;
    /// <summary>Событие свайпа (нормализованное направление: up/down/left/right).</summary>
    public event Action<Vector2> OnSwipe;
    /// <summary>Событие свайпа с полным вектором.</summary>
    public event Action<Vector2> OnSwipeRaw;
    /// <summary>Событие свайпа с нормализованным вектором.</summary>
    public event Action<Vector2> OnSwipeNormalized;
    /// <summary>Событие drag при перетаскивании.</summary>
    public event Action<Vector2> OnDrag;
    /// <summary>Событие окончания свайпа.</summary>
    public event Action OnSwipeEnd;
    /// <summary>Событие pinch (масштабирование двумя пальцами).</summary>
    public event Action<float> OnPinch;
    /// <summary>Событие rotate (вращение двумя пальцами).</summary>
    public event Action<float> OnRotate;
    /// <summary>Событие при уничтожении.</summary>
    public event Action OnPreDestroyed;

    private void Update()
    {
        ResetFlags();

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#endif
        HandleTouchInput();
        CalculateSwipe();
        CheckLongPress();
    }

    private void ResetFlags()
    {
        _tap = _swipeLeft = _swipeRight = _swipeUp = _swipeDown = false;
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RegisterTap();
            _isDragging = true;
            _startTouch = Input.mousePosition;
            _pressStartTime = Time.time;
            _longPressTriggered = false;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            OnSwipeEnd?.Invoke();
            _pressStartTime = -1f;
            _longPressTriggered = false;
            Reset();
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        if (Input.touchCount == 1 && !_isPinchingOrRotating)
        {
            var touch = Input.touches[0];
            if (touch.phase == TouchPhase.Began)
            {
                RegisterTap();
                _isDragging = true;
                _startTouch = touch.position;
                _pressStartTime = Time.time;
                _longPressTriggered = false;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                _isDragging = false;
                OnSwipeEnd?.Invoke();
                _pressStartTime = -1f;
                _longPressTriggered = false;
                Reset();
            }
        }

        if (Input.touchCount == 2)
        {
            _isPinchingOrRotating = true;

            Touch t0 = Input.touches[0];
            Touch t1 = Input.touches[1];

            Vector2 pos0 = t0.position;
            Vector2 pos1 = t1.position;

            float currentPinchDist = Vector2.Distance(pos0, pos1);

            if (_lastPinchDistance != 0)
            {
                float scaleDelta = currentPinchDist / _lastPinchDistance;
                if (Mathf.Abs(scaleDelta - 1) > 0.01f)
                    OnPinch?.Invoke(scaleDelta);
            }
            _lastPinchDistance = currentPinchDist;

            float currentAngle = Mathf.Atan2(pos1.y - pos0.y, pos1.x - pos0.x) * Mathf.Rad2Deg;

            if (_lastRotationAngle != 0)
            {
                float angleDelta = Mathf.DeltaAngle(_lastRotationAngle, currentAngle);
                if (Mathf.Abs(angleDelta) > 0.5f)
                    OnRotate?.Invoke(angleDelta);
            }
            _lastRotationAngle = currentAngle;

            if (t0.phase == TouchPhase.Ended || t1.phase == TouchPhase.Ended ||
                t0.phase == TouchPhase.Canceled || t1.phase == TouchPhase.Canceled)
            {
                _lastPinchDistance = 0;
                _lastRotationAngle = 0;
                _isPinchingOrRotating = false;
            }
        }
    }

    private void CalculateSwipe()
    {
        if (_isPinchingOrRotating) return;

        _swipeDelta = Vector2.zero;

        if (_isDragging)
        {
            if (Input.touches.Length > 0)
                _swipeDelta = Input.touches[0].position - _startTouch;
            else if (Input.GetMouseButton(0))
                _swipeDelta = (Vector2)Input.mousePosition - _startTouch;

            OnDrag?.Invoke(_swipeDelta);
        }

        if (Mathf.Abs(_swipeDelta.x) > deadZoneX || Mathf.Abs(_swipeDelta.y) > deadZoneY)
        {
            Vector2 direction = Vector2.zero;

            if (Mathf.Abs(_swipeDelta.x) > Mathf.Abs(_swipeDelta.y))
            {
                if (_swipeDelta.x < 0)
                {
                    _swipeLeft = true;
                    OnSwipeLeft?.Invoke();
                    direction = Vector2.left;
                }
                else
                {
                    _swipeRight = true;
                    OnSwipeRight?.Invoke();
                    direction = Vector2.right;
                }
            }
            else
            {
                if (_swipeDelta.y < 0)
                {
                    _swipeDown = true;
                    OnSwipeDown?.Invoke();
                    direction = Vector2.down;
                }
                else
                {
                    _swipeUp = true;
                    OnSwipeUp?.Invoke();
                    direction = Vector2.up;
                }
            }

            OnSwipe?.Invoke(direction);
            OnSwipeRaw?.Invoke(_swipeDelta);
            OnSwipeNormalized?.Invoke(direction.normalized);

            Reset();
        }
    }

    private void RegisterTap()
    {
        _tap = true;
        OnTap?.Invoke();

        if (Time.time - _lastTapTime <= doubleTapTime)
        {
            OnDoubleTap?.Invoke();
            _lastTapTime = -1f;
        }
        else
        {
            _lastTapTime = Time.time;
        }
    }

    private void CheckLongPress()
    {
        if (_pressStartTime > 0 && !_longPressTriggered && Time.time - _pressStartTime >= longPressTime)
        {
            OnLongPress?.Invoke();
            _longPressTriggered = true;
        }
    }

    private void Reset()
    {
        _startTouch = Vector2.zero;
        _swipeDelta = Vector2.zero;
        _isDragging = false;
    }

    public void Startup() { }
}
