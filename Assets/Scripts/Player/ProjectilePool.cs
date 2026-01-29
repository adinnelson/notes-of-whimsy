using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePool : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int defaultPoolSize = 30;
    [SerializeField] private int maxPoolSize = 100;

    private ObjectPool<Projectile> pool;

    private void Awake()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("ProjectilePool: projectilePrefab not assigned.", this);
            return;
        }

        pool = new ObjectPool<Projectile>(
            createFunc: CreateNew,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyProjectile,
            collectionCheck: true,
            defaultCapacity: defaultPoolSize,
            maxSize: maxPoolSize
        );

        // Pre-instantiate projectiles
        for (int i = 0; i < defaultPoolSize; i++)
        {
            Projectile newProjectile = pool.Get();
            pool.Release(newProjectile);
        }
    }

    private Projectile CreateNew()
    {
        Projectile newProjectile = Instantiate(projectilePrefab);
        newProjectile.SetPool(this);
        return newProjectile;
    }

    // Get a projectile from the queue
    public void OnGet(Projectile newProjectile)
    {
        newProjectile.gameObject.SetActive(true);
    }

    private void OnRelease(Projectile newProjectile)
    {
        newProjectile.gameObject.SetActive(false);
    }

    private void OnDestroyProjectile(Projectile newProjectile)
    {
        Destroy(newProjectile.gameObject);
    }

    public Projectile Get()
    {
        return pool.Get();
    }

    // Return a projectile back to the queue
    public void Return(Projectile returningProjectile)
    {
        pool.Release(returningProjectile);
    }
}
