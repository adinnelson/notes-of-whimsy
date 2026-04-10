using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine.Pool;

public class PlayerAttack : MonoBehaviour
{


    // Reference to FMOD Events (attack and dashing)
    public EventReference attackEvent;
    public EventReference dashEvent;

    public const string MISSED_ATTACK_LOCK_KEY = "MISSED_ATTACK";

    [SerializeField] private BeatHandler beathandler;
    private PlayerActiveSpellsHandler playerActiveSpellsHandler;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject attackBeamPrefab;
    [SerializeField] private float fireCooldownSeconds = 0.2f;
    [SerializeField] private float failedInputLockoutTime = 0.15f;

    [SerializeField] Sprite redSpellBeam0;
    [SerializeField] Sprite redSpellBeam1;
    [SerializeField] Sprite blueSpellBeam0;
    [SerializeField] Sprite blueSpellBeam1;
    [SerializeField] Sprite yellowSpellBeam0;
    [SerializeField] Sprite yellowSpellBeam1;

    private LayerMask ignoredLayersMask;
    private const float MIN_AIM_DEADZONE_SQR = 0.0001f;
    private const float MIN_STICK_DEADZONE_SQR = 0.09f;
    private const float MOUSE_MOVE_DETECT_SQR = 0.1f;
    private float maxDistance = 50.0f;
    private InputSystem_Actions inputActions;
    private Camera mainCamera;
    private GameManager gm;
    private string wallTag = "Walls";

    private float lastFireTimeSeconds;

    private AimSource activeAimSource = AimSource.Mouse;

    private Vector2 lastAimDirection = Vector2.right;
    private Vector2 lastMousePos;

    private float lockoutTimer = 0.0f;
    [SerializeField] private float dashInvulnerabilityDuration = 0.2f;
    private float dashInvulnerableUntil = 0.0f;

    //Animator
    private Animator animator;

    // hashset of attack locks
    private HashSet<string> attackLocks = new HashSet<string>();

    private enum AimSource
    {
        Mouse,
        Gamepad
    }

    public bool IsDashInvulnerable => Time.time <= dashInvulnerableUntil;

    private ObjectPool<GenericBeam> beamSectionPool;

