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

    [Header("Range")]
    [SerializeField] private float maxDistance = 50.0f;
    [SerializeField] private string wallTag = "Walls";

    [Header("Damage")]
    [SerializeField] private float damagePerSecond = 20.0f;
    [SerializeField] private float hitRadius = 0.2f;
    [SerializeField] private LayerMask enemyMask;

    private float lifeTimer;
    private bool loggedMissingRefs = false;
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
        lifeTimer = SecondsPerBeat() * activeBeats; // lasts 2 beats
    }

    private void Update()
    {
        if(Time.timeScale == 0)
        {
            return;
        }

        if (startTransform == null || cam == null || Mouse.current == null)
        {
            if (!loggedMissingRefs)
            {
                if (startTransform == null)
                {
                    Debug.LogWarning("LaserBeam: startTransform is NULL.", this);
                }

                if (cam == null)
                {
                    Debug.LogWarning("LaserBeam: Camera.main not found. Is there a camera tagged MainCamera?", this);
                }

                if (Mouse.current == null)
                {
                    Debug.LogWarning("LaserBeam: Mouse.current is NULL. Input System not initialized?", this);
                }
                // Added this cause it's on update so it will likely hit every frame and spam the log, so only log once
                loggedMissingRefs = true;
            }
            return;
        }

        // lifetime countdown
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0.0f)
        {
            StopBeam();
            return;
        }

        

        // mouse world
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 worldMouse = cam.ScreenToWorldPoint(mouseScreen);
        worldMouse.z = startTransform.position.z;

        Vector2 origin = (Vector2)startTransform.position;
        Vector2 dir = ((Vector2)worldMouse - origin).normalized;

        // TO DO: currently anchors laser object on player (change to position of bard weapon when that exists)
        transform.position = startTransform.position + (Vector3)dir * 1f;

        // Raycast in that direction and find the closest wall by tag
        RaycastHit2D[] allHits = Physics2D.RaycastAll(origin, dir, maxDistance);
        Vector2 endpoint = origin + dir * maxDistance;
        float closestDist = maxDistance;
        foreach (var hit in allHits)
        {
            if (hit.collider != null && hit.collider.CompareTag(wallTag) && hit.distance < closestDist)
            {
                closestDist = hit.distance;
                endpoint = hit.point;
            }
        }

        // local vector for beam_logic visuals
        Vector2 localVector = transform.InverseTransformPoint(endpoint);
        beam.SetVector(localVector);

        // damage along beam
        ApplyBeamDamage(origin, endpoint);
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

    // Stop Laser beam and particles
    private void StopBeam()
    {
        beam.EndBeam();
        beam.DestroyBeam();
        enabled = false;
    }

    private float SecondsPerBeat()
    {
        return 60.0f / beatHandler.GetBPM();
    }
}
