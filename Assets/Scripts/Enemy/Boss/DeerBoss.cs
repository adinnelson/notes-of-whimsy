using UnityEngine;
using System.Collections;

/// Deer boss controller. Subscribes to OnOddBeatTriggered and runs a beat sequencer
/// that steps through 8-beat attack sequences. All timing is beat-driven — increasing
/// BPM between phases automatically speeds up every attack.

public class DeerBoss : MonoBehaviour
{
    private enum BossAttack { Charge, Beam, Shockwave }
    private enum BossPhase { First, Second, Third }

    private const string WALL_TAG = "Walls";
    private const int WALL_OVERLAP_BUFFER_SIZE = 8;
    private const int FALLBACK_BEAM_TEXTURE_SIZE = 64;

    [Header("Phase BPM")]
    // Phase 1 beat speed.
    [SerializeField] private float phase1BPM = 90.0f;
    // Phase 2 beat speed.
    [SerializeField] private float phase2BPM = 100.0f;
    // Phase 3 beat speed.
    [SerializeField] private float phase3BPM = 120.0f;

    [Header("Charge Attack")]
    // Charge move speed.
    [Tooltip("Speed of each charge burst.")]
    [SerializeField] private float chargeSpeed = 25.0f;
    // Charge move time.
    [Tooltip("Duration of each charge burst in seconds. Short = snappy lunge.")]
    [SerializeField] private float chargeDuration = 0.12f;
    // Charge aim randomness.
    [Tooltip("Max random angular offset (degrees) on each charge direction.")]
    [SerializeField] private float chargeJitterDegrees = 15.0f;
    // Charge hit damage.
    [SerializeField] private float chargeDamage = 10.0f;
    // Charge warning visual.
    [Tooltip("Child GameObject shown during telegraph. Sprite must point RIGHT (+X).")]
    [SerializeField] private GameObject telegraphVisual;
    // Charge hitbox child.
    [Tooltip("Child hitbox toggled on during charge bursts (same pattern as BoarHitbox).")]
    [SerializeField] private DeerBossChargeHitBox chargeHitbox;

    [Header("Slam (end of Charge)")]
    // Slam hitbox child.
    [Tooltip("Child hitbox repositioned and toggled for the slam AoE.")]
    [SerializeField] private DeerBossSlamHitBox slamHitbox;
    // Slam box size.
    [Tooltip("Size of the slam rectangle (width x height in world units).")]
    [SerializeField] private Vector2 slamSize = new Vector2(4.0f, 2.0f);
    // Slam damage.
    [SerializeField] private float slamDamage = 15.0f;
    // Slam box offset.
    [Tooltip("How far in front of the boss the slam hitbox center is placed.")]
    [SerializeField] private float slamOffset = 2.0f;

    [Header("Beam Attack")]
    // Beam prefab.
    [Tooltip("Prefab with beam_logic + BossBeamAttack component. Same visual as purple spell.")]
    [SerializeField] private GameObject beamPrefab;
    // Beam start point.
    [Tooltip("Empty Transform between the deer's horns — beam fires from here.")]
    [SerializeField] private Transform beamOrigin;
    // Optional beam charge visual.
    [Tooltip("Optional charge-up glow/particle object at horn position.")]
    [SerializeField] private GameObject beamChargeVisual;
    // Beam damage per second.
    [SerializeField] private float beamDamagePerSecond = 20.0f;
    // Fallback charge color.
    [Tooltip("Fallback charge orb color used when Beam Charge Visual is left empty.")]
    [SerializeField] private Color fallbackBeamChargeColor = new Color(1.0f, 0.25f, 0.1f, 0.9f);
    // Final beam warning color.
    [Tooltip("Charge color on the last warning beat before the beam fires.")]
    [SerializeField] private Color fallbackBeamChargeFireCueColor = new Color(1.0f, 0.95f, 0.35f, 1.0f);
    // Smallest size of the fallback beam charge visual.
    [SerializeField] private float fallbackBeamChargeMinScale = 0.7f;
    // Largest size of the fallback beam charge visual during normal charge.
    [SerializeField] private float fallbackBeamChargeMaxScale = 1.05f;
    // Pulse speed of the fallback beam charge visual.
    [SerializeField] private float fallbackBeamChargePulseSpeed = 7.0f;
    // Size of the fallback beam charge visual on the final warning beat.
    [SerializeField] private float fallbackBeamChargeFireCueScale = 1.25f;

