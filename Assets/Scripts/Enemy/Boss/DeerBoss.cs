using UnityEngine;
using System.Collections;

/// <summary>
/// Deer boss controller. Subscribes to OnOddBeatTriggered and runs a beat sequencer
/// that steps through 8-beat attack sequences. All timing is beat-driven — increasing
/// BPM between phases automatically speeds up every attack.
///
/// Does NOT extend EnemyBase; the base class state machine is too simple for
/// multi-beat boss sequences.
///
/// Required components on this GameObject:
///   - Rigidbody2D (Continuous collision detection recommended)
///   - Health
///   - Collider2D
///   - SpriteRenderer
///
/// Required children:
///   - TelegraphVisual: child with sprite pointing RIGHT (+X), disabled by default
///   - ChargeHitbox: child with trigger Collider2D + DeerBossChargeHitbox
///   - SlamHitbox: child with trigger BoxCollider2D + DeerBossSlamHitbox + SpriteRenderer
///   - BeamOrigin: empty child Transform positioned between the deer's horns
///   - BeamChargeVisual (optional): particle/glow child at horn position, disabled by default
/// </summary>
public class DeerBoss : MonoBehaviour
{
    private enum BossAttack { Charge, Beam, Shockwave }
    private enum BossPhase { First, Second, Third }

    [Header("Phase BPM")]
    [SerializeField] private float phase1BPM = 90f;
    [SerializeField] private float phase2BPM = 100f;
    [SerializeField] private float phase3BPM = 120f;

    [Header("Charge Attack")]
    [Tooltip("Speed of each charge burst.")]
    [SerializeField] private float chargeSpeed = 25f;
    [Tooltip("Duration of each charge burst in seconds. Short = snappy lunge.")]
    [SerializeField] private float chargeDuration = 0.12f;
    [Tooltip("Max random angular offset (degrees) on each charge direction.")]
    [SerializeField] private float chargeJitterDegrees = 15f;
    [SerializeField] private float chargeDamage = 10f;
    [Tooltip("Child GameObject shown during telegraph. Sprite must point RIGHT (+X).")]
    [SerializeField] private GameObject telegraphVisual;
    [Tooltip("Child hitbox toggled on during charge bursts (same pattern as BoarHitbox).")]
    [SerializeField] private DeerBossChargeHitBox chargeHitbox;

    [Header("Slam (end of Charge)")]
    [Tooltip("Child hitbox repositioned and toggled for the slam AoE.")]
    [SerializeField] private DeerBossSlamHitBox slamHitbox;
    [Tooltip("Size of the slam rectangle (width x height in world units).")]
    [SerializeField] private Vector2 slamSize = new Vector2(4f, 2f);
    [SerializeField] private float slamDamage = 15f;
    [Tooltip("How far in front of the boss the slam hitbox center is placed.")]
    [SerializeField] private float slamOffset = 2f;

    [Header("Beam Attack")]
    [Tooltip("Prefab with beam_logic + BossBeamAttack component. Same visual as purple spell.")]
    [SerializeField] private GameObject beamPrefab;
    [Tooltip("Empty Transform between the deer's horns — beam fires from here.")]
    [SerializeField] private Transform beamOrigin;
    [Tooltip("Optional charge-up glow/particle object at horn position.")]
    [SerializeField] private GameObject beamChargeVisual;
    [SerializeField] private float beamDamagePerSecond = 20f;

    [Header("Shockwave Attack")]
    [Tooltip("Prefab with CircleCollider2D (trigger) + SpriteRenderer (ring) + BossShockWaveAttack.")]
    [SerializeField] private GameObject shockwavePrefab;
    [Tooltip("Transform at the center of the arena.")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float shockwaveDamage = 12f;

    [Header("Death")]
    [SerializeField] private float deathFadeDuration = 1.0f;

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
    private float chargeTimer = 0f;

    // Slam direction (locked on telegraph)
    private Vector2 slamDirection;

    // Phase threshold tracking
    private bool phase2Triggered = false;
    private bool phase3Triggered = false;

    // ───────── Lifecycle ─────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
        else
            Debug.LogError($"{name}: No Player found in scene.");

        health.OnDeath += HandleDeath;

        // Initialize child hitboxes
        if (chargeHitbox != null)
            chargeHitbox.Initialize(chargeDamage);
        if (slamHitbox != null)
            slamHitbox.Initialize(slamDamage);

        // Player walks through boss by default, collision enabled only during charges
        SetPlayerCollisionEnabled(false);
    }

    /// <summary>
    /// Toggles whether the boss body physically blocks the player.
    /// Enabled during charges so the boss carries the player; disabled otherwise.
    /// Same pattern as BoarEnemy.SetPlayerCollisionEnabled.
    /// </summary>
    private void SetPlayerCollisionEnabled(bool enabled)
    {
        if (target == null || col == null) return;

        Collider2D playerCol = target.GetComponent<Collider2D>();
        if (playerCol != null)
            Physics2D.IgnoreCollision(col, playerCol, !enabled);
    }

    private void Start()
    {
        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
        beatHandler = GameObject.Find("BeatBar")?.GetComponent<BeatHandler>();

        gameManager.OnOddBeatTriggered += OnBeat;

        beatHandler.SetBPM(phase1BPM);

        currentAttack = PickNextAttack();
        sequenceBeat = 0;
    }

    private void FixedUpdate()
    {
        // Charge burst auto-stops after chargeDuration
        if (isCharging)
        {
            chargeTimer -= Time.fixedDeltaTime;
            if (chargeTimer <= 0f)
            {
                isCharging = false;
                rb.linearVelocity = Vector2.zero;
                if (chargeHitbox != null) chargeHitbox.SetEnabled(false);
                SetPlayerCollisionEnabled(false);
            }
        }
    }

