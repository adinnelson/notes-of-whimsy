using UnityEngine;

/// <summary>
/// Boss beam attack that fires until it hits a wall.
/// </summary>
public class BossBeamAttack : MonoBehaviour
{
    [Header("Range")]
    [SerializeField] private float maxDistance = 50.0f;
    [SerializeField] private string wallTag = "Walls";

    [Header("Damage")]
    [SerializeField] private float hitRadius = 0.3f;
    [Tooltip("Layer mask for the Player — beam damages anything on this layer.")]
    [SerializeField] private LayerMask playerMask;

    private beam_logic beam;
    private SpriteRenderer beamSprite;

    private Transform origin;
    private Vector2 fireDirection;
    private float damagePerSecond;
    private float lifeTimer;
    private bool active = false;

    private void Awake()
    {
        beam_logic candidate = GetComponent<beam_logic>();
        if (candidate == null)
        {
            return;
        }

        if (!HasValidBeamLogicHierarchy())
        {
            candidate.enabled = false;
            Debug.LogWarning(
                "[BossBeamAttack] beam_logic exists on this prefab, but its required child hierarchy is incomplete. " +
                "Falling back to SpriteRenderer visual instead.",
                this);
        }
    }

    /// <summary>
    /// Sets beam data after spawn.
    /// </summary>
    public void Initialize(Transform originTransform, Vector2 direction, float dps, float duration)
    {
        origin        = originTransform;
        fireDirection = direction.normalized;
        damagePerSecond = dps;
        lifeTimer     = duration;
    }

    private void Start()
    {
        beam = GetComponent<beam_logic>();

        if (beam != null && beam.enabled)
        {
            beam.StartBeam();
            Debug.Log("[BossBeamAttack] Using beam_logic visual.", this);
        }
        else
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex,
                                          new Rect(0, 0, 1, 1),
                                          new Vector2(0.0f, 0.5f),
                                          pixelsPerUnit: 1.0f);

            beamSprite = gameObject.AddComponent<SpriteRenderer>();
            beamSprite.sprite       = sprite;
            beamSprite.color        = Color.red;
            beamSprite.sortingOrder = 999;   // render on top of everything
            Debug.Log("[BossBeamAttack] Using SpriteRenderer fallback visual.", this);
        }

        active = true;
    }

    private void Update()
    {
        if (!active || origin == null)
        {
            return;
        }

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0.0f)
        {
            StopBeam();
            return;
        }

        Vector2 start       = (Vector2)origin.position;
        float   closestDist = maxDistance;

        foreach (var hit in Physics2D.RaycastAll(start, fireDirection, maxDistance))
        {
            if (hit.collider != null && hit.collider.CompareTag(wallTag) && hit.distance < closestDist)
            {
                closestDist = hit.distance;
            }
        }

        Vector2 endpoint = start + fireDirection * closestDist;

        if (beam != null)
        {
            transform.position = (Vector2)origin.position + fireDirection * 1.0f;
            beam.SetVector(transform.InverseTransformPoint(endpoint));
        }
        else
        {
            if (beamSprite != null)
            {
                float   length = closestDist;
                float   angle  = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
                transform.position    = new Vector3(start.x, start.y, 0.0f);
                transform.rotation    = Quaternion.Euler(0.0f, 0.0f, angle);
                transform.localScale  = new Vector3(length, 0.2f, 1.0f);
            }
        }

        Debug.DrawLine(start, endpoint, Color.red);

        ApplyDamage(start, endpoint);
    }

    private void StopBeam()
    {
        active = false;

        if (beam != null && beam.enabled)
        {
            beam.EndBeam();
            beam.DestroyBeam();
        }
        else
        {
            if (beamSprite != null)
            {
                beamSprite.enabled = false;
                Destroy(gameObject, 0.05f);
            }
        }
    }

    private void ApplyDamage(Vector2 start, Vector2 end)
    {
        Vector2 dir  = end - start;
        float   dist = dir.magnitude;
        if (dist <= 0.001f)
        {
            return;
        }
        dir /= dist;

        float dmg = damagePerSecond * Time.deltaTime;

        foreach (var hit in Physics2D.CircleCastAll(start, hitRadius, dir, dist, playerMask))
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (!hit.collider.CompareTag("Player"))
            {
                continue;
            }

            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            damageable?.TakeDamage(dmg);
        }
    }

    private bool HasValidBeamLogicHierarchy()
    {
        Transform beamVisual = transform.Find("BeamVisual");
        Transform musicNoteEmitter = transform.Find("MusicNoteParticles");
        Transform impactParticleEmitter = transform.Find("ImpactParticles");
        Transform lingeringParticleEmitter = transform.Find("LingeringParticles");

        if (beamVisual == null ||
            musicNoteEmitter == null ||
            impactParticleEmitter == null ||
            lingeringParticleEmitter == null)
        {
            return false;
        }

        if (beamVisual.GetComponent<SpriteRenderer>() == null)
        {
            return false;
        }

        if (musicNoteEmitter.GetComponent<ParticleSystem>() == null)
        {
            return false;
        }

        return true;
    }
}
