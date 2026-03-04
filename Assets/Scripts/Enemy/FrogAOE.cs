using System.Collections;
using UnityEngine;

public class FrogAOE : MonoBehaviour
{
    [Header("Visual Prefabs")]
    [SerializeField] private GameObject telegraphVisualPrefab;
    [SerializeField] private GameObject explosionVisualPrefab;

    [Tooltip("Seconds to keep the explosion visible before returning to pool.")]
    [SerializeField] private float explosionLifetimeSeconds = 0.35f;

    [Header("Damage")]
    [SerializeField] private LayerMask damageMask;

    private ProjectilePoolManager poolManager;
    private FrogAOE owningPrefab;

    private GameObject telegraphVisualInstance;
    private GameObject explosionVisualInstance;

    private float telegraphBaseDiameter = 1.0f;
    private float explosionBaseDiameter = 1.0f;

    private float radius = 1.5f;

    private bool isActive = false;
    private Coroutine returnRoutine = null;

    // Stores the pool manager so this object can return itself to the pool.
    public void SetPoolManager(ProjectilePoolManager manager)
    {
        poolManager = manager;
    }

    public void SetOwningPrefab(FrogAOE prefab)
    {
        owningPrefab = prefab;
    }

    // Creates the telegraph and explosion visuals when the object is first initialized.
    private void Awake()
    {
        if (telegraphVisualPrefab != null)
        {
            telegraphVisualInstance = Instantiate(telegraphVisualPrefab, transform);
            telegraphVisualInstance.transform.localPosition = Vector3.zero;
            telegraphVisualInstance.transform.localRotation = Quaternion.identity;
            telegraphVisualInstance.transform.localScale = Vector3.one;
            telegraphVisualInstance.SetActive(false);

            telegraphBaseDiameter = MeasureDiameter(telegraphVisualInstance);
        }

        if (explosionVisualPrefab != null)
        {
            explosionVisualInstance = Instantiate(explosionVisualPrefab, transform);
            explosionVisualInstance.transform.localPosition = Vector3.zero;
            explosionVisualInstance.transform.localRotation = Quaternion.identity;
            explosionVisualInstance.transform.localScale = Vector3.one;
            explosionVisualInstance.SetActive(false);

            explosionBaseDiameter = MeasureDiameter(explosionVisualInstance);
        }
    }

    // Resets the AoE visuals and stops timers when the object is disabled.
    private void OnDisable()
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        isActive = false;

        SetTelegraphVisible(false);
        SetExplosionVisible(false);
    }

    // Activates the telegraph visual at a position and prepares the AoE for explosion.
    public void ActivateTelegraphOnly(Vector2 position, float aoeRadius)
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        transform.position = position;
        radius = aoeRadius;

        isActive = true;

        ApplyVisualScale();

        SetExplosionVisible(false);
        SetTelegraphVisible(true);
    }

    // Triggers the explosion, damages the player, and schedules the object to return to the pool.
    public void ExplodeNow(float damage)
    {
        if (!isActive)
        {
            return;
        }

        isActive = false;

        SetTelegraphVisible(false);
        SetExplosionVisible(true);

        Vector2 center = transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, damageMask);

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                break;
            }
        }

        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
        }

        returnRoutine = StartCoroutine(ReturnAfterDelay());
    }

    // Cancels the AoE and immediately returns it to the pool.
    public void CancelAndReturn()
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        isActive = false;

        SetTelegraphVisible(false);
        SetExplosionVisible(false);

        ReturnToPool();
    }

    // Waits for the explosion visual to finish before returning the object to the pool.
    private IEnumerator ReturnAfterDelay()
    {
        float elapsed = 0.0f;

        while (elapsed < explosionLifetimeSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        ReturnToPool();
    }

    // Returns the AoE object back to the projectile pool.
    private void ReturnToPool()
    {
        if (poolManager != null && owningPrefab != null)
        {
            poolManager.Return(owningPrefab, this);
            return;
        }

        gameObject.SetActive(false);
    }

    // Scales the visuals so they match the AoE damage radius.
    private void ApplyVisualScale()
    {
        float diameter = radius * 2.0f;

        if (telegraphVisualInstance != null)
        {
            float scale = diameter / Mathf.Max(0.0001f, telegraphBaseDiameter);
            telegraphVisualInstance.transform.localScale = new Vector3(scale, scale, 1.0f);
        }

        if (explosionVisualInstance != null)
        {
            float scale = diameter / Mathf.Max(0.0001f, explosionBaseDiameter);
            explosionVisualInstance.transform.localScale = new Vector3(scale, scale, 1.0f);
        }
    }

    // Measures the base size of a sprite so scaling works correctly.
    private float MeasureDiameter(GameObject root)
    {
        SpriteRenderer sr = root.GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
        {
            return 1.0f;
        }

        float diameter = sr.bounds.size.x;
        if (diameter <= 0.0001f)
        {
            diameter = 1.0f;
        }

        return diameter;
    }

    // Shows or hides the telegraph visual.
    private void SetTelegraphVisible(bool visible)
    {
        if (telegraphVisualInstance != null)
        {
            telegraphVisualInstance.SetActive(visible);
        }
    }

    // Shows or hides the explosion visual.
    private void SetExplosionVisible(bool visible)
    {
        if (explosionVisualInstance != null)
        {
            explosionVisualInstance.SetActive(visible);
        }
    }
}
