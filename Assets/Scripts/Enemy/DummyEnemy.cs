using UnityEngine;

// VERY BASIC DUMMY ENEMY IMPLEMENTATION FOR TESTING PURPOSES
public class DummyEnemy : EnemyBase
{
    [Header("Debug")]
    [SerializeField] private bool logStateChanges = true;

    protected override bool CanStartAttack()
    {
        return TargetInAttackRange();
    }

    protected override void OnAttack()
    {
        //if (logStateChanges) Debug.Log("[DummyEnemy] Attack START");
        // For a dummy: do nothing else
    }

    protected override void OnTelegraph(float bpm)
    {
        //if (logStateChanges) Debug.Log("[DummyEnemy] Telegraph START");
    }

    protected override void OnRecover()
    {
        //if (logStateChanges) Debug.Log("[DummyEnemy] Recover START");
    }

    protected override void OnHurt()
    {
        //if (logStateChanges) Debug.Log("[DummyEnemy] Hurt START");
    }
}
