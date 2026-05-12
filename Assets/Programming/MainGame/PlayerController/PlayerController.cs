using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, Actionplayer.IControllerActions
{
    [Header("Оружие")]
    [SerializeField] private ProjectileShooter shooter;

    [Header("Mobile Input")]
    [SerializeField] private bool allowScreenTouchShoot = true;
    [SerializeField] private bool ignoreInputOverUI = true;

    private Actionplayer inputActions;
    private bool isFiring;
    private bool isPointerFiring;
    private bool pointerStartedOverUI;
    private bool pointerActive;
    private bool controlsEnabled = true;

    private void Awake()
    {
        inputActions = new Actionplayer();
        inputActions.controller.SetCallbacks(this);

        if (shooter == null)
            shooter = GetComponent<ProjectileShooter>();
    }

    private void OnEnable()
    {
        if (controlsEnabled)
            inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
        isFiring = false;
        isPointerFiring = false;
        pointerActive = false;
    }

    private void Update()
    {
        if (!controlsEnabled)
            return;

        UpdateScreenPointerFire();

        if ((isFiring || isPointerFiring) && shooter != null && Time.timeScale > 0f)
        {
            shooter.Shoot();
        }
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (!controlsEnabled)
            return;

        if (context.phase == InputActionPhase.Canceled)
        {
            isFiring = false;
            return;
        }

        if (Time.timeScale == 0f)
            return;

        if (context.phase == InputActionPhase.Started || context.phase == InputActionPhase.Performed)
            isFiring = true;
    }

    private void UpdateScreenPointerFire()
    {
        isPointerFiring = false;

        if (!allowScreenTouchShoot || Time.timeScale <= 0f)
            return;

        if (!ScreenPointerUtility.TryGetPrimaryPointer(out ScreenPointerState pointer))
        {
            pointerActive = false;
            pointerStartedOverUI = false;
            return;
        }

        if (pointer.WasPressedThisFrame)
        {
            pointerActive = true;
            pointerStartedOverUI = ignoreInputOverUI && ScreenPointerUtility.IsPointerOverUI(pointer.Position, pointer.PointerId);
        }

        if (pointer.WasReleasedThisFrame)
        {
            pointerActive = false;
            pointerStartedOverUI = false;
            return;
        }

        if (!pointerActive && pointer.IsPressed)
        {
            pointerActive = true;
            pointerStartedOverUI = ignoreInputOverUI && ScreenPointerUtility.IsPointerOverUI(pointer.Position, pointer.PointerId);
        }

        isPointerFiring = pointer.IsPressed && !pointerStartedOverUI;
    }

    public void SetControlsEnabled(bool isEnabled)
    {
        controlsEnabled = isEnabled;

        if (isEnabled)
        {
            inputActions.Enable();
            return;
        }

        inputActions.Disable();
        isFiring = false;
        isPointerFiring = false;
        pointerActive = false;
        pointerStartedOverUI = false;

        if (shooter != null)
            shooter.StopShooting(true);
    }
}
