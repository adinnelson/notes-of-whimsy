using UnityEngine;

/// Safety net that prevents the player from being pushed through walls
/// by stacked boar charges (or any other physics force).
/// Attach to the Player GameObject alongside PlayerMovement.
///
/// How it works:
///   Every FixedUpdate, after physics resolves, it checks whether the
///   player's collider overlaps any "Walls"-tagged collider. If so, it
///   nudges the player back toward their last known safe position until
///   the overlap is cleared.
///
/// Also enforces Continuous collision detection on the player Rigidbody2D
/// so Unity's solver catches fast-moving pushes before they tunnel.

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerWallContainment : MonoBehaviour
{
    [Tooltip("How quickly the player is pushed back to safety when clipped into a wall.")]
    [SerializeField] private float correctionSpeed = 50f;

    [Tooltip("LayerMask for wall colliders. Set this to whatever layer your walls are on.")]
    [SerializeField] private LayerMask wallLayers;

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 lastSafePosition;

    private ContactFilter2D wallFilter;
    private readonly Collider2D[] overlapResults = new Collider2D[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Enforce continuous collision detection to reduce tunneling
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        lastSafePosition = rb.position;

        wallFilter = new ContactFilter2D();
        wallFilter.SetLayerMask(wallLayers);
        wallFilter.useLayerMask = true;
        wallFilter.useTriggers = false; // only solid wall colliders
    }

    private void FixedUpdate()
    {
        if (IsOverlappingWall())
        {
            // Snap velocity to zero so boars can't keep pushing
            rb.linearVelocity = Vector2.zero;

            // Lerp back toward the last safe position
            Vector2 corrected = Vector2.MoveTowards(
                rb.position,
                lastSafePosition,
                correctionSpeed * Time.fixedDeltaTime
            );
            rb.MovePosition(corrected);
        }
        else
        {
            // Only update safe position when we're fully clear of walls
            lastSafePosition = rb.position;
        }
    }

    private bool IsOverlappingWall()
    {
        // Physics2D.OverlapCollider checks against the actual collider shape
        int count = Physics2D.OverlapCollider(col, wallFilter, overlapResults);
        return count > 0;
    }
}
