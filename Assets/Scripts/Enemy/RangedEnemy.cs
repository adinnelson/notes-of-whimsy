using UnityEngine;

public class RangedEnemy : EnemyBase
{
    [Header("Ranged Attack")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float spreadDegrees = 12.0f;

    protected override void Start()
    {
        base.Start();
    }

    protected override void OnChase()
    {
        StopMovement();

        if (CanStartAttack())
        {
            EnterState(State.Telegraph);
        }
    }

    protected override bool CanStartAttack()
    {
        return TargetInAttackRange();
    }

    protected override void OnAttack()
    {
        FireTripleShot();
    }

    private Vector2 GetTargetPosition()
    {
        if (target != null)
        {
            return (Vector2)target.position;
        }

        return (Vector2)transform.position;
    }

    private Vector2 GetFirePosition()
    {
        if (firePoint != null)
        {
            return (Vector2)firePoint.position;
        }

        return (Vector2)transform.position;
    }

    private void FireTripleShot()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("[RangedEnemy] Missing projectilePrefab.", this);
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("[RangedEnemy] Target is null. Ensure EnemyBase is finding the Player tag or assign target.", this);
            return;
        }

        Vector2 firePos = GetFirePosition();
        Vector2 targetPos = GetTargetPosition();

        Vector2 baseDir = targetPos - firePos;
        if (baseDir.sqrMagnitude <= 0.0001f)
        {
            baseDir = Vector2.right;
        }

        baseDir.Normalize();

        Vector2 dirCenter = baseDir;
        Vector2 dirLeft = (Quaternion.AngleAxis(+spreadDegrees, Vector3.forward) * baseDir).normalized;
        Vector2 dirRight = (Quaternion.AngleAxis(-spreadDegrees, Vector3.forward) * baseDir).normalized;

        SpawnProjectile(firePos, dirCenter);
        SpawnProjectile(firePos, dirLeft);
        SpawnProjectile(firePos, dirRight);
    }

    private void SpawnProjectile(Vector2 pos, Vector2 dir)
    {
        ProjectilePoolManager pool = ProjectilePoolManager.GetOrCreate();
        Projectile p = pool.Get(projectilePrefab);

        if (p == null)
        {
            return;
        }

        p.SetOwner(gameObject);
        p.Activate(pos, dir);
    }
}
