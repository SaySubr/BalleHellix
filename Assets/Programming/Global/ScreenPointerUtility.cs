using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public readonly struct ScreenPointerState
{
    public ScreenPointerState(
        Vector2 position,
        int pointerId,
        bool isPressed,
        bool wasPressedThisFrame,
        bool wasReleasedThisFrame,
        bool isTouch)
    {
        Position = position;
        PointerId = pointerId;
        IsPressed = isPressed;
        WasPressedThisFrame = wasPressedThisFrame;
        WasReleasedThisFrame = wasReleasedThisFrame;
        IsTouch = isTouch;
    }

    public Vector2 Position { get; }
    public int PointerId { get; }
    public bool IsPressed { get; }
    public bool WasPressedThisFrame { get; }
    public bool WasReleasedThisFrame { get; }
    public bool IsTouch { get; }
}

public static class ScreenPointerUtility
{
    private const int MousePointerId = -1;
    private static readonly List<RaycastResult> UiRaycastResults = new List<RaycastResult>();

    public static bool TryGetPrimaryPointer(out ScreenPointerState pointer)
    {
        if (TryGetLegacyTouch(out pointer))
            return true;

        if (TryGetInputSystemTouch(out pointer))
            return true;

        if (TryGetInputSystemMouse(out pointer))
            return true;

        if (TryGetLegacyMouse(out pointer))
            return true;

        pointer = default;
        return false;
    }

    public static bool IsPointerOverUI(Vector2 screenPosition, int pointerId)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        PointerEventData eventData = new PointerEventData(eventSystem)
        {
            position = screenPosition
        };

        UiRaycastResults.Clear();
        eventSystem.RaycastAll(eventData, UiRaycastResults);

        for (int i = 0; i < UiRaycastResults.Count; i++)
        {
            if (IsBlockingUiElement(UiRaycastResults[i].gameObject))
                return true;
        }

        return false;
    }

    private static bool IsBlockingUiElement(GameObject gameObject)
    {
        if (gameObject == null)
            return false;

        return gameObject.GetComponentInParent<Selectable>() != null
            || gameObject.GetComponentInParent<IPointerClickHandler>() != null
            || gameObject.GetComponentInParent<IPointerDownHandler>() != null
            || gameObject.GetComponentInParent<IDragHandler>() != null;
    }

    private static bool TryGetInputSystemTouch(out ScreenPointerState pointer)
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
        {
            pointer = default;
            return false;
        }

        var touch = touchscreen.primaryTouch;
        bool isPressed = touch.press.isPressed;
        bool wasPressed = touch.press.wasPressedThisFrame;
        bool wasReleased = touch.press.wasReleasedThisFrame;

        if (!isPressed && !wasPressed && !wasReleased)
        {
            pointer = default;
            return false;
        }

        pointer = new ScreenPointerState(
            touch.position.ReadValue(),
            touch.touchId.ReadValue(),
            isPressed,
            wasPressed,
            wasReleased,
            true);
        return true;
    }

    private static bool TryGetInputSystemMouse(out ScreenPointerState pointer)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            pointer = default;
            return false;
        }

        bool isPressed = mouse.leftButton.isPressed;
        bool wasPressed = mouse.leftButton.wasPressedThisFrame;
        bool wasReleased = mouse.leftButton.wasReleasedThisFrame;

        if (!isPressed && !wasPressed && !wasReleased)
        {
            pointer = default;
            return false;
        }

        pointer = new ScreenPointerState(
            mouse.position.ReadValue(),
            MousePointerId,
            isPressed,
            wasPressed,
            wasReleased,
            false);
        return true;
    }

    private static bool TryGetLegacyTouch(out ScreenPointerState pointer)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount <= 0)
        {
            pointer = default;
            return false;
        }

        Touch touch = Input.GetTouch(0);
        bool wasPressed = touch.phase == UnityEngine.TouchPhase.Began;
        bool wasReleased = touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled;

        pointer = new ScreenPointerState(
            touch.position,
            touch.fingerId,
            !wasReleased,
            wasPressed,
            wasReleased,
            true);
        return true;
#else
        pointer = default;
        return false;
#endif
    }

    private static bool TryGetLegacyMouse(out ScreenPointerState pointer)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        bool isPressed = Input.GetMouseButton(0);
        bool wasPressed = Input.GetMouseButtonDown(0);
        bool wasReleased = Input.GetMouseButtonUp(0);

        if (!isPressed && !wasPressed && !wasReleased)
        {
            pointer = default;
            return false;
        }

        pointer = new ScreenPointerState(
            Input.mousePosition,
            MousePointerId,
            isPressed,
            wasPressed,
            wasReleased,
            false);
        return true;
#else
        pointer = default;
        return false;
#endif
    }
}
