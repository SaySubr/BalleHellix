using UnityEngine;
using UnityEngine.InputSystem;

public class StoreSwipeNavigator : MonoBehaviour
{
    [SerializeField] private SkinSelector skinSelector;
    [SerializeField] private StoreUIHandler storeUIHandler;
    [SerializeField] private float minSwipeDistance = 80f;
    [SerializeField] private float axisBias = 1.25f;

    private bool _isPressed;
    private Vector2 _startPosition;

    private void Awake()
    {
        if (skinSelector == null)
            skinSelector = FindFirstObjectByType<SkinSelector>();

        if (storeUIHandler == null)
            storeUIHandler = FindFirstObjectByType<StoreUIHandler>();
    }

    public void OnClick_NextSkin()
    {
        skinSelector?.ToNext();
    }

    public void OnClick_PrevSkin()
    {
        skinSelector?.ToPrev();
    }

    public void OnClick_NextTarget()
    {
        skinSelector?.ToNextTarget();
    }

    public void OnClick_PrevTarget()
    {
        skinSelector?.ToPrevTarget();
    }

    public void OnClick_PurchaseOrSelect()
    {
        if (storeUIHandler == null)
            storeUIHandler = FindFirstObjectByType<StoreUIHandler>();

        storeUIHandler?.OnClick_PurchaseOrSelect();
    }

    public void OnClick_ToMainMenu()
    {
        if (storeUIHandler == null)
            storeUIHandler = FindFirstObjectByType<StoreUIHandler>();

        storeUIHandler?.OnClick_ToMainMenu();
    }

    private void Update()
    {
        HandleMouse();
        HandleTouch();
    }

    private void HandleMouse()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            Begin(Mouse.current.position.ReadValue());

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            End(Mouse.current.position.ReadValue());
    }

    private void HandleTouch()
    {
        if (Touchscreen.current == null)
            return;

        var touch = Touchscreen.current.primaryTouch;
        if (touch.press.wasPressedThisFrame)
            Begin(touch.position.ReadValue());

        if (touch.press.wasReleasedThisFrame)
            End(touch.position.ReadValue());
    }

    private void Begin(Vector2 position)
    {
        _isPressed = true;
        _startPosition = position;
    }

    private void End(Vector2 position)
    {
        if (!_isPressed || skinSelector == null)
            return;

        _isPressed = false;
        Vector2 delta = position - _startPosition;
        if (delta.magnitude < minSwipeDistance)
            return;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * axisBias)
        {
            if (delta.x < 0f)
                skinSelector.ToNext();
            else
                skinSelector.ToPrev();
        }
        else if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * axisBias)
        {
            if (delta.y > 0f)
                skinSelector.ToPrevTarget();
            else
                skinSelector.ToNextTarget();
        }
    }
}
