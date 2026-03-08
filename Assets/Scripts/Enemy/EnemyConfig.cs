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

    [Header("Recovery")]
    public int recoverBeats = 1;     // Post-attack recovery time in beats

    [Header("Hurt Reaction")]
    public bool hurtInterruptsAttack = true;
    public int hurtBeats = 1;       // Beats of vulnerability after being hurt

    [Header("Gold Drop")]
    public GameObject goldCoinPrefab;
    public int goldCoinCount = 1;   //base amount of gold coins an enemy has
}
