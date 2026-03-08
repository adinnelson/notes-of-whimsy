using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePoolManager : MonoBehaviour
{
    public static ProjectilePoolManager Instance { get; private set; }

    [SerializeField] private int defaultPoolSize = 30;
    [SerializeField] private int maxPoolSize = 100;

    private readonly Dictionary<Projectile, ObjectPool<Projectile>> pools = new();
    private readonly Dictionary<FrogAOE, ObjectPool<FrogAOE>> frogAoePools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // temporarily disabling for demo
        //DontDestroyOnLoad(gameObject);
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

    // Creates a new ObjectPool for a FrogAOE prefab.
    private ObjectPool<FrogAOE> CreatePool(FrogAOE prefab)
    {
        return new ObjectPool<FrogAOE>(
            createFunc: () =>
            {
                // Instantiates a new FrogAOE and assigns pool references.
                FrogAOE aoe = Instantiate(prefab);
                aoe.SetOwningPrefab(prefab);
                aoe.SetPoolManager(this);
                return aoe;
            },
            // Enables the object when it is retrieved from the pool.
            actionOnGet: aoe => aoe.gameObject.SetActive(true),
            // Disables the object when it is returned to the pool.
            actionOnRelease: aoe => aoe.gameObject.SetActive(false),
            // Destroys the object if the pool exceeds its maximum size.
            actionOnDestroy: aoe => Destroy(aoe.gameObject),
            collectionCheck: true,
            defaultCapacity: defaultPoolSize,
            maxSize: maxPoolSize
        );
    }

    // Retrieves a FrogAOE instance from the pool for the given prefab.
    public FrogAOE Get(FrogAOE prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("ProjectilePoolManager.Get called with null FrogAoe prefab.");
            return null;
        }

        // Creates a new pool for this prefab if one does not exist yet.
        if (!frogAoePools.TryGetValue(prefab, out ObjectPool<FrogAOE> pool))
        {
            pool = CreatePool(prefab);
            frogAoePools.Add(prefab, pool);

            // Pre-populates the pool with inactive FrogAOE objects.
            for (int i = 0; i < defaultPoolSize; i++)
            {
                FrogAOE aoe = pool.Get();
                pool.Release(aoe);
            }
        }

        return pool.Get();
    }

    // Returns a FrogAOE instance back to its pool.
    public void Return(FrogAOE prefabKey, FrogAOE instance)
    {
        if (prefabKey == null || instance == null)
        {
            return;
        }

        if (frogAoePools.TryGetValue(prefabKey, out ObjectPool<FrogAOE> pool))
        {
            // Releases the object back into its pool.
            pool.Release(instance);
        }
        else
        {
            // If the pool no longer exists, just disable the object.
            instance.gameObject.SetActive(false);
        }
    }
}
