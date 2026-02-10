using UnityEngine;

// VERY BASIC DUMMY ENEMY IMPLEMENTATION FOR TESTING PURPOSES
public class DummyEnemy : EnemyBase
{
    [Header("Debug")]
    [SerializeField] private bool logStateChanges = true;

    protected override bool CanStartAttack()
    {
        return IsReadyAndInRange();
    }

    protected override void OnAttackStart()
    {
        //if (logStateChanges) Debug.Log("[DummyEnemy] Attack START");
        // For a dummy: do nothing else
    }

    protected override void OnAttackTick(float dt)
    {
        // For a dummy: do nothing each frame
    }

    protected override void OnAttackEnd()
    {
        //if (logStateChanges) Debug.Log("[DummyEnemy] Attack END");
    }

    protected override void OnTelegraphStart()
    {
        //if (logStateChanges) Debug.Log("[DummyEnemy] Telegraph START");
    }

    protected override void OnRecoverStart()
    {
        //if (logStateChanges) Debug.Log("[DummyEnemy] Recover START");
    }

    protected override void OnHurtStart()
    {
        //if (logStateChanges) Debug.Log("[DummyEnemy] Hurt START");
    }
}
