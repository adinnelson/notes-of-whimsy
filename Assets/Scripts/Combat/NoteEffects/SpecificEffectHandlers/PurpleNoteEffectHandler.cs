using UnityEngine;

public class PurpleNoteEffectHandler : NoteEffectHandler
{
    private BeatHandler beatHandler;
    private LaserBeam laserPrefab;

    private bool onCooldown;

    public void SetLaser(LaserBeam laserPrefab)
    {
        this.laserPrefab = laserPrefab;
    }

    public override void Init(SpellDataSO spellData, PlayerAttack playerAttack)
    {
        this.spellData = spellData;
        this.playerAttack = playerAttack;
        this.note = spellData.NoteProjectilePrefab;

        beatHandler = FindObjectOfType<BeatHandler>();
    }

    public override void Fire()
    {
        if (onCooldown)
        {
            return;
        }

        if (beatHandler == null)
        {
            Debug.LogError("PurpleNoteEffectHandler: BeatHandler not assigned.", this);
            return;
        }

        if (laserPrefab == null)
        {
            Debug.LogError("PurpleNoteEffectHandler: Laser prefab not assigned.", this);
            return;
        }

        // Spawn laser
        LaserBeam beam = Instantiate(laserPrefab);
        beam.Init(transform, beatHandler);

        // Cooldown starts immediately (3 beats total)
        onCooldown = true;
        float secondsPerBeat = 60.0f / beatHandler.GetBPM();
        Invoke(nameof(ResetCooldown), secondsPerBeat * 3.0f);
    }

    private void ResetCooldown()
    {
        onCooldown = false;
    }

    public override void HitEnemy(IDamageable damageable)
    {
        // not used for beam
    }
}
