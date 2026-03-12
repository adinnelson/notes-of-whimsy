using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
    public const string MISSED_ATTACK_LOCK_KEY = "MISSED_ATTACK";

    [SerializeField] private BeatHandler beathandler;
    private PlayerActiveSpellsHandler playerActiveSpellsHandler;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject attackBeamPrefab;
    [SerializeField] private float fireCooldownSeconds = 0.2f;
    private LayerMask ignoredLayersMask;
    private const float MIN_AIM_DEADZONE_SQR = 0.0001f;
    private const float MIN_STICK_DEADZONE_SQR = 0.09f;
    private const float MOUSE_MOVE_DETECT_SQR = 0.1f;
    private InputSystem_Actions inputActions;
    private Camera mainCamera;
    private GameManager gm;

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
        inputActions.Player.Fire.performed += OnBeatActionPerformed;
        inputActions.Player.Sprint.performed += OnBeatActionPerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.Sprint.performed -= OnBeatActionPerformed;
        inputActions.Player.Fire.performed -= OnBeatActionPerformed;
        inputActions.Player.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerActiveSpellsHandler = FindObjectOfType<PlayerActiveSpellsHandler>();
        gm = GameObject.FindWithTag("GameManager")?.GetComponent<GameManager>();

        ignoredLayersMask = LayerMask.GetMask("Pickupables", "Player", "Default");
    }

    private void Update()
    {
        // Continuously check to update aim based on the most recent input (Mouse and/or Controller)
        UpdateAimSourceFromStick();
        UpdateAimSourceFromMouseMovement();
    }

    private void OnBeatActionPerformed(InputAction.CallbackContext context)
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

        switch(context.action.name)
        {
            case "Fire":
            {
                int beatID = beathandler.GetBeatIndex();

                playerActiveSpellsHandler.GetSpellEffectHandlerFromSlotId(beatID)?.Fire();

                lastFireTimeSeconds = Time.time;
                break;
            }
            case "Sprint":
            {

                Rigidbody2D rb = GetComponent<Rigidbody2D>();

                if(rb.linearVelocity.magnitude > 0)
                {
                    rb.AddForce(rb.linearVelocity.normalized * 2500);
                    break;
                }
                Vector3 mouseScreenPosition = Mouse.current.position.value;
                Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
                Vector2 direction = new Vector2(mouseWorldPosition.x - transform.position.x, mouseWorldPosition.y - transform.position.y);

                rb.AddForce(direction * 2500);
                break;
            }
        }
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

    private GameObject SpawnAttackBeam(Vector2 start, Vector2 end, int? spellId)
    {
        Vector2 pos = (start + end) / 2f;

        GameObject attackBeam = Instantiate(attackBeamPrefab, pos, Quaternion.identity);

        Vector2 d = end - start;

        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        attackBeam.transform.rotation = Quaternion.Euler(0, 0, angle);

        attackBeam.transform.localScale = new Vector3(d.magnitude, 0.2f, 1f);
        SpriteRenderer spriteRenderer = attackBeam.GetComponent<SpriteRenderer>();


        switch(spellId)
        {
            case 1:
            {
                spriteRenderer.color = Color.red;
                break;
            }
            case 2:
            {
                spriteRenderer.color = Color.yellow;
                break;
            }
            case 3:
            {
                spriteRenderer.color = Color.purple;
                break;
            }
            case 4:
            {
                spriteRenderer.color = Color.blue;
                break;
            }
        }

        return attackBeam;
    }

    public void FireProjectile(Projectile projectilePrefab, NoteEffectHandler noteEffectHandler = null, GameObject owner = null)
    {
        Vector3 projectileSpawnPosition = GetProjectileSpawnPosition();
        Vector2 finalDirection = GetFinalFireDirection(projectileSpawnPosition);

        // Shooter asks the manager for a projectile of its assigned prefab
        /*ProjectilePoolManager manager = ProjectilePoolManager.GetOrCreate();
        Projectile projectile = manager.Get(projectilePrefab);

        if (projectile == null)
        {
            return;
        }

        if (owner != null)
        {
            projectile.SetOwner(owner);
        }*/
        //projectile.Activate(projectileSpawnPosition, finalDirection, noteEffectHandler);

        //Fire raycast if we hit anything call noteEffectHandler hit
        // probalbly can draw a line as well

        Vector3 spawnPoint = projectileSpawnPosition + (Vector3)finalDirection;

        RaycastHit2D hit = Physics2D.Raycast(spawnPoint, finalDirection, 100, ~ignoredLayersMask);

        Vector3 endBeamPos;

        IDamageable damageable = hit.collider?.gameObject?.GetComponent<IDamageable>();

        if(hit.collider?.gameObject != null)
        {
            endBeamPos = hit.collider.gameObject.transform.position;
        }
        else
        {
            endBeamPos = spawnPoint + 10 * (Vector3)finalDirection;
        }

        if(damageable != null)
        {
            noteEffectHandler?.HitEnemy(damageable);
        }
        GameObject attackBeamInstance = SpawnAttackBeam(spawnPoint, endBeamPos, noteEffectHandler?.SpellData.spellId);
        SimpleTimer timer = new SimpleTimer();
        timer.StartTimer(0.5f, onFinish: () => Destroy(attackBeamInstance) ,gameManager: gm);
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
