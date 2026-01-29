using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 12.0f;
    [SerializeField] private float lifetimeSeconds = 2.0f;

    private const float MIN_DIRECTION_SQR = 0.0001f;

    private new Rigidbody2D rigidbody;
    private Vector2 direction;

    private ProjectilePool pool;
    private float despawnTime;
    private bool isActive;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
    }

    // Pool sets this once when it instantiates the projectile
    public void SetPool(ProjectilePool projectilePool)
    {
        pool = projectilePool;
    }

    // set's spawn position and travel direction, activates the projectile
    public void Activate(Vector3 spawnPosition, Vector2 travelDirection)
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

    private void Despawn()
    {
        // Reset anything that could “leak” into the next reuse
        rigidbody.linearVelocity = Vector2.zero;
        isActive = false;

        if (pool != null)
        {
            pool.Return(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // Safety: if disabled by something else, stop motion
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector2.zero;
        }

        isActive = false;
    }
}
