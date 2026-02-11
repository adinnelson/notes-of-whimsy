using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Detection")]
    public float aggroRange = 8.0f;     // Distance at which enemy wakes up

    [Header("Movement")]
    public float chaseSpeed = 3.0f;     // Speed while chasing
    public float attackRange = 8.0f;  // Distance from player before starting attack


    [Header("Attack")]
    public float damage = 10.0f;        // Damage dealt per hit
    public float telegraphSeconds = 0.25f; // How long the enemy stays in the Telegraph state before the attack actually happens.
    public float attackSeconds = 0.25f; // How long the enemy stays in the Attack state, meaning the attack is active.

    [Header("Recovery")]
    public float recoverSeconds = 0.2f;     // Post-attack downtime
    public float cooldownSeconds = 0.8f;    // Time before next attack can start

    [Header("Hurt Reaction")]
    public bool hurtInterruptsAttack = true;
    public float hurtSeconds = 0.15f;
}
