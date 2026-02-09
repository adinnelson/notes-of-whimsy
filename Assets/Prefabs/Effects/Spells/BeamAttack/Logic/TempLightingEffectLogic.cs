using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TempLightingEffectLogic : MonoBehaviour
{
    /// <summary>
    /// Invoked when the BeamStart animation is finished and the beam is at full strength.
    /// </summary>
    public event Action EventBeamOn;

    /// <summary>
    /// Invoked when the BeamEnd animation is finshed and the beam is no longer visible.
    /// </summary>
    public event Action EventBeamOff;

    [SerializeField] private float noteParticleEmitPercent = 1.0f;
    [SerializeField] private float yScale = 1.0f;
    [SerializeField] private float destroyTime = 1.0f;

    private Vector2 pointingVector;

    private Animator animator;

    private Transform beamVisual;
    private Transform musicNoteEmitter;
    private Transform impactParticleEmitter;
    private Transform lingeringParticleEmitter;

    private SpriteRenderer beamVisualSprite;
    private ParticleSystem musicNoteParticles;
    private ParticleSystem.ShapeModule musicNoteParticlesShape;
    private ParticleSystem.EmissionModule musicNoteParticlesEmission;

    private Vector2 targetPos = new Vector2(0, 0);
    public Vector2 TargetPos 
    {
        get 
        {
            return targetPos;
        }

        set
        {
            targetPos = value;
        }
    }

    void Start()
    {
        animator = GetComponent<Animator>();

        beamVisual = transform.Find("BeamVisual");
        musicNoteEmitter = transform.Find("MusicNoteParticles");
        impactParticleEmitter = transform.Find("ImpactParticles");
        lingeringParticleEmitter = transform.Find("LingeringParticles");

        beamVisualSprite = beamVisual.GetComponent<SpriteRenderer>();
        musicNoteParticles = musicNoteEmitter.GetComponent<ParticleSystem>();
        musicNoteParticlesShape = musicNoteParticles.shape;
        musicNoteParticlesEmission = musicNoteParticles.emission;

        StartBeam();
        UpdateBeam();
    }

    void Update()
    {

        // Example use, follows mouse and starts / stops with click

        Vector2 mouse_pos = targetPos;
        SetVector(transform.InverseTransformPoint(mouse_pos));
    }

    /// <summary>
    /// Sets the local vector of the beam.
    /// Beam will run from position -> position + vector
    /// </summary>
    /// <param name="newVector">The new local vector of the beam.</param>
    public void SetVector(Vector2 newVector)
    {
        pointingVector = newVector;
        UpdateBeam();
    }

    /// <summary>
    /// Plays the start animation of the beam.
    /// This animation also plays when the beam is created.
    /// </summary>
    public void StartBeam()
    {
        animator.Play("BeamStart");
    }

    /// <summary>
    /// Plays the end animation of the beam.
    /// Currently this does not remove the beam object, nor is there a way to know when the animation is finished.
    /// </summary>
    public void EndBeam()
    {
        animator.Play("BeamEnd");
    }

    /// <summary>
    /// Destroys the beam after all particles have faded.
    /// Must be called AFTER EndBeam().
    /// </summary>
    public void DestroyBeam()
    {
        Destroy(gameObject, destroyTime);
    }

    private void UpdateBeam()
    {
        float vect_rad = Mathf.Atan2(pointingVector.y, pointingVector.x);
        float vect_angle = vect_rad * Mathf.Rad2Deg;
        float vect_length = pointingVector.magnitude;

        beamVisual.localPosition = pointingVector - pointingVector * 0.5f * yScale;
        beamVisual.localRotation = Quaternion.Euler(0.0f, 0.0f, vect_angle);
        beamVisual.localScale = new Vector2(vect_length * 2, yScale);
        beamVisualSprite.material.SetFloat("_Rotation", vect_angle);
        beamVisualSprite.material.SetFloat("_XScale", vect_length);

        musicNoteEmitter.localPosition = beamVisual.localPosition;
        musicNoteEmitter.localRotation = beamVisual.localRotation;
        musicNoteParticlesShape.scale = new Vector3(vect_length, 0.0f, 0.0f);
        musicNoteParticlesEmission.rateOverTime = 8f * vect_length * noteParticleEmitPercent;

        impactParticleEmitter.localPosition = pointingVector - pointingVector * 0.5f;
        impactParticleEmitter.rotation = Quaternion.Euler(0, 0, vect_angle + 180.0f - 45.0f / 2.0f);

        lingeringParticleEmitter.localPosition = pointingVector - pointingVector.normalized;
        lingeringParticleEmitter.localRotation = beamVisual.localRotation;
    }
    
}
