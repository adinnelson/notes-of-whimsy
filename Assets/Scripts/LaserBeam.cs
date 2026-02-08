using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LaserBeam : MonoBehaviour
{
    private beam_logic beam;
    private Camera cam;
    private Transform startTransform;
    private BeatHandler beatHandler;

    [Header("Lifetime (beats)")]
    [SerializeField] private float activeBeats = 2.0f;

    [Header("Damage")]
    [SerializeField] private float damagePerSecond = 20.0f;
    [SerializeField] private float hitRadius = 0.2f;
    [SerializeField] private LayerMask enemyMask;

    private float lifeTimer;
    private readonly HashSet<IDamageable> uniqueHits = new HashSet<IDamageable>();

    public void Init(Transform start, BeatHandler handler)
    {
        startTransform = start;
        beatHandler = handler;
    }

    private void Start()
    {
        cam = Camera.main;
        beam = GetComponent<beam_logic>();

        if (beam == null)
        {
            Debug.LogError("LaserBeam: beam_logic missing on prefab.", this);
            enabled = false;
            return;
        }

        if (beatHandler == null)
        {
            Debug.LogError("LaserBeam: BeatHandler was not provided.", this);
            enabled = false;
            return;
        }

        beam.StartBeam();
        lifeTimer = beatHandler.SecondsPerBeat * activeBeats; // lasts 2 beats
    }

    private void Update()
    {
        if (startTransform == null || cam == null || Mouse.current == null)
        {
            return;
        }

        // lifetime countdown
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0.0f)
        {
            StopBeam();
            return;
        }

        // TO DO: currently anchors laser object on player (change to position of bard weapaon when that exists)
        transform.position = startTransform.position;

        // mouse world
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 worldMouse = cam.ScreenToWorldPoint(mouseScreen);
        worldMouse.z = startTransform.position.z;

        // local vector for beam_logic visuals
        Vector2 localVector = transform.InverseTransformPoint(worldMouse);
        beam.SetVector(localVector);

        // damage along beam
        ApplyBeamDamage((Vector2)startTransform.position, (Vector2)worldMouse);
    }

    private void ApplyBeamDamage(Vector2 start, Vector2 end)
    {
        Vector2 dir = end - start;
        float dist = dir.magnitude;
        if (dist <= 0.001f)
        {
            return;
        }

        // turns dir into a unit vector
        dir /= dist;

        uniqueHits.Clear();

        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            start,
            hitRadius,
            dir,
            dist,
            enemyMask
        );

        float dmgThisFrame = damagePerSecond * Time.deltaTime;

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(dmgThisFrame);
            }
            else
            {
                Debug.Log($"Laser hit {hit.collider.name} but no IDamageable on it or its parents");
            }
        }
    }

    private void StopBeam()
    {
        beam.EndBeam();
        beam.DestroyBeam();
        enabled = false;
    }
}
