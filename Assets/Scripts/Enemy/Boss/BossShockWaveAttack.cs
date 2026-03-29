using UnityEngine;
using System.Collections;

/// <summary>
/// Expanding shockwave ring spawned by DeerBoss during the SHOCKWAVE attack.
/// Subscribes to OnOddBeatTriggered and rapidly scales up on each beat,
/// mimicking how a boar charges quickly right after a beat hits.
/// Damages the player on contact.
///
/// Prefab setup:
///   - CircleCollider2D set to "Is Trigger"
///   - SpriteRenderer with a ring/donut sprite
///   - This component
///
/// The ring starts small and expands 3 times (one per beat on beats 5, 6, 7
/// of the boss's sequence). Each expansion is a quick lerp, not a teleport.
/// </summary>
public class BossShockWaveAttack : MonoBehaviour
{
    [Header("Expansion")]
    [Tooltip("How quickly the ring scales to its next size after a beat.")]
    [SerializeField] private float expandDuration = 0.15f;
    [Tooltip("Number of beat-driven expansions before the ring self-destructs.")]
    [SerializeField] private int totalExpansions = 3;
    [Tooltip("How much localScale increases per expansion step.")]
    [SerializeField] private float scalePerExpansion = 3f;

    private float damage;
    private GameManager gameManager;
    private int expansionsRemaining;
    private float currentScale = 1f;

    /// <summary>
    /// Called by DeerBoss immediately after instantiation.
    /// </summary>
    public void Initialize(float dmg, GameManager gm)
    {
        damage = dmg;
        gameManager = gm;
        expansionsRemaining = totalExpansions;

        gameManager.OnOddBeatTriggered += OnBeat;
    }

    private void OnBeat()
    {
        if (expansionsRemaining <= 0) return;

        expansionsRemaining--;
        StartCoroutine(ExpandStep());

        if (expansionsRemaining <= 0)
        {
            gameManager.OnOddBeatTriggered -= OnBeat;
            Destroy(gameObject, expandDuration + 0.1f);
        }
    }

    private IEnumerator ExpandStep()
    {
        float startScale = currentScale;
        float targetScale = currentScale + scalePerExpansion;
        float elapsed = 0f;

        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / expandDuration;
            currentScale = Mathf.Lerp(startScale, targetScale, t);
            transform.localScale = Vector3.one * currentScale;
            yield return null;
        }

        currentScale = targetScale;
        transform.localScale = Vector3.one * currentScale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(damage);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnOddBeatTriggered -= OnBeat;
    }
}
