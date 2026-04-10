using UnityEngine;
using FMODUnity;

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
    private bool shouldExplodeNext = false;
    private int explodeBeatCounter = 0;
    private const int BEAT_TO_EXPLODE_AFTER_TELEGRAPH = 2;

    private Animator animator;

    protected override void Start()
    {
        animator = GetComponent<Animator>();
        base.Start();

        poolManager = ProjectilePoolManager.GetOrCreate();

        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody2D>();

            // Frog never charges, so always let the player walk through it
            Collider2D frogCol = GetComponent<Collider2D>();
            Collider2D playerCol = target.GetComponent<Collider2D>();
            if (frogCol != null && playerCol != null)
            {
                Physics2D.IgnoreCollision(frogCol, playerCol);
            }
        }

        int zSub = (int)(gameObject.transform.position.y / GameManager.Z_RANGE);

        gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.y - GameManager.Z_RANGE * zSub);
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
            StunVisuals();
            return;
        }

        switch(explodeBeatCounter)
        {
            case 0:
                SpawnTelegraph();
                break;
            case BEAT_TO_EXPLODE_AFTER_TELEGRAPH:
                ExplodePending();
                break;
        }

        explodeBeatCounter++;
        if(explodeBeatCounter > BEAT_TO_EXPLODE_AFTER_TELEGRAPH)
        {
            explodeBeatCounter = 0;
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
        animator.SetTrigger("Charge");
    }

    // Triggers the stored AoE explosion.
    private void ExplodePending()
    {
        if (pendingAoe == null)
        {
            return;
        }

        
        pendingAoe.ExplodeNow(aoeDamage);
        animator.SetTrigger("Pop");
        attackSoundEffect(); 
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

    protected override void attackSoundEffect()
    {
        if (!attackSound.IsNull)
        {
            //Apply attack sound for the frog
            var dashingInstance = RuntimeManager.CreateInstance(attackSound);

            dashingInstance.set3DAttributes(RuntimeUtils.To3DAttributes(target.position));
            dashingInstance.start();
            dashingInstance.release();
        }
    }
}
