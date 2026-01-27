using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int poolSize = 30;

    private readonly Queue<Projectile> pool = new Queue<Projectile>();

    private void Awake()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("ProjectilePool: projectilePrefab not assigned.", this);
            return;
        }

        // Pre-create a bunch of projectiles.
        for (int i = 0; i < poolSize; i++)
        {
            Projectile newProjectile = CreateNew();
            Return(newProjectile);
        }
    }

    private Projectile CreateNew()
    {
        Projectile newProjectile = Instantiate(projectilePrefab);
        newProjectile.SetPool(this);
        return newProjectile;
    }

    // Get a projectile from the queue
    public Projectile Get()
    {
        if (pool.Count == 0)
        {
            // If we run out, expand the pool so firing never fails.
            Debug.LogWarning("ProjectilePool expanded beyond initial size. Consider increasing the pool size");
            Projectile newProjectile = CreateNew();
            return newProjectile;
        }

        Projectile pooledProjectile = pool.Dequeue();
        pooledProjectile.gameObject.SetActive(true);
        return pooledProjectile;
    }

    // Return a projectile back to the queue
    public void Return(Projectile returningProjectile)
    {
        returningProjectile.gameObject.SetActive(false);
        pool.Enqueue(returningProjectile);
    }
}
