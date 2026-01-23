using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class beam_logic : MonoBehaviour
{
    /// <summary>
    /// Invoked when the BeamStart animation is finished and the beam is at full strength.
    /// </summary>
    public event Action event_beam_on;

    /// <summary>
    /// Invoked when the BeamEnd animation is finshed and the beam is no longer visible.
    /// </summary>
    public event Action event_beam_off;

    public float note_particle_emit_percent = 1f;
    public float y_scale = 1f;
    public float destroy_time = 1f;

    private Vector2 pointing_vector;

    private Animator animator;

    private Transform beam_visual;
    private Transform music_note_emitter;
    private Transform impact_particle_emitter;
    private Transform lingering_particle_emitter;

    private SpriteRenderer beam_visual_sprite;
    private ParticleSystem music_note_particles;
    private ParticleSystem.ShapeModule music_note_particles_shape;
    private ParticleSystem.EmissionModule music_note_particles_emission;



    /// <summary>
    /// Sets the local vector of the beam.
    /// Beam will run from position -> position + vector
    /// </summary>
    /// <param name="new_vector">The new local vector of the beam.</param>
    public void SetVector(Vector2 new_vector)
    {
        pointing_vector = new_vector + new_vector.normalized;
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
        Destroy(gameObject, destroy_time);
    }



    void Start()
    {
        animator = GetComponent<Animator>();

        beam_visual = transform.Find("BeamVisual");
        music_note_emitter = transform.Find("MusicNoteParticles");
        impact_particle_emitter = transform.Find("ImpactParticles");
        lingering_particle_emitter = transform.Find("LingeringParticles");

        beam_visual_sprite = beam_visual.GetComponent<SpriteRenderer>();
        music_note_particles = music_note_emitter.GetComponent<ParticleSystem>();
        music_note_particles_shape = music_note_particles.shape;
        music_note_particles_emission = music_note_particles.emission;

        UpdateBeam();
    }

    void Update()
    {

        // Example use, follows mouse and starts / stops with click

        //Vector2 mouse_pos = Mouse.current.position.ReadValue();
        //SetVector(transform.InverseTransformPoint(Camera.main.ScreenToWorldPoint(mouse_pos)));

        //Mouse mouse = Mouse.current;

        //if (mouse == null) return;

        //if (mouse.leftButton.wasPressedThisFrame)
        //{
        //    StartBeam();
        //}

        //if (mouse.leftButton.wasReleasedThisFrame)
        //{
        //    EndBeam();
        //}
    }




    private void UpdateBeam()
    {
        float vect_rad = Mathf.Atan2(pointing_vector.y, pointing_vector.x);
        float vect_angle = vect_rad * Mathf.Rad2Deg;
        float vect_length = pointing_vector.magnitude;

        beam_visual.localPosition = pointing_vector / 2f - pointing_vector.normalized / y_scale / 2f;
        beam_visual.localRotation = Quaternion.Euler(0, 0, vect_angle);
        beam_visual.localScale = new Vector2(vect_length, y_scale);
        beam_visual_sprite.material.SetFloat("_Rotation", vect_angle);
        beam_visual_sprite.material.SetFloat("_XScale", vect_length);

        music_note_emitter.localPosition = beam_visual.localPosition;
        music_note_emitter.localRotation = beam_visual.localRotation;
        music_note_particles_shape.scale = new Vector3(vect_length - 1f, 0f, 0f);
        music_note_particles_emission.rateOverTime = 8f * (vect_length - 1f) * note_particle_emit_percent;

        impact_particle_emitter.localPosition = pointing_vector - pointing_vector.normalized * 0.5f;
        impact_particle_emitter.rotation = Quaternion.Euler(0, 0, vect_angle + 180f - 45f / 2f);

        lingering_particle_emitter.localPosition = pointing_vector - pointing_vector.normalized;
        lingering_particle_emitter.localRotation = beam_visual.localRotation;
    }
}
