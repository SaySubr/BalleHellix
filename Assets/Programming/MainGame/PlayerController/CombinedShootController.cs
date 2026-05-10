using UnityEngine;
using UnityEngine.InputSystem;

public class CombinedShootController : MonoBehaviour, Actionplayer.IControllerActions
{
    [Header("Weapon")]
    [SerializeField] private Weapon weapon;

    [Header("Input Settings")]
    [SerializeField] private bool allowMouseShoot = true;
    [SerializeField] private bool allowUIShoot = true;

    private Actionplayer inputActions;
    private bool isMouseFiring;
    private bool isUIFiring;

    private void Awake()
    {
        // Input System
        inputActions = new Actionplayer();
        inputActions.controller.SetCallbacks(this);

        // Find weapon
        if (weapon == null)
            weapon = GetComponent<Weapon>();
    }

    private void OnEnable()
    {
        if (allowMouseShoot)
            inputActions.Enable();
    }

    private void OnDisable()
    {
        if (allowMouseShoot)
            inputActions.Disable();
    }

    private void Update()
    {
        // Стреляем если хотя бы один источник стрельбы активен
        bool shouldFire = (allowMouseShoot && isMouseFiring) || (allowUIShoot && isUIFiring);

        if (shouldFire && weapon != null)
        {
            //weapon.Fire();
        }
    }

    // Вызывается из UI кнопки (в инспекторе)
    public void OnUIButtonDown()
    {
        isUIFiring = true;
    }

    public void OnUIButtonUp()
    {
        isUIFiring = false;
    }

    // Input System callback
    public void OnFire(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
            case InputActionPhase.Performed:
                isMouseFiring = true;
                break;
            case InputActionPhase.Canceled:
                isMouseFiring = false;
                break;
        }
    }
}