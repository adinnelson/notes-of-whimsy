using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePoolManager : MonoBehaviour
{
    public static ProjectilePoolManager Instance { get; private set; }

    [SerializeField] private int defaultPoolSize = 30;
    [SerializeField] private int maxPoolSize = 100;

    private readonly Dictionary<Projectile, ObjectPool<Projectile>> pools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Creates a new ObjectPool for a specific projectile prefab
    private ObjectPool<Projectile> CreatePool(Projectile prefab)
    {
        return new ObjectPool<Projectile>(
            createFunc: () =>
            {
                Projectile p = Instantiate(prefab);
                p.SetOwningPrefab(prefab);
                p.SetPoolManager(this);
                return p;
            },
            actionOnGet: p => p.gameObject.SetActive(true),
            actionOnRelease: p => p.gameObject.SetActive(false),
            actionOnDestroy: p => Destroy(p.gameObject),
            collectionCheck: true,
            defaultCapacity: defaultPoolSize,
            maxSize: maxPoolSize
        );
    }

    // Ensures the manager exists, creating one at runtime if needed
    public static ProjectilePoolManager GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject go = new GameObject("ProjectilePoolManager");
        return go.AddComponent<ProjectilePoolManager>();
    }

    // Returns an active projectile instance for the given prefab
    public Projectile Get(Projectile prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("ProjectilePoolManager.Get called with null prefab.");
            return null;
        }

        // If no pool exists for this prefab yet, create one
        if (!pools.TryGetValue(prefab, out ObjectPool<Projectile> pool))
        {
            pool = CreatePool(prefab);
            pools.Add(prefab, pool);

            // Prepopulate pool
            for (int i = 0; i < defaultPoolSize; i++)
            {
                Projectile p = pool.Get();
                pool.Release(p);
            }
        }

        return pool.Get();
    }

    // Called by projectiles when they are finished (hit or expired)
    public void Return(Projectile prefabKey, Projectile instance)
    {
        if (prefabKey == null || instance == null) return;

        if (pools.TryGetValue(prefabKey, out ObjectPool<Projectile> pool))
        {
            pool.Release(instance);
        }
        else
        {
            // If pool doesn't exist anymore, just disable
            instance.gameObject.SetActive(false);
        }
    }
}
