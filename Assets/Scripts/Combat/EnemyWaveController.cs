using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyWaveController : MonoBehaviour
{
    // Has the player entered the room before?
    private bool hasEntered = false;
    private bool wavesComplete = false;
    private int numberOfWaves;
    private bool isCombatRoom;
    private bool spawningEnemies = false;
    private int currentWave = 0;
    private int pendingSpawns = 0;

    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private List<GameObject> enemyPrefabs = new List<GameObject>();

    private RoomInfo roomInfo;

    private const int BASE_ENEMIES_PER_WAVE = 1;
    private const float GENERIC_WAVE_ENEMY_SCALAR = 1.5f;
    private const float CHALLENGE_WAVE_SCALAR = 1.34f;
    private const float ENEMY_SPAWN_EFFECT_DURATION = 1.0f;

    // Max rectangular offset from center of room that enemies can spawn
    private const float SPAWN_OFFSET_MAX = 3.0f;

     private void Awake()
    {
        roomInfo = GetComponentInParent<RoomInfo>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyPrefabs = roomInfo.GetEnemyPrefabs();
        numberOfWaves = roomInfo.GetNumberOfWaves();
        isCombatRoom = roomInfo.GetRoomType() == RoomTypes.Combat;
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasEntered || roomInfo.GetSafety() || spawningEnemies)
            return;

        // Check if all enemies in the current wave have been defeated
        if (currentWave >= 1 && spawnedEnemies.Count == 0)
        {
            if (currentWave < numberOfWaves)
            {
                SpawnEnemyWave();
            }
        }

        if (currentWave >= numberOfWaves && spawnedEnemies.Count == 0 && !wavesComplete && !spawningEnemies)
        {
            wavesComplete = true;
            roomInfo.UnlockRoom();
        }
    }

    public bool AreWavesComplete()
    {
        return wavesComplete;
    }

    /// <summary>
    /// Called by an enemy when it dies to remove itself from the spawnedEnemies list
    /// </summary>
    /// <param name="enemy"></param>
    public void OnEnemyDeath(GameObject enemy)
    {
        spawnedEnemies.Remove(enemy);
    }

    /// <summary>
    /// When the player enters the room for the first time, lock the room and start spawning enemy waves. If the room is already safe or has been entered before, do nothing.
    /// </summary>
    /// <param name="collision"></param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasEntered || roomInfo.GetSafety() || numberOfWaves <= 0)
        {
            hasEntered = true;
            return;
        }

        if (collision.CompareTag("Player"))
        {
            hasEntered = true;
            roomInfo.LockRoom();
            SpawnEnemyWave();
        }
    }

    /// <summary>
    /// Spawns all enemies assigned to this room at random positions on the floor
    /// </summary>
    private void SpawnEnemyWave()
    {
        currentWave++;
        spawningEnemies = true;

        float waveMultiplier = isCombatRoom ? CHALLENGE_WAVE_SCALAR : GENERIC_WAVE_ENEMY_SCALAR;
        int enemyCount = BASE_ENEMIES_PER_WAVE +
                        (int)(currentWave * waveMultiplier);

        pendingSpawns = enemyCount;
        for (int i = 0; i < enemyCount; i++)
        {
            // TODO: Replace with actual floor bounds (with padding) instead of random range
            Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-SPAWN_OFFSET_MAX, SPAWN_OFFSET_MAX), Random.Range(-SPAWN_OFFSET_MAX, SPAWN_OFFSET_MAX), 0);
            SpawnRandomEnemy(spawnPosition);
        }
    }

    /// <summary>
    /// Spawns a random enemy from the list of enemy prefabs at the given position with a spawn effect
    /// </summary>
    /// <param name="spawnPosition"></param>
    private void SpawnRandomEnemy(Vector3 spawnPosition)
    {
        if (enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("No enemy prefabs assigned to this room!");
            return;
        }

        // Randomly select an enemy prefab from the list
        int randomIndex = Random.Range(0, enemyPrefabs.Count);
        GameObject enemyPrefab = enemyPrefabs[randomIndex];

        StartCoroutine(SpawnEnemyWithEffect(spawnPosition, enemyPrefab, ENEMY_SPAWN_EFFECT_DURATION));
    }

    /// <summary>
    /// Coroutine to spawn a primitive object then scale up from zero over x seconds before replacing with the actual enemy prefab
    /// </summary>
    /// <param name="spawnPosition"></param>
    /// <param name="enemyPrefab"></param>
    /// <param name="effectDuration"></param>
    private IEnumerator SpawnEnemyWithEffect(Vector3 spawnPosition, GameObject enemyPrefab, float effectDuration)
    {
        // Placeholder spawn effect using a sphere that scales up from zero
        // TODO: Replace with actual spawn effect
        GameObject spawnEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spawnEffect.transform.parent = transform; // Set the spawn effect as a child of the EnemyWaveController for organization
        spawnEffect.transform.position = spawnPosition;
        spawnEffect.transform.localScale = Vector3.zero;
        spawnEffect.GetComponent<Collider>().enabled = false;

        // Scale up the spawn effect over time
        float elapsedTime = 0.0f;
        while (elapsedTime < effectDuration)
        {
            float scale = Mathf.Lerp(0.0f, 1.0f, elapsedTime / effectDuration);
            spawnEffect.transform.localScale = new Vector3(scale, scale, scale);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the spawn effect is fully scaled up
        spawnEffect.transform.localScale = Vector3.one;

        // Instantiate the actual enemy prefab at the same position
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, transform);
        spawnedEnemies.Add(enemy);

        // Destroy the spawn effect object
        Destroy(spawnEffect);

        pendingSpawns--;
        if (pendingSpawns <= 0)
            spawningEnemies = false;
    }
}
