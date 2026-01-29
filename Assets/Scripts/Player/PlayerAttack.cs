using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireCooldownSeconds = 0.2f;
    [SerializeField] private ProjectilePool projectilePool;

    private const float MIN_AIM_DEADZONE_SQR = 0.0001f;
    private const float MIN_STICK_DEADZONE_SQR = 0.09f;
    private const float MOUSE_MOVE_DETECT_SQR = 0.1f;

    private enum AimSource
    {
        Mouse,
        Gamepad
    }

    private InputSystem_Actions inputActions;
    private Camera mainCamera;

    private float lastFireTimeSeconds;

    private AimSource activeAimSource = AimSource.Mouse;

    private Vector2 lastAimDirection = Vector2.right;
    private Vector2 lastMousePos;

    private void Awake()
    {
        mainCamera = Camera.main;
        inputActions = new InputSystem_Actions();
        CacheInitialMousePosition();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Fire.performed += OnFirePerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.Fire.performed -= OnFirePerformed;
        inputActions.Player.Disable();
    }

    private void Update()
    {
        // Continuously check to update aim based on the most recent input (Mouse and/or Controller)
        UpdateAimSourceFromStick();
        UpdateAimSourceFromMouseMovement();
    }

    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        // whichever device (Mouse or Controller) that fired gets to be the active aim source for this shot.
        SetAimSourceFromFireDevice(context);

        if (!IsOffCooldown())
        {
            return;
        }

        FireProjectile();
        lastFireTimeSeconds = Time.time;
    }

    private bool IsOffCooldown()
    {
        return Time.time >= lastFireTimeSeconds + fireCooldownSeconds;
    }

    private void FireProjectile()
    {
        if (projectilePool == null)
        {
            Debug.LogError("ProjectilePool is not assigned on PlayerAttack.");
            return;
        }

        Vector3 projectileSpawnPosition = GetProjectileSpawnPosition();
        Vector2 finalDirection = GetFinalFireDirection(projectileSpawnPosition);

        Projectile projectile = projectilePool.Get();
        projectile.Activate(projectileSpawnPosition, finalDirection);
    }

    private Vector3 GetProjectileSpawnPosition()
    {
        // Return firePoint position if assigned, otherwise return the player's position
        return firePoint != null ? firePoint.position : transform.position;
    }

    private void CacheInitialMousePosition()
    {
        if (Mouse.current == null)
        {
            return;
        }

        lastMousePos = Mouse.current.position.ReadValue();
    }

    // Updates the active aim source and cached direction when stick input is detected
    private void UpdateAimSourceFromStick()
    {
        Vector2 stickAim = inputActions.Player.Aim.ReadValue<Vector2>();

        if (stickAim.sqrMagnitude < MIN_STICK_DEADZONE_SQR)
        {
            return;
        }

        activeAimSource = AimSource.Gamepad;
        lastAimDirection = stickAim.normalized;
    }

    // Updates the active aim source and cached direction when mouse movement is detected
    private void UpdateAimSourceFromMouseMovement()
    {
        if (Mouse.current == null)
        {
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 mouseDelta = mousePos - lastMousePos;

        // Mouse position is always "valid", so we require movement to avoid constantly overriding controller aim.
        if (mouseDelta.sqrMagnitude < MOUSE_MOVE_DETECT_SQR)
        {
            return;
        }

        lastMousePos = mousePos;
        activeAimSource = AimSource.Mouse;

        Vector3 aimOrigin = GetProjectileSpawnPosition();
        Vector2 mouseAim = GetMouseAimDirection(aimOrigin);

        if (mouseAim.sqrMagnitude >= MIN_AIM_DEADZONE_SQR)
        {
            lastAimDirection = mouseAim.normalized;
        }
    }

    // Depending on which device triggered the fire action, Use the Cached Aim direction from that device. 
    private void SetAimSourceFromFireDevice(InputAction.CallbackContext context)
    {
        // Get the input device (mouse or gamepad) that triggered this action
        InputDevice device = context.control?.device;

        if (device == null)
        {
            return;
        }

        if (device is Mouse)
        {
            activeAimSource = AimSource.Mouse;
        }

        else if (device is Gamepad)
        {
            activeAimSource = AimSource.Gamepad;
        }
    }

    // Depending on AimSource, get the final fire direction using the cached aim direction for that device.
    private Vector2 GetFinalFireDirection(Vector3 spawnPosition)
    {
        if (activeAimSource == AimSource.Gamepad)
        {
            Vector2 stickAim = inputActions.Player.Aim.ReadValue<Vector2>();

            if (stickAim.sqrMagnitude >= MIN_STICK_DEADZONE_SQR)
            {
                lastAimDirection = stickAim.normalized;
            }

            // When the stick returns to neutral, keep the last direction stored and use that as fire direction.
            return lastAimDirection;
        }

        Vector2 mouseAim = GetMouseAimDirection(spawnPosition);

        if (mouseAim.sqrMagnitude >= MIN_AIM_DEADZONE_SQR)
        {
            lastAimDirection = mouseAim.normalized;
        }

        return lastAimDirection;
    }

    private Vector2 GetMouseAimDirection(Vector3 originWorldPosition)
    {
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found. Ensure a camera is tagged MainCamera.", this);
            return Vector2.zero;
        }

        if (Mouse.current == null)
        {
            Debug.LogError("Mouse input is not available.", this);
            return Vector2.zero;
        }

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, 0.0f));
        mouseWorldPosition.z = originWorldPosition.z;

        return mouseWorldPosition - originWorldPosition;
    }
}
