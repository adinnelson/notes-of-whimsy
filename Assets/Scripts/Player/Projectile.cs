using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 12.0f;
    [SerializeField] private float lifetimeSeconds = 2.0f;

    private new Rigidbody2D rigidbody;
    private Vector2 direction;
    private bool isInitialized;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Destroy the projectile after its lifetime expires
        Destroy(gameObject, lifetimeSeconds);
    }

    public void Initialize(Vector2 travelDirection)
    {
        direction = travelDirection.normalized;
        isInitialized = true;
    }

    private void FixedUpdate()
    {
        if (!isInitialized)
        {
            return;
        }

        // Move the projectile in the initialized direction at a constant speed
        rigidbody.linearVelocity = direction * speed;
    }
}
