using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
    public const string MISSED_ATTACK_LOCK_KEY = "MISSED_ATTACK";

    [SerializeField] private BeatHandler beathandler;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float fireCooldownSeconds = 0.2f;
    private const float MIN_AIM_DEADZONE_SQR = 0.0001f;
    private const float MIN_STICK_DEADZONE_SQR = 0.09f;
    private const float MOUSE_MOVE_DETECT_SQR = 0.1f;
    private InputSystem_Actions inputActions;
    private Camera mainCamera;

    private YellowNoteEffectHandler yellowEffectHandler;
    private PurpleNoteEffectHandler purpleEffectHandler;
    private RedNoteEffectHandler redEffectHandler;
    private BlueNoteEffectHandler blueEffectHandler;

    private float lastFireTimeSeconds;

    private AimSource activeAimSource = AimSource.Mouse;

    private Vector2 lastAimDirection = Vector2.right;
    private Vector2 lastMousePos;

    // hashset of attack locks
    private HashSet<string> attackLocks = new HashSet<string>();

    private enum AimSource
    {
        Mouse,
        Gamepad
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        inputActions = new InputSystem_Actions();
        CacheInitialMousePosition();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.FireYellow.performed += OnFirePerformed;
        inputActions.Player.FirePurple.performed += OnFirePerformed;
        inputActions.Player.FireRed.performed += OnFirePerformed;
        inputActions.Player.FireBlue.performed += OnFirePerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.FireBlue.performed -= OnFirePerformed;
        inputActions.Player.FireRed.performed -= OnFirePerformed;
        inputActions.Player.FirePurple.performed -= OnFirePerformed;
        inputActions.Player.FireYellow.performed -= OnFirePerformed;
        inputActions.Player.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        yellowEffectHandler = GetComponent<YellowNoteEffectHandler>();
        purpleEffectHandler = GetComponent<PurpleNoteEffectHandler>();
        redEffectHandler = GetComponent<RedNoteEffectHandler>();
        blueEffectHandler = GetComponent<BlueNoteEffectHandler>();
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

        if(attackLocks.Count > 0)
        {
            return;
        }

        if (!IsOffCooldown())
        {
            return;
        }

        // if you fire off beat lock attacks until next beat
        if (beathandler != null && !beathandler.ValidAttackInterval) 
        {
            AddAttackLock(MISSED_ATTACK_LOCK_KEY);
            return;
        }

        // Activates the different effect handlers
        switch (context.action.name)
        {
            case "FireYellow":
            {
                yellowEffectHandler?.Fire();
                break;        
            }
            case "FirePurple":
            {
                purpleEffectHandler?.Fire();
                break;        
            }
            case "FireRed":
            {
                redEffectHandler?.Fire();
                break;        
            }
            case "FireBlue":
            {
                blueEffectHandler?.Fire();
                break;        
            }
        }

        beathandler.RemoveFrontBeat();
        lastFireTimeSeconds = Time.time;
    }

    private bool IsOffCooldown()
    {
        return Time.time >= lastFireTimeSeconds + fireCooldownSeconds;
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

    public void FireProjectile(Projectile projectilePrefab, NoteEffectHandler noteEffectHandler = null, GameObject owner = null)
    {
        Vector3 projectileSpawnPosition = GetProjectileSpawnPosition();
        Vector2 finalDirection = GetFinalFireDirection(projectileSpawnPosition);

        // Shooter asks the manager for a projectile of its assigned prefab
        ProjectilePoolManager manager = ProjectilePoolManager.GetOrCreate();
        Projectile projectile = manager.Get(projectilePrefab);

        if (projectile == null) 
        {
            return;
        }

        if (owner != null)
        {
            projectile.SetOwner(owner);
        }
        projectile.Activate(projectileSpawnPosition, finalDirection, noteEffectHandler);
    }


    // adds lock with key to attackLocks
    public void AddAttackLock(string key)
    {
        attackLocks.Add(key);
    }

    // removes lock with key from attackLocks
    public void RemoveAttackLock(string key)
    {
        attackLocks.Remove(key);
    }
}