    [Header("Shockwave Attack")]
    // Shockwave prefab.
    [Tooltip("Prefab with CircleCollider2D (trigger) + SpriteRenderer (ring) + BossShockWaveAttack.")]
    [SerializeField] private GameObject shockwavePrefab;
    // Arena center point.
    [Tooltip("Transform at the center of the arena.")]
    [SerializeField] private Transform arenaCenter;
    // Shockwave damage.
    [SerializeField] private float shockwaveDamage = 12.0f;
    // Shockwave pulse count.
    [Tooltip("How many beat-pulses the shockwave gets before it fades.")]
    [SerializeField] private int shockwavePulseCount = 8;
    // Shockwave beat push.
    [Tooltip("How strong each beat-driven outward burst feels.")]
    [SerializeField] private float shockwaveBeatImpulse = 3.6f;

    [Header("Death")]
    // Death fade time.
    [SerializeField] private float deathFadeDuration = 1.0f;

    [Header("Wall Containment")]
    // Wall correction speed.
    [SerializeField] private float wallCorrectionSpeed = 50.0f;

    // ───────── Runtime state ─────────

    private Transform target;
    private Rigidbody2D rb;
    private Health health;
    private Collider2D col;
    private SpriteRenderer sprite;
    private GameManager gameManager;
    private BeatHandler beatHandler;

    private BossAttack currentAttack;
    private BossPhase currentPhase = BossPhase.First;
    private int sequenceBeat = 0;     // 0-7 across two 4-beat bars
    private bool mustCharge = false;  // forced Charge after Beam/Shockwave
    private bool isDead = false;
    private bool inPhaseTransition = false;

    // Charge movement
    private Vector2 lockedChargeDir;
    private bool isCharging = false;
    private float chargeTimer = 0.0f;

    // Slam direction (locked on telegraph)
    private Vector2 slamDirection;

    // Active shockwave, used to keep the boss in end-lag until the ring is finished.
    private BossShockWaveAttack activeShockwave;

    // Phase threshold tracking
    private bool phase2Triggered = false;
    private bool phase3Triggered = false;
    private SpriteRenderer fallbackBeamChargeRenderer;
    private Transform fallbackBeamChargeTransform;
    private Texture2D fallbackBeamChargeTexture;
    private Sprite fallbackBeamChargeSprite;
    private bool beamFireCueActive = false;
    private Vector2 lastSafePosition;
    private readonly Collider2D[] wallOverlapResults = new Collider2D[WALL_OVERLAP_BUFFER_SIZE];

    // ───────── Lifecycle ─────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        lastSafePosition = rb.position;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else
        {
            Debug.LogError($"{name}: No Player found in scene.");
        }

        health.OnDeath += HandleDeath;

        // Initialize child hitboxes
        if (chargeHitbox != null)
        {
            chargeHitbox.Initialize(chargeDamage);
            chargeHitbox.gameObject.layer = gameObject.layer;
        }
        if (slamHitbox != null)
        {
            slamHitbox.Initialize(slamDamage);
            slamHitbox.gameObject.layer = gameObject.layer;
        }

