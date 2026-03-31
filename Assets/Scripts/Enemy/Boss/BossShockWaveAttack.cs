using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Expanding boss shockwave ring. Each regular beat gives the ring a fresh push outward,
/// then the motion slows down until the next beat lands.
///
/// Damage is applied on the moving ring edge rather than throughout the filled circle.
/// </summary>
public class BossShockWaveAttack : MonoBehaviour
{
    [Header("Expansion")]
    [Tooltip("How many beat-pulses the ring receives before it fades out.")]
    [SerializeField] private int totalPulses = 8;
    [Tooltip("Scale velocity added each beat. Higher = stronger outward kick.")]
    [SerializeField] private float scaleImpulsePerBeat = 3.6f;
    [Tooltip("How quickly the ring loses speed between beats.")]
    [SerializeField] private float expansionDrag = 5.0f;
    [Tooltip("Starting scale when the ring first appears.")]
    [SerializeField] private float initialScale = 0.65f;
    [Tooltip("After the final pulse, wait until the ring settles before destroying it.")]
    [SerializeField] private float destroyVelocityThreshold = 0.05f;

    [Header("Ring")]
    [Tooltip("Thickness of the damaging ring band in world units.")]
    [SerializeField] private float damageBandThickness = 0.55f;
    [Tooltip("Constant visual width of the shockwave outline in world units.")]
    [SerializeField] private float visualRingWidth = 0.35f;
    [SerializeField] private Color ringColor = new Color(0.98f, 0.91f, 0.0f, 0.95f);
    [SerializeField] private int sortingOrder = 3;
    [SerializeField] private int circleSegments = 96;

    private float damage;
    private GameManager gameManager;
    private CircleCollider2D circleCollider;
    private SpriteRenderer spriteRenderer;
    private LineRenderer lineRenderer;
    private float baseColliderRadius;

    private int pulsesRemaining;
    private float currentScale;
    private float scaleVelocity;
    private bool isFinishing;
    private readonly HashSet<IDamageable> hitThisPulse = new HashSet<IDamageable>();

    public bool IsFinished => isFinishing && Mathf.Abs(scaleVelocity) <= destroyVelocityThreshold;

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColliderRadius = circleCollider != null ? circleCollider.radius : 0.5f;
        ConfigureRingVisual();
    }

    /// Called by DeerBoss immediately after instantiation.
    public void Initialize(float dmg, GameManager gm)
    {
        damage = dmg;
        gameManager = gm;
        pulsesRemaining = Mathf.Max(1, totalPulses);
        currentScale = Mathf.Max(0.01f, initialScale);
        ApplyCurrentRadius();

        if (gameManager != null)
        {
            gameManager.OnBeatTriggered += OnBeat;
        }

        PushExpansion();
        pulsesRemaining--;

        if (pulsesRemaining <= 0)
        {
            isFinishing = true;

            if (gameManager != null)
            {
                gameManager.OnBeatTriggered -= OnBeat;
            }
        }
    }

    public void ConfigureExpansion(int pulseCount, float beatImpulse)
    {
        totalPulses = Mathf.Max(1, pulseCount);
        scaleImpulsePerBeat = Mathf.Max(0.01f, beatImpulse);
    }

    private void Update()
    {
        if (Mathf.Abs(scaleVelocity) > 0.0001f)
        {
            currentScale += scaleVelocity * Time.deltaTime;
            currentScale = Mathf.Max(currentScale, 0.01f);
            scaleVelocity = Mathf.MoveTowards(scaleVelocity, 0.0f, expansionDrag * Time.deltaTime);
            ApplyCurrentRadius();
        }
        else if (isFinishing)
        {
            Destroy(gameObject);
        }
    }

    private void OnBeat()
    {
        if (pulsesRemaining <= 0)
        {
            return;
        }

        PushExpansion();
        pulsesRemaining--;

        if (pulsesRemaining <= 0)
        {
            isFinishing = true;

            if (gameManager != null)
            {
                gameManager.OnBeatTriggered -= OnBeat;
            }
        }
    }

    private void PushExpansion()
    {
        scaleVelocity += scaleImpulsePerBeat;
        hitThisPulse.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (Mathf.Abs(scaleVelocity) <= 0.01f)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerAttack playerAttack = other.GetComponentInParent<PlayerAttack>();
        if (playerAttack != null && playerAttack.IsDashInvulnerable)
        {
            return;
        }

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null || hitThisPulse.Contains(damageable))
        {
            return;
        }

        float outerRadius = GetOuterRadius();
        float innerRadius = Mathf.Max(0f, outerRadius - damageBandThickness);
        float playerDistance = Vector2.Distance(transform.position, other.transform.position);

        if (playerDistance < innerRadius || playerDistance > outerRadius)
        {
            return;
        }

        damageable.TakeDamage(damage);
        hitThisPulse.Add(damageable);
    }

    private float GetOuterRadius()
    {
        return circleCollider != null ? circleCollider.radius : baseColliderRadius;
    }

    private void ApplyCurrentRadius()
    {
        float outerRadius = Mathf.Max(baseColliderRadius * currentScale, 0.01f);

        if (circleCollider != null)
        {
            circleCollider.radius = outerRadius;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (lineRenderer != null)
        {
            UpdateLineRenderer(outerRadius);
        }
    }

    private void ConfigureRingVisual()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = Mathf.Max(12, circleSegments);
        lineRenderer.widthMultiplier = visualRingWidth;
        lineRenderer.numCapVertices = 8;
        lineRenderer.numCornerVertices = 8;
        lineRenderer.sortingOrder = sortingOrder;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.alignment = LineAlignment.TransformZ;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(ringColor, 0.0f),
                new GradientColorKey(ringColor, 1.0f)
            },
            new[]
            {
                new GradientAlphaKey(ringColor.a, 0.0f),
                new GradientAlphaKey(ringColor.a, 1.0f)
            });
        lineRenderer.colorGradient = gradient;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        }
        if (shader != null)
        {
            lineRenderer.material = new Material(shader);
        }
    }

    private void UpdateLineRenderer(float outerRadius)
    {
        if (lineRenderer == null)
        {
            return;
        }

        int segments = lineRenderer.positionCount;
        for (int i = 0; i < segments; i++)
        {
            float angle = (Mathf.PI * 2.0f * i) / segments;
            Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f) * outerRadius;
            lineRenderer.SetPosition(i, point);
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnBeatTriggered -= OnBeat;
        }

        if (lineRenderer != null && lineRenderer.material != null)
        {
            Destroy(lineRenderer.material);
        }
    }
}
