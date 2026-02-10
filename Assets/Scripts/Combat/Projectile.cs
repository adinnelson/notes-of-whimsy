using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 12.0f;
    [SerializeField] private float lifetimeSeconds = 2.0f;
    [SerializeField] private float damage = 1.0f;
    private ProjectilePoolManager poolManager;
    private Projectile owningPrefab;
    private const float MIN_DIRECTION_SQR = 0.0001f;
    private new Rigidbody2D rigidbody;
    private Vector2 direction;
    private float despawnTime;
    protected bool isActive;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
    }

    public void SetPoolManager(ProjectilePoolManager manager) => poolManager = manager;
    public void SetOwningPrefab(Projectile prefab) => owningPrefab = prefab;

    // set's spawn position and travel direction, activates the projectile
    public virtual void Activate(Vector3 spawnPosition, Vector2 travelDirection, NoteEffectHandler noteEffectHandler = null)
    {
        transform.position = spawnPosition;

        if (travelDirection.sqrMagnitude < MIN_DIRECTION_SQR)
        {
            travelDirection = Vector2.right;
        }

        direction = travelDirection.normalized;

        despawnTime = Time.time + lifetimeSeconds;
        isActive = true;
    }

    protected void Despawn()
    {
        rigidbody.linearVelocity = Vector2.zero;
        isActive = false;

        if (poolManager != null && owningPrefab != null)
        {
            poolManager.Return(owningPrefab, this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        if (!isActive)
        {
            return;
        }

        rigidbody.linearVelocity = direction * speed;

        // If it's passed despawn time, return to pool
        if (Time.time >= despawnTime)
        {
            Despawn();
        }
    }

    private void OnDisable()
    {
        // if disabled by something else, stop motion
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector2.zero;
        }

        isActive = false;
    }

    public virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) 
        {
            return;
        }

        // Don't hit yourself
        if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == gameObject)
        {
            return;
        }

        // If the thing we hit can take damage, damage it
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage);
            Despawn(); // return projectile to pool after a successful hit
        }
    }
}

