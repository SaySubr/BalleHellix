using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, Actionplayer.IControllerActions
{
    [Header("Оружие")]
    [SerializeField] private ProjectileShooter shooter;

    private Actionplayer inputActions;
    private bool isFiring;

    private void Awake()
    {
        inputActions = new Actionplayer();
        inputActions.controller.SetCallbacks(this);

        if (shooter == null)
            shooter = GetComponent<ProjectileShooter>();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        if (isFiring && shooter != null && Time.timeScale > 0f)
        {
            shooter.Shoot();
        }
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        
        if (Time.timeScale == 0f) return;

        if (context.phase == InputActionPhase.Performed)
            isFiring = true;
        else if (context.phase == InputActionPhase.Canceled)
            isFiring = false;
    }
}