    private void Awake()
    {
        mainCamera = Camera.main;
        inputActions = new InputSystem_Actions();
        CacheInitialMousePosition();
        animator = GetComponent<Animator>();

        beamSectionPool = new ObjectPool<GenericBeam>(
            createFunc: CreateItem,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyItem,
            collectionCheck: true,   // helps catch double-release mistakes
            defaultCapacity: 10,
            maxSize: 50
        );
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

        GenericBeam[] beamSections = Object.FindObjectsOfType<GenericBeam>(true);

        for(int i = 0;i < beamSections.Length;i++)
        {
            Destroy(beamSections[i].gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerActiveSpellsHandler = FindObjectOfType<PlayerActiveSpellsHandler>();
        gm = GameObject.FindWithTag("GameManager")?.GetComponent<GameManager>();

        ignoredLayersMask = LayerMask.GetMask("Pickupables", "Player", "Default");
    }

    private void FixedUpdate()
    {
        lockoutTimer -= Time.fixedDeltaTime;
    }

    private void Update()
    {
        // Continuously check to update aim based on the most recent input (Mouse and/or Controller)
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (beathandler == null)
        {
            beathandler = GameObject.Find("BeatBar")?.GetComponent<BeatHandler>();
        }

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

        if (lockoutTimer > 0)
        {
            lockoutTimer = failedInputLockoutTime;
            return;
        }

        if (!IsOffCooldown())
        {
            return;
        }

        if (beathandler != null && beathandler.ValidDashInterval && context.action.name == "Sprint")
        {
            animator.SetTrigger("Dash");
            dashInvulnerableUntil = Time.time + dashInvulnerabilityDuration;
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            ApplyDashingSoundEffect();
            animator.SetTrigger("Dash");

            if(rb.linearVelocity.magnitude > 0)
            {

                rb.AddForce(rb.linearVelocity.normalized * 3000);
            }
            else
            {
                Vector3 mouseScreenPosition = Mouse.current.position.value;
                Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
                Vector2 direction = new Vector2(mouseWorldPosition.x - transform.position.x, mouseWorldPosition.y - transform.position.y);

                rb.AddForce(direction.normalized * 3000);   
            }
            return;
        }

        // if you fire off beat lock attacks until next beat
        if (beathandler != null && !beathandler.ValidAttackInterval)
        {
            //AddAttackLock(MISSED_ATTACK_LOCK_KEY);
            lockoutTimer = failedInputLockoutTime;
            return;
        }

        switch (context.action.name)
        {
            case "Fire":
                {
                    if (beathandler != null)
                    {
                        int beatID = beathandler.GetBeatIndex();
                        playerActiveSpellsHandler.GetSpellEffectHandlerFromSlotId(beatID)?.Fire();
                        //apply sound effect to the spell that been used 
                        ApplySpellSoundEffect(beatID);

                        lastFireTimeSeconds = Time.time;
                    }
                    break;
                }
                /*case "Sprint":
                {

                    Rigidbody2D rb = GetComponent<Rigidbody2D>();

                    if(rb.linearVelocity.magnitude > 0)
                    {
                        rb.AddForce(rb.linearVelocity.normalized * 3000);
                        break;
                    }
                    Vector3 mouseScreenPosition = Mouse.current.position.value;
                    Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
                    Vector2 direction = new Vector2(mouseWorldPosition.x - transform.position.x, mouseWorldPosition.y - transform.position.y);

                    rb.AddForce(direction.normalized * 3000);
                    break;
                }*/
        }
    }

    //Method to apply the dashing sound effect 
    private void ApplyDashingSoundEffect()
    {
            //Dashing sound effect instance 
            var dashingInstance = RuntimeManager.CreateInstance(dashEvent);

            dashingInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
            dashingInstance.start();
            dashingInstance.release();
    }

    //Method to apply sound effect reflecting the spell in a beat in use
    private void ApplySpellSoundEffect(int beatID)
    {
        var spellHandler = playerActiveSpellsHandler.GetSpellEffectHandlerFromSlotId(beatID);
        // Debug.Log($"Spell Handler: {spellHandler?.GetType().Name}, Spell ID: {spellHandler?.SpellData.spellId}");

        if (spellHandler != null)
        {
            string spellLabel = GetSpellLabel(spellHandler.SpellData.spellId); // Id will match FMOD naming convention 
            if (!string.IsNullOrEmpty(spellLabel))
            {
                Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                mouseWorldPosition.z = 0;
                //created to get the Event of the sound effect 
                //Sound effect is dependend on the projectile position 
                var spellInstance = RuntimeManager.CreateInstance(attackEvent);
                spellInstance.set3DAttributes(RuntimeUtils.To3DAttributes(mouseWorldPosition));
                //matching the spell name as a parameter in fmod
                // fire it one time! 
                spellInstance.setParameterByNameWithLabel("spell", spellLabel);
                spellInstance.start();
                spellInstance.release();
            }
        }

    }
    //Helper to match the cases of spells with sound effect name in FMOD
    private string GetSpellLabel(int spellId)
    {
        switch (spellId)
        {
            case 1: return "explosion";
            case 2: return "lightning";
            case 3: return "laser";
            case 4: return "whirlpool";
            default: return null;
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
        Vector2 pos = start;//(start + end) / 2f;

        GenericBeam attackBeam = beamSectionPool.Get();
        attackBeam.transform.position = pos;
        attackBeam.transform.rotation = Quaternion.identity;

        Vector2 d = end - start;

        SetSpriteOfBeamSection(attackBeam, spellId, false);

        GenericBeam prevBeamSection = attackBeam;
        
        for (int i = 1; i < d.magnitude * 2; i++)
        {
            // Calculate interpolation factor (0.0 to 1.0)
            float t = (float)i / (d.magnitude - 1);
            
            Vector3 spawnPos = (Vector3)start + new Vector3(0.49f * i, 0, 0);

            GenericBeam currentBeamSection = beamSectionPool.Get();
            currentBeamSection.transform.position = spawnPos;
            currentBeamSection.transform.rotation = Quaternion.identity;

            SetSpriteOfBeamSection(currentBeamSection, spellId, i % 2 != 0);

            currentBeamSection.transform.SetParent(prevBeamSection.transform);

            prevBeamSection = currentBeamSection;
        }

        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        attackBeam.transform.rotation = Quaternion.Euler(0, 0, angle);

        return attackBeam.gameObject;
    }

    private void SetSpriteOfBeamSection(GenericBeam attackBeam, int? spellId, bool alternate)
    {
        SpriteRenderer spriteRenderer = attackBeam.GetComponent<SpriteRenderer>();

        switch (spellId)
        {
            case 1:
            {
                if (alternate)
                {
                    attackBeam.Init(redSpellBeam1, redSpellBeam0);
                    break;
                }
                
                attackBeam.Init(redSpellBeam0, redSpellBeam1);
                break;
            }
            case 2:
            {
                if (alternate)
                {
                    attackBeam.Init(yellowSpellBeam1, yellowSpellBeam0);
                    break;
                }
                
                attackBeam.Init(yellowSpellBeam0, yellowSpellBeam1);
                break;
                break;
            }
            case 3: 
            {
                spriteRenderer.color = Color.purple;
                break;
            }
            case 4:
            {
                if (alternate)
                {
                    attackBeam.Init(blueSpellBeam1, blueSpellBeam0);
                    break;
                }
                
                attackBeam.Init(blueSpellBeam0, blueSpellBeam1);
                break;
            }
        }
    }

    private void ReleaseBeam(GameObject startBeam)
    {
        Transform[] transforms = startBeam.transform.GetComponentsInChildren<Transform>();

        foreach (Transform child in transforms)
        {
            if (child == startBeam.transform) continue; 

            child.SetParent(null);
        }

        foreach (Transform child in transforms)
        {
            beamSectionPool.Release(child.gameObject.GetComponent<GenericBeam>());
        }
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

        Vector2 spawnPoint = (Vector2)projectileSpawnPosition + 0.25f * finalDirection;

        RaycastHit2D hit = Physics2D.Raycast(spawnPoint, finalDirection, 100, ~ignoredLayersMask);

        Vector3 endBeamPos = spawnPoint + finalDirection * 10;

        IDamageable damageable = hit.collider?.gameObject?.GetComponent<IDamageable>();

        if (hit.collider?.gameObject != null)
        {
            endBeamPos = hit.collider.gameObject.transform.position;

            print(hit.collider.gameObject.name);
        }
        else
        {
            RaycastHit2D[] allHits = Physics2D.RaycastAll(spawnPoint, finalDirection, maxDistance);
            Vector2 endpoint = spawnPoint + finalDirection * maxDistance;
            float closestDist = maxDistance;
            foreach (var wallhit in allHits)
            {
                if (wallhit.collider != null && wallhit.collider.CompareTag(wallTag) && wallhit.distance < closestDist)
                {
                    closestDist = wallhit.distance;
                    endBeamPos = wallhit.point;
                }
            }
        }

        endBeamPos = new Vector3(endBeamPos.x, endBeamPos.y, -GameManager.Z_RANGE);

        if(damageable != null)
        {
            noteEffectHandler?.HitEnemy(damageable);
        }

        if (noteEffectHandler != null)
        {
            switch (noteEffectHandler.SpellData.spellId)
            {
                case 1:
                    ((RedNoteEffectHandler)noteEffectHandler).Explode(endBeamPos);
                    break;
                case 4:
                    ((BlueNoteEffectHandler)noteEffectHandler).WhirlPool(endBeamPos);
                    break;
            }
        }

        GameObject attackBeamInstance = SpawnAttackBeam(spawnPoint, endBeamPos, noteEffectHandler?.SpellData.spellId);
        SimpleTimer timer = new SimpleTimer();
        timer.StartTimer(0.35f, onFinish: () => ReleaseBeam(attackBeamInstance) ,gameManager: gm);
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

    // Creates a new pooled GameObject the first time (and whenever the pool needs more).
    private GenericBeam CreateItem()
    {
        GenericBeam genericBeam = Instantiate(attackBeamPrefab, transform.position, Quaternion.identity).GetComponent<GenericBeam>();
        genericBeam.gameObject.SetActive(false);
        DontDestroyOnLoad(genericBeam);

        return genericBeam;
    }

    // Called when an item is taken from the pool.
    private void OnGet(GenericBeam genericBeam)
    {
        genericBeam.gameObject.SetActive(true);
    }

    // Called when an item is returned to the pool.
    private void OnRelease(GenericBeam genericBeam)
    {
        genericBeam.Disable();
        genericBeam.transform.SetParent(null);
        genericBeam.gameObject.SetActive(false);
    }

    // Called when the pool decides to destroy an item (e.g., above max size).
    private void OnDestroyItem(GenericBeam genericBeam)
    {
        Destroy(genericBeam);
    }
}