    // ───────── Beat sequencer ─────────

    private void OnBeat()
    {
        if (isDead || target == null || inPhaseTransition) return;

        // Check for phase transition before executing the beat
        if (CheckPhaseTransition()) return;

        switch (currentAttack)
        {
            case BossAttack.Charge: ExecuteChargeBeat(); break;
            case BossAttack.Beam: ExecuteBeamBeat(); break;
            case BossAttack.Shockwave: ExecuteShockwaveBeat(); break;
        }

        sequenceBeat++;
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
            mustCharge = true;

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
                TelegraphCharge();
                break;
            case 2:
                DoCharge();
                TelegraphCharge();
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
                if (slamHitbox != null) slamHitbox.Deactivate();
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
        if (chargeHitbox != null) chargeHitbox.SetEnabled(false);
        SetPlayerCollisionEnabled(false);

        // Lock direction toward player + jitter
        Vector2 toTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(toTarget.y, toTarget.x);
        float jitter = Random.Range(-chargeJitterDegrees, chargeJitterDegrees) * Mathf.Deg2Rad;
        float finalAngle = baseAngle + jitter;
        lockedChargeDir = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));

        ShowTelegraph(lockedChargeDir);
    }

    private void DoCharge()
    {
        HideTelegraph();

        isCharging = true;
        chargeTimer = chargeDuration;
        rb.linearVelocity = lockedChargeDir * chargeSpeed;

        SetPlayerCollisionEnabled(true);
        if (chargeHitbox != null) chargeHitbox.SetEnabled(true);
    }

    private void TelegraphSlam()
    {
        rb.linearVelocity = Vector2.zero;
        isCharging = false;
        if (chargeHitbox != null) chargeHitbox.SetEnabled(false);
        SetPlayerCollisionEnabled(false);

        slamDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;

        // Position and show the slam warning visual (collider stays off until DoSlam)
        if (slamHitbox != null)
            slamHitbox.Telegraph(slamDirection, slamSize, slamOffset);

        ShowTelegraph(slamDirection);
    }

    private void DoSlam()
    {
        HideTelegraph();

        if (slamHitbox != null)
            slamHitbox.Activate();
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
                if (beamChargeVisual != null)
                    beamChargeVisual.SetActive(true);
                break;

            case 1:
            case 2:
            case 3:
                // Charge-up continues — visual already active
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
        if (beamChargeVisual != null)
            beamChargeVisual.SetActive(false);

        if (beamPrefab == null || beamOrigin == null) return;

        // Direction locked at the moment of firing
        Vector2 dir = ((Vector2)target.position - (Vector2)beamOrigin.position).normalized;

        // Beam lasts roughly 1 beat — duration scales with BPM automatically
        float secondsPerBeat = 60f / beatHandler.GetBPM();

        GameObject beam = Instantiate(beamPrefab, beamOrigin.position, Quaternion.identity);
        BossBeamAttack bossBeam = beam.GetComponent<BossBeamAttack>();
        if (bossBeam != null)
            bossBeam.Initialize(beamOrigin, dir, beamDamagePerSecond, secondsPerBeat);
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

        if (arenaCenter == null) return;

        Vector2 toCenter = ((Vector2)arenaCenter.position - (Vector2)transform.position).normalized;
        ShowTelegraph(toCenter);
    }

    private void ExecuteCenterCharge()
    {
        HideTelegraph();

        if (arenaCenter == null) return;

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

        if (shockwavePrefab == null) return;

        GameObject sw = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
        BossShockWaveAttack shockwave = sw.GetComponent<BossShockWaveAttack>();
        if (shockwave != null)
            shockwave.Initialize(shockwaveDamage, gameManager);
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
        if (chargeHitbox != null) chargeHitbox.SetEnabled(false);
        if (slamHitbox != null) slamHitbox.Deactivate();
        if (beamChargeVisual != null) beamChargeVisual.SetActive(false);

        // Pause game
        Time.timeScale = 0f;

        // ── TODO: Hook into your inventory UI here ──
        // Show inventory, randomize unlocked beat / spell order multiple times
        // over a few seconds, pause on final combo, then close inventory.
        yield return new WaitForSecondsRealtime(3f);

        // Resume game
        Time.timeScale = 1f;

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
        if (telegraphVisual == null) return;

        float degrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        telegraphVisual.transform.rotation = Quaternion.Euler(0f, 0f, degrees);
        telegraphVisual.SetActive(true);
    }

    private void HideTelegraph()
    {
        if (telegraphVisual != null)
            telegraphVisual.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════
    //  DEATH
    // ═══════════════════════════════════════════════════════════════

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        CancelInvoke();
        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;
        isCharging = false;
        if (col != null) col.enabled = false;

        HideTelegraph();
        if (chargeHitbox != null) chargeHitbox.SetEnabled(false);
        if (slamHitbox != null) slamHitbox.Deactivate();
        if (beamChargeVisual != null) beamChargeVisual.SetActive(false);

        StartCoroutine(FadeOutAndDisable());
    }

    private IEnumerator FadeOutAndDisable()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Color[] startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            startColors[i] = renderers[i].color;

        float elapsed = 0f;
        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / deathFadeDuration);
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
        if (health != null) health.OnDeath -= HandleDeath;
        if (gameManager != null) gameManager.OnOddBeatTriggered -= OnBeat;
    }
}
