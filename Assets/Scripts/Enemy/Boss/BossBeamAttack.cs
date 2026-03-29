using UnityEngine;

/// <summary>
/// Boss variant of LaserBeam. Fires in a fixed direction from the deer's horns
/// toward the player's position at the moment of firing. Does not track the mouse.
/// Stops at the nearest wall, same as the player's purple spell beam.
///
/// Prefab setup:
///   - Same prefab structure as the purple spell beam (needs beam_logic component)
///   - Add this component alongside or instead of LaserBeam
///   - Assign playerMask to the layer your Player is on
/// </summary>
public class BossBeamAttack : MonoBehaviour
{
    [Header("Range")]
    [SerializeField] private float maxDistance = 50f;
    [SerializeField] private string wallTag = "Walls";

    [Header("Damage")]
    [SerializeField] private float hitRadius = 0.3f;
    [Tooltip("Layer mask for the Player — beam damages anything on this layer.")]
    [SerializeField] private LayerMask playerMask;

    private beam_logic beam;
    private Transform origin;
    private Vector2 fireDirection;
    private float damagePerSecond;
    private float lifeTimer;
    private bool active = false;

    /// <summary>
    /// Called by DeerBoss immediately after instantiation.
    /// </summary>
    public void Initialize(Transform originTransform, Vector2 direction, float dps, float duration)
    {
        origin = originTransform;
        fireDirection = direction.normalized;
        damagePerSecond = dps;
        lifeTimer = duration;
    }

    private void Start()
    {
        beam = GetComponent<beam_logic>();
        if (beam == null)
        {
            Debug.LogError("BossBeamAttack: No beam_logic component found on prefab.", this);
            Destroy(gameObject);
            return;
        }

        beam.StartBeam();
        active = true;
    }

    private void Update()
    {
        if (!active || origin == null) return;

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            beam.EndBeam();
            beam.DestroyBeam();
            active = false;
            return;
        }

        // Keep beam anchored to horn position (boss might still be drifting)
        transform.position = origin.position;

        // Raycast to find nearest wall
        Vector2 start = (Vector2)origin.position;
        float closestDist = maxDistance;

        RaycastHit2D[] hits = Physics2D.RaycastAll(start, fireDirection, maxDistance);
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.CompareTag(wallTag) && hit.distance < closestDist)
                closestDist = hit.distance;
        }

        Vector2 endpoint = start + fireDirection * closestDist;

        // beam_logic expects a local-space vector for its visuals
        Vector2 localVector = transform.InverseTransformPoint(endpoint);
        beam.SetVector(localVector);

        ApplyDamage(start, endpoint);
    }

    private void ApplyDamage(Vector2 start, Vector2 end)
    {
        Vector2 dir = end - start;
        float dist = dir.magnitude;
        if (dist <= 0.001f) return;
        dir /= dist;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(start, hitRadius, dir, dist, playerMask);
        float dmg = damagePerSecond * Time.deltaTime;

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(dmg);
        }
    }
}
