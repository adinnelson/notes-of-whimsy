using UnityEngine;

public class FrogEnemy : EnemyBase
{
    [Header("Frog AoE")]
    [SerializeField] private FrogAOE aoePrefab;

    [SerializeField] private float aoeRadius = 1.5f;
    [SerializeField] private float aoeDamage = 10.0f;

    [SerializeField] private float predictionTime = 0.35f;
    //Scatter is to make them place slightly differently so all the aoe aren't just stacked on each other.
    [SerializeField] private float aoeScatterRadius = 1.5f;

    private ProjectilePoolManager poolManager;
    private Rigidbody2D targetRb;

    private FrogAOE pendingAoe = null;

    private int beatIndex = 0;

    protected override void Start()
    {
        base.Start();

        poolManager = ProjectilePoolManager.GetOrCreate();

        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody2D>();
        }
    }

    // Allows the frog enemy to attack whenever its beat logic triggers.
    protected override bool CanStartAttack()
    {
        return true;
    }

    // Handles the frog's rhythm attack pattern on every beat.
    protected override void OnBeat()
    {
        if (target == null)
        {
            return;
        }

        if (state == State.Dead)
        {
            return;
        }

        if (stunEffects.Count > 0)
        {
            return;
        }

        beatIndex++;

        if (beatIndex > 4)
        {
            beatIndex = 1;
        }

        // Explode on beat 1 and 3
        if (beatIndex == 1 || beatIndex == 3)
        {
            ExplodePending();
        }

        // Telegraph on beat 2 and 4
        if (beatIndex == 2 || beatIndex == 4)
        {
            SpawnTelegraph();
        }
    }

    // Spawns the AoE telegraph aimed slightly ahead of the player's movement.
    private void SpawnTelegraph()
    {
        if (aoePrefab == null || poolManager == null)
        {
            return;
        }

        Vector2 targetPos = target.position;

        Vector2 targetVelocity = Vector2.zero;
        if (targetRb != null)
        {
            targetVelocity = targetRb.linearVelocity; 
        }

        Vector2 predicted = targetPos + (targetVelocity * predictionTime);
        Vector2 randomOffset = Random.insideUnitCircle * aoeScatterRadius;
        Vector2 finalPos = predicted + randomOffset;

        if (pendingAoe != null)
        {
            pendingAoe.CancelAndReturn();
            pendingAoe = null;
        }

        pendingAoe = poolManager.Get(aoePrefab);
        if (pendingAoe == null)
        {
            return;
        }

        pendingAoe.ActivateTelegraphOnly(finalPos, aoeRadius);
    }

    // Triggers the stored AoE explosion.
    private void ExplodePending()
    {
        if (pendingAoe == null)
        {
            return;
        }

        pendingAoe.ExplodeNow(aoeDamage);
        pendingAoe = null;
    }

    // Cleans up any active AoE when the frog enemy dies.
    protected override void HandleDeath()
    {
        if (pendingAoe != null)
        {
            pendingAoe.CancelAndReturn();
            pendingAoe = null;
        }

        base.HandleDeath();
    }
}