        // Player walks through boss by default, collision enabled only during charges
        SetPlayerCollisionEnabled(false);
    }

    /// Toggles whether the boss body physically blocks the player.
    /// Enabled during charges so the boss carries the player; disabled otherwise.
    /// Same pattern as BoarEnemy.SetPlayerCollisionEnabled.
    private void SetPlayerCollisionEnabled(bool enabled)
    {
        if (target == null || col == null)
        {
            return;
        }

        Collider2D playerCol = target.GetComponent<Collider2D>();
        if (playerCol != null)
        {
            Physics2D.IgnoreCollision(col, playerCol, !enabled);
        }
    }

    private void Start()
    {
        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
        beatHandler = GameObject.Find("BeatBar")?.GetComponent<BeatHandler>();

        EnsureFallbackBeamChargeVisual();

        // Force the boss into a clean idle visual state so the first chosen
        // attack does not inherit any prefab-active telegraphs or warning sprites.
        HideTelegraph();
        if (slamHitbox != null)
        {
            slamHitbox.Deactivate();
        }
        SetBeamChargeVisualActive(false);

        if (beamChargeVisual != null && !beamChargeVisual.scene.IsValid())
        {
            Debug.LogWarning(
                "[DeerBoss] beamChargeVisual is pointing at a prefab asset, not a scene child. " +
                "Assign a child object under the boss or leave this empty.",
                this);
            beamChargeVisual = null;
        }

        if (shockwavePrefab != null && shockwavePrefab.scene.IsValid())
        {
            // The boss prefab currently stores the shockwave as a child object template.
            // Keep the template hidden and only show spawned copies during the attack.
            shockwavePrefab.SetActive(false);
        }

        if (arenaCenter == null)
        {
            Debug.LogWarning(
                "[DeerBoss] arenaCenter is not assigned. Shockwave will not reliably start from the middle of the room.",
                this);
        }

        gameManager.OnOddBeatTriggered += OnBeat;

        beatHandler.SetBPM(phase1BPM);

        currentAttack = PickNextAttack();
        sequenceBeat = 0;
    }

    private void FixedUpdate()
    {
        UpdateWallContainment();

        // Charge burst auto-stops after chargeDuration
        if (isCharging)
        {
            chargeTimer -= Time.fixedDeltaTime;
            if (chargeTimer <= 0.0f)
            {
                isCharging = false;
                rb.linearVelocity = Vector2.zero;
                if (chargeHitbox != null) 
                {
                    chargeHitbox.SetEnabled(false);
                }
                SetPlayerCollisionEnabled(false);
            }
        }

        UpdateFallbackBeamChargeVisual();
    }

    private void UpdateWallContainment()
    {
        if (rb == null || col == null)
        {
            return;
        }

        if (IsOverlappingWall())
        {
            rb.linearVelocity = Vector2.zero;
            Vector2 corrected = Vector2.MoveTowards(
                rb.position,
                lastSafePosition,
                wallCorrectionSpeed * Time.fixedDeltaTime);
            rb.MovePosition(corrected);
        }
        else
        {
            lastSafePosition = rb.position;
        }
    }

    private bool IsOverlappingWall()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = false;
        filter.useTriggers = false;

        int count = col.Overlap(filter, wallOverlapResults);
        for (int i = 0; i < count; i++)
        {
            Collider2D overlap = wallOverlapResults[i];
            if (overlap != null && !overlap.isTrigger && overlap.CompareTag(WALL_TAG))
            {
                return true;
            }
        }
        return false;
    }

    // ───────── Beat sequencer ─────────

    private void OnBeat()
    {
        if (isDead || target == null || inPhaseTransition)
        {
            return;
        }

        if (activeShockwave == null || activeShockwave.IsFinished)
        {
            activeShockwave = null;
        }

        // Check for phase transition before executing the beat
        if (CheckPhaseTransition()) 
        {
            return;
        }

        switch (currentAttack)
        {
            case BossAttack.Charge: ExecuteChargeBeat(); break;
            case BossAttack.Beam: ExecuteBeamBeat(); break;
            case BossAttack.Shockwave: ExecuteShockwaveBeat(); break;
        }

        sequenceBeat++;
        if (currentAttack == BossAttack.Shockwave &&
            sequenceBeat >= 8 &&
            activeShockwave != null)
        {
            sequenceBeat = 8;
            return;
        }

        if (sequenceBeat >= 8)
        {
            sequenceBeat = 0;
            currentAttack = PickNextAttack();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  ATTACK SELECTION
    // ═══════════════════════════════════════════════════════════════

    private BossAttack PickNextAttack()
    {
        if (mustCharge)
        {
            mustCharge = false;
            return BossAttack.Charge;
        }

        int roll = Random.Range(0, 3);
        BossAttack picked = roll switch
        {
            0 => BossAttack.Charge,
            1 => BossAttack.Beam,
            _ => BossAttack.Shockwave
        };

        if (picked != BossAttack.Charge)
        {
            mustCharge = true;
        }

        return picked;
    }

    // ═══════════════════════════════════════════════════════════════
    //  CHARGE  (8 odd-beat sequence)
    // ═══════════════════════════════════════════════════════════════
    //  Beat 0 : Telegraph charge 1
    //  Beat 1 : Execute charge 1  +  telegraph charge 2
    //  Beat 2 : Execute charge 2  +  telegraph charge 3
    //  Beat 3 : Execute charge 3
    //  Beat 4 : Telegraph slam
    //  Beat 5 : Execute slam
    //  Beat 6 : End lag (deactivate slam)
    //  Beat 7 : End lag

    private void ExecuteChargeBeat()
    {
        switch (sequenceBeat)
        {
            case 0:
                TelegraphCharge();
                break;
            case 1:
                DoCharge();
                PrepareNextCharge(); // lock next direction + show visual without cancelling current dash
                break;
            case 2:
                DoCharge();
                PrepareNextCharge();
                break;
            case 3:
                DoCharge();
                HideTelegraph();
                break;
            case 4:
                TelegraphSlam();
                break;
            case 5:
                DoSlam();
                break;
            case 6:
                if (slamHitbox != null)
                {
                     slamHitbox.Deactivate();
                }
                rb.linearVelocity = Vector2.zero;
                break;
            case 7:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    private void TelegraphCharge()
    {
        rb.linearVelocity = Vector2.zero;
        isCharging = false;
        if (chargeHitbox != null)
        {
            chargeHitbox.SetEnabled(false);
        }
        SetPlayerCollisionEnabled(false);

        // Lock direction toward player + jitter
        Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(toTarget.y, toTarget.x);
        float jitter = Random.Range(-chargeJitterDegrees, chargeJitterDegrees) * Mathf.Deg2Rad;
        float finalAngle = baseAngle + jitter;
        lockedChargeDir = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));

        ShowTelegraph(lockedChargeDir);
    }

    /// <summary>
    /// Locks the direction for the NEXT charge and shows the telegraph visual,
    /// but does NOT stop the current charge burst. Used at beats 1 and 2 so
    /// the previous dash can finish while the next one is already being telegraphed.
    /// </summary>
    private void PrepareNextCharge()
    {
        Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(toTarget.y, toTarget.x);
        float jitter = Random.Range(-chargeJitterDegrees, chargeJitterDegrees) * Mathf.Deg2Rad;
        lockedChargeDir = new Vector2(Mathf.Cos(baseAngle + jitter), Mathf.Sin(baseAngle + jitter));
        ShowTelegraph(lockedChargeDir);
    }

    private void DoCharge()
    {
        HideTelegraph();

        isCharging = true;
        chargeTimer = chargeDuration;
        rb.linearVelocity = lockedChargeDir * chargeSpeed;

        SetPlayerCollisionEnabled(true);
        if (chargeHitbox != null)
        {
            chargeHitbox.SetEnabled(true);
        }
    }

    private void TelegraphSlam()
    {
        rb.linearVelocity = Vector2.zero;
        isCharging = false;
        if (chargeHitbox != null)
        {
            chargeHitbox.SetEnabled(false);
        }
        SetPlayerCollisionEnabled(false);

        slamDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;

        // Position and show the slam warning visual (collider stays off until DoSlam)
        if (slamHitbox != null)
        {
            slamHitbox.Telegraph(slamDirection, slamSize, slamOffset);
        }
    }

    private void DoSlam()
    {
        rb.linearVelocity = Vector2.zero;
        isCharging = false;
        chargeTimer = 0.0f;
        SetPlayerCollisionEnabled(false);
        if (chargeHitbox != null)
        {
            chargeHitbox.SetEnabled(false);
        }
        HideTelegraph();

        if (slamHitbox != null)
        {
            slamHitbox.Activate(target);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  BEAM  (8 odd-beat sequence)
    // ═══════════════════════════════════════════════════════════════
    //  Beat 0 : Start charging beam (visual between horns)
    //  Beat 1 : Keep charging
    //  Beat 2 : Keep charging
    //  Beat 3 : Keep charging
    //  Beat 4 : Fire beam toward player
    //  Beat 5 : End lag
    //  Beat 6 : End lag
    //  Beat 7 : End lag

    private void ExecuteBeamBeat()
    {
        switch (sequenceBeat)
        {
            case 0:
                rb.linearVelocity = Vector2.zero;
                SetBeamChargeVisualActive(true);
                SetBeamChargeFireCue(false);
                break;

            case 1:
            case 2:
                // Charge-up continues — visual already active
                break;

            case 3:
                SetBeamChargeFireCue(true);
                break;

            case 4:
                FireBeam();
                break;

            case 5:
            case 6:
            case 7:
                // End lag
                break;
        }
    }

    private void FireBeam()
    {
        SetBeamChargeVisualActive(false);
        SetBeamChargeFireCue(false);

        if (beamPrefab == null)
        {
            Debug.LogError("[DeerBoss] beamPrefab is not assigned — beam cannot fire. Assign it in the Inspector.", this);
            return;
        }
        if (beamOrigin == null)
        {
            Debug.LogError("[DeerBoss] beamOrigin is not assigned — beam cannot fire. Assign the horn Transform in the Inspector.", this);
            return;
        }

        // Direction locked at the moment of firing
        Vector2 dir = ((Vector2)target.position - (Vector2)beamOrigin.position).normalized;

        // Each sequence step spans 2 regular beats (OnOddBeatTriggered fires every other beat)
        float secondsPerSequenceBeat = (60.0f / beatHandler.GetBPM()) * 2.0f;

        GameObject beamObj = Instantiate(beamPrefab, beamOrigin.position, Quaternion.identity);
        BossBeamAttack bossBeam = beamObj.GetComponent<BossBeamAttack>();
        if (bossBeam == null)
        {
            Debug.LogError("[DeerBoss] beamPrefab is missing a BossBeamAttack component — add it to the prefab.", this);
            Destroy(beamObj);
            return;
        }

        Debug.Log($"[DeerBoss] Firing beam toward {target.position}, duration {secondsPerSequenceBeat:F2}s");
        bossBeam.Initialize(beamOrigin, dir, beamDamagePerSecond, secondsPerSequenceBeat);
    }

    // ═══════════════════════════════════════════════════════════════
    //  SHOCKWAVE  (8 odd-beat sequence)
    // ═══════════════════════════════════════════════════════════════
    //  Beat 0 : Telegraph charge to center
    //  Beat 1 : Execute charge to center
    //  Beat 2 : End lag
    //  Beat 3 : Telegraph shockwave slam
    //  Beat 4 : Spawn shockwave ring (starts expanding on subsequent beats)
    //  Beat 5 : End lag (ring expands)
    //  Beat 6 : End lag (ring expands)
    //  Beat 7 : End lag (ring expands)

    private void ExecuteShockwaveBeat()
    {
        switch (sequenceBeat)
        {
            case 0:
                TelegraphCenterCharge();
                break;

            case 1:
                ExecuteCenterCharge();
                break;

            case 2:
                rb.linearVelocity = Vector2.zero;
                isCharging = false;
                SetPlayerCollisionEnabled(false);
                if (arenaCenter != null)
                {
                    transform.position = arenaCenter.position;
                }
                break;

            case 3:
                rb.linearVelocity = Vector2.zero;
                break;

            case 4:
                SpawnShockwave();
                break;

            case 5:
            case 6:
            case 7:
                // End lag — shockwave ring handles its own expansion via beats
                break;
        }
    }

    private void TelegraphCenterCharge()
    {
        rb.linearVelocity = Vector2.zero;
        isCharging = false;

        if (arenaCenter == null)
        {
            return;
        }

        Vector2 toCenter = ((Vector2)arenaCenter.position - (Vector2)transform.position).normalized;
        ShowTelegraph(toCenter);
    }

    private void ExecuteCenterCharge()
    {
        HideTelegraph();

        if (arenaCenter == null)
        {
            return;
        }

        Vector2 toCenter = (Vector2)arenaCenter.position - (Vector2)transform.position;
        float dist = toCenter.magnitude;

        if (dist < 0.5f)
        {
            transform.position = arenaCenter.position;
            return;
        }

        Vector2 dir = toCenter / dist;
        isCharging = true;
        chargeTimer = dist / chargeSpeed;
        rb.linearVelocity = dir * chargeSpeed;
        SetPlayerCollisionEnabled(true);
    }

    private void SpawnShockwave()
    {
        rb.linearVelocity = Vector2.zero;
        isCharging = false;
        SetPlayerCollisionEnabled(false);
        HideTelegraph();

        if (shockwavePrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = transform.position;
        if (arenaCenter != null)
        {
            spawnPosition = arenaCenter.position;
            transform.position = spawnPosition;
        }

        GameObject sw = Instantiate(shockwavePrefab, spawnPosition, Quaternion.identity);
        sw.SetActive(true);
        BossShockWaveAttack shockwave = sw.GetComponent<BossShockWaveAttack>();
        if (shockwave != null)
        {
            shockwave.ConfigureExpansion(shockwavePulseCount, shockwaveBeatImpulse);
            shockwave.Initialize(shockwaveDamage, gameManager);
            activeShockwave = shockwave;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  PHASE TRANSITIONS
    // ═══════════════════════════════════════════════════════════════

    private bool CheckPhaseTransition()
    {
        float ratio = health.CurrentHealth / health.MaxHealth;

        if (!phase2Triggered && ratio <= 0.66f)
        {
            phase2Triggered = true;
            StartCoroutine(PhaseTransitionRoutine(BossPhase.Second, phase2BPM));
            return true;
        }

        if (!phase3Triggered && ratio <= 0.33f)
        {
            phase3Triggered = true;
            StartCoroutine(PhaseTransitionRoutine(BossPhase.Third, phase3BPM));
            return true;
        }

        return false;
    }

    private IEnumerator PhaseTransitionRoutine(BossPhase newPhase, float newBPM)
    {
        inPhaseTransition = true;
        currentPhase = newPhase;

        // Stop boss mid-action
        rb.linearVelocity = Vector2.zero;
        isCharging = false;
        HideTelegraph();
        if (chargeHitbox != null)
        {
            chargeHitbox.SetEnabled(false);
        }
        if (slamHitbox != null)
        {
            slamHitbox.Deactivate();
        }
        SetBeamChargeVisualActive(false);
        SetBeamChargeFireCue(false);
        if (activeShockwave != null)
        {
            Destroy(activeShockwave.gameObject);
        }
        activeShockwave = null;

        // Pause game
        Time.timeScale = 0.0f;

        // TODO: Hook inventory UI here 
        // Show inventory, randomize unlocked beat / spell order multiple times
        // over a few seconds, pause on final combo, then close inventory.
        yield return new WaitForSecondsRealtime(3.0f);

        // Resume game
        Time.timeScale = 1.0f;

        // Apply new BPM — all beat-driven logic automatically speeds up
        beatHandler.SetBPM(newBPM);

        // Reset sequence so next beat starts a fresh attack
        sequenceBeat = 0;
        currentAttack = PickNextAttack();

        inPhaseTransition = false;
    }

    // ═══════════════════════════════════════════════════════════════
    //  TELEGRAPH VISUAL HELPERS
    // ═══════════════════════════════════════════════════════════════

    private void ShowTelegraph(Vector2 direction)
    {
        if (telegraphVisual == null)
        {
            return;
        }

        float degrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        telegraphVisual.transform.rotation = Quaternion.Euler(0.0f, 0.0f, degrees);
        telegraphVisual.SetActive(true);
    }

    private void HideTelegraph()
    {
        if (telegraphVisual != null)
        {
            telegraphVisual.SetActive(false);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  DEATH
    // ═══════════════════════════════════════════════════════════════

    private void HandleDeath()
    {
        if (isDead)
        {
            return;
        }
        isDead = true;

        CancelInvoke();
        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;
        isCharging = false;
        if (col != null)
        {
            col.enabled = false;
        }

        HideTelegraph();
        if (chargeHitbox != null)
        {
            chargeHitbox.SetEnabled(false);
        }
        if (slamHitbox != null)
        {
            slamHitbox.Deactivate();
        }
        SetBeamChargeVisualActive(false);
        SetBeamChargeFireCue(false);

        StartCoroutine(FadeOutAndDisable());
    }

    private IEnumerator FadeOutAndDisable()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Color[] startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            startColors[i] = renderers[i].color;

        float elapsed = 0.0f;
        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1.0f, 0.0f, elapsed / deathFadeDuration);
            for (int i = 0; i < renderers.Length; i++)
            {
                Color c = startColors[i];
                c.a = alpha;
                renderers[i].color = c;
            }
            yield return null;
        }

        gameObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════
    //  CLEANUP
    // ═══════════════════════════════════════════════════════════════

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= HandleDeath;
        }
        if (gameManager != null)
        {
            gameManager.OnOddBeatTriggered -= OnBeat;
        }

        if (fallbackBeamChargeSprite != null)
        {
            Destroy(fallbackBeamChargeSprite);
        }

        if (fallbackBeamChargeTexture != null)
        {
            Destroy(fallbackBeamChargeTexture);
        }
    }

    private void SetBeamChargeVisualActive(bool isActive)
    {
        if (beamChargeVisual != null)
        {
            beamChargeVisual.SetActive(isActive);
            if (!isActive)
            {
                SetBeamChargeVisualTint(Color.white, Vector3.one);
            }
            return;
        }

        if (fallbackBeamChargeRenderer != null)
        {
            fallbackBeamChargeRenderer.enabled = isActive;
            if (!isActive)
            {
                beamFireCueActive = false;
                fallbackBeamChargeRenderer.color = fallbackBeamChargeColor;
                fallbackBeamChargeTransform.localScale = Vector3.one * fallbackBeamChargeMinScale;
            }
        }
    }

    private void SetBeamChargeFireCue(bool isActive)
    {
        beamFireCueActive = isActive;

        if (beamChargeVisual != null)
        {
            SetBeamChargeVisualTint(
                isActive ? fallbackBeamChargeFireCueColor : Color.white,
                isActive ? Vector3.one * fallbackBeamChargeFireCueScale : Vector3.one);
            return;
        }

        if (fallbackBeamChargeRenderer != null)
        {
            fallbackBeamChargeRenderer.color = isActive ? fallbackBeamChargeFireCueColor : fallbackBeamChargeColor;
            fallbackBeamChargeTransform.localScale = Vector3.one *
                (isActive ? fallbackBeamChargeFireCueScale : fallbackBeamChargeMinScale);
        }
    }

    private void SetBeamChargeVisualTint(Color tint, Vector3 scale)
    {
        if (beamChargeVisual == null)
        {
            return;
        }

        SpriteRenderer[] renderers = beamChargeVisual.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].color = tint;
        }

        beamChargeVisual.transform.localScale = scale;
    }

    private void EnsureFallbackBeamChargeVisual()
    {
        if (beamChargeVisual != null || beamOrigin == null || fallbackBeamChargeRenderer != null)
        {
            return;
        }

        GameObject fallbackObj = new GameObject("BeamChargeFallback");
        fallbackObj.transform.SetParent(beamOrigin, false);
        fallbackObj.transform.localPosition = Vector3.zero;
        fallbackObj.transform.localRotation = Quaternion.identity;

        fallbackBeamChargeTransform = fallbackObj.transform;
        fallbackBeamChargeRenderer = fallbackObj.AddComponent<SpriteRenderer>();
        fallbackBeamChargeTexture = BuildFilledCircleTexture(FALLBACK_BEAM_TEXTURE_SIZE, fallbackBeamChargeColor);
        fallbackBeamChargeSprite = Sprite.Create(
            fallbackBeamChargeTexture,
            new Rect(0, 0, fallbackBeamChargeTexture.width, fallbackBeamChargeTexture.height),
            new Vector2(0.5f, 0.5f),
            fallbackBeamChargeTexture.width);

        fallbackBeamChargeRenderer.sprite = fallbackBeamChargeSprite;
        fallbackBeamChargeRenderer.color = Color.white;
        fallbackBeamChargeRenderer.sortingOrder = sprite != null ? sprite.sortingOrder + 1 : 5;
        fallbackBeamChargeRenderer.enabled = false;
        fallbackBeamChargeTransform.localScale = Vector3.one * fallbackBeamChargeMinScale;
    }

    private void UpdateFallbackBeamChargeVisual()
    {
        if (fallbackBeamChargeRenderer == null || !fallbackBeamChargeRenderer.enabled)
        {
            return;
        }

        if (beamFireCueActive)
        {
            float cuePulse = (Mathf.Sin(Time.time * fallbackBeamChargePulseSpeed * 1.8f) + 1.0f) * 0.5f;
            float cueScale = Mathf.Lerp(fallbackBeamChargeMaxScale, fallbackBeamChargeFireCueScale, cuePulse);
            fallbackBeamChargeTransform.localScale = Vector3.one * cueScale;
            fallbackBeamChargeRenderer.color = Color.Lerp(fallbackBeamChargeColor, fallbackBeamChargeFireCueColor, cuePulse);
            return;
        }

        float pulse = (Mathf.Sin(Time.time * fallbackBeamChargePulseSpeed) + 1.0f) * 0.5f;
        float scale = Mathf.Lerp(fallbackBeamChargeMinScale, fallbackBeamChargeMaxScale, pulse);
        fallbackBeamChargeTransform.localScale = Vector3.one * scale;
        fallbackBeamChargeRenderer.color = fallbackBeamChargeColor;
    }

    private static Texture2D BuildFilledCircleTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float half = (size - 1) * 0.5f;
        float radius = half;
        Color clear = new Color(0.0f, 0.0f, 0.0f, 0.0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - half;
                float dy = y - half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                texture.SetPixel(x, y, dist <= radius ? color : clear);
            }
        }

        texture.Apply();
        return texture;
    }
}
