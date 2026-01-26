using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireCooldownSeconds = 0.2f;

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
    private AimSource lastAimSource = AimSource.Mouse;
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
        UpdateAimSourceFromStick();
        UpdateAimSourceFromMouseMovement();
    }

    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        // Use the firing device as the tiebreaker so stick drift cannot steal mouse shots.
        UpdateAimSourceFromFireInput(context);

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
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile prefab is not assigned on PlayerAttack.");
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        Vector2 finalDirection = GetFinalFireDirection(spawnPosition);

        Projectile projectileInstance = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        projectileInstance.Initialize(finalDirection);
    }

    private Vector3 GetSpawnPosition()
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

    private void UpdateAimSourceFromStick()
    {
        Vector2 stickAim = inputActions.Player.Aim.ReadValue<Vector2>();

        if (stickAim.sqrMagnitude < MIN_STICK_DEADZONE_SQR)
        {
            return;
        }

        lastAimSource = AimSource.Gamepad;
        lastAimDirection = stickAim.normalized;
    }

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
        lastAimSource = AimSource.Mouse;

        Vector3 aimOrigin = GetSpawnPosition();
        Vector2 mouseAim = GetMouseAimDirection(aimOrigin);

        if (mouseAim.sqrMagnitude >= MIN_AIM_DEADZONE_SQR)
        {
            lastAimDirection = mouseAim.normalized;
        }
    }

    private void UpdateAimSourceFromFireInput(InputAction.CallbackContext context)
    {
        if (context.control == null || context.control.device == null)
        {
            return;
        }

        if (context.control.device is Mouse)
        {
            lastAimSource = AimSource.Mouse;
            return;
        }

        if (context.control.device is Gamepad)
        {
            lastAimSource = AimSource.Gamepad;
        }
    }

    private Vector2 GetFinalFireDirection(Vector3 spawnPosition)
    {
        if (lastAimSource == AimSource.Gamepad)
        {
            Vector2 stickAim = inputActions.Player.Aim.ReadValue<Vector2>();

            if (stickAim.sqrMagnitude >= MIN_STICK_DEADZONE_SQR)
            {
                lastAimDirection = stickAim.normalized;
            }

            // When the stick returns to neutral, keep the last direction so shots don't snap to the idle mouse cursor.
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
