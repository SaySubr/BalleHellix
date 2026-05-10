using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Класс - наследник от MonoBehaviour <br/>
/// Отвечает за генерацию препятствий на уровне
/// </summary>
public class PlayerInputMobile : MonoBehaviour
{
    public Action OnUp;
    public Action OnDown;
    public Action OnLeft;
    public Action OnRight;
    public Action OnTap;

    private PlayerInputActionsMobile _inputActions;
  

    private void OnEnable()
    {
        _inputActions = new();
        _inputActions.Enable();

        _inputActions.Player.Move.performed += HandleMove;

        var swipe = GestureController.Instance;

        swipe.OnSwipeRight += HandleSwipeRight;
        swipe.OnSwipeLeft += HandleSwipeLeft;
        swipe.OnSwipeUp += HandleSwipeUp;
        swipe.OnSwipeDown += HandleSwipeDown;
    }

    private void OnDisable()
    {
        _inputActions.Player.Move.performed -= HandleMove;
        _inputActions.Disable();

        var swipe = GestureController.Instance;

        if (swipe != null)
        {
            swipe.OnSwipeRight -= HandleSwipeRight;
            swipe.OnSwipeLeft -= HandleSwipeLeft;
            swipe.OnSwipeUp -= HandleSwipeUp;
            swipe.OnSwipeDown -= HandleSwipeDown;
        }
    }

    private void HandleSwipeDown()
    {
        OnDown?.Invoke();
    }


    private void HandleSwipeUp()
    {
        OnUp?.Invoke();
    }

    private void HandleSwipeLeft()
    {
        OnLeft?.Invoke();
    }

    private void HandleSwipeRight()
    {
        OnRight?.Invoke();
    }

    private void HandleMove(InputAction.CallbackContext context)
    {
        Vector2 dir = context.ReadValue<Vector2>();
        CheckDiraction(dir);
    }

    private void CheckDiraction(Vector2 dir)
    {
        if (dir.x > 0)
            OnRight?.Invoke();

        if (dir.x < 0)
            OnLeft?.Invoke();

        if (dir.y > 0)
            OnUp?.Invoke();

        if (dir.y < 0)
            OnDown?.Invoke();
    }
}
