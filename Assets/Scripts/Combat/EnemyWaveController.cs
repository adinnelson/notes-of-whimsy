using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Tilemaps;

public class EnemyWaveController : MonoBehaviour
{
    // Has the player entered the room before?
    private bool hasEntered = false;
    private bool wavesComplete = false;
    private bool isCombatRoom;
    private bool spawningEnemies = false;
    private int currentWave = 0;
    private int pendingSpawns = 0;
    // Used for wave difficulty scaling with progression
    private int roomsCleared = 0;

    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private List<GameObject> enemyPrefabs = new List<GameObject>();
    private BoxCollider2D roomSpawnableBoundsCollider;

    private RoomInfo roomInfo;

    private const int BASE_ENEMIES = 1;
    private const float ENEMY_SPAWN_EFFECT_DURATION = 1.0f;
    // Hard cap, likely never reached due to wave specific caps
    private const int MAX_ENEMY_SPAWNS = 8;

     private void Awake()
    {
        roomInfo = GetComponentInParent<RoomInfo>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyPrefabs = roomInfo.GetEnemyPrefabs();
        isCombatRoom = roomInfo.GetRoomType() == RoomTypes.Combat;
        roomSpawnableBoundsCollider = GetComponent<BoxCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasEntered || roomInfo.GetSafety() || spawningEnemies)
            return;

        int numberOfWaves = CalculateWaveCount();

        // Check if all enemies in the current wave have been defeated
        if (currentWave >= 1 && spawnedEnemies.Count == 0)
        {
            if (currentWave < numberOfWaves)
            {
                SpawnEnemyWave();
            }
        }

        // If all waves have been spawned and all enemies defeated, unlock the room
        if (currentWave >= numberOfWaves && spawnedEnemies.Count == 0 && !wavesComplete && !spawningEnemies)
        {
            wavesComplete = true;
            roomInfo.UnlockRoom();
            roomsCleared += 1;
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
    /// Calculate the number of waves as current floor (one-indexed).
    /// Adds one extra wave for combat rooms.
    /// </summary>
    /// <returns></returns>
    public int CalculateWaveCount()
    {
        int numberOfWaves = ProgressFloor.GetFloorsCompleted() + 1;
        if (isCombatRoom)
        {
            numberOfWaves++;
        }
        return numberOfWaves;
    }

    /// <summary>
    /// When the player enters the room for the first time, lock the room and start spawning enemy waves. If the room is already safe or has been entered before, do nothing.
    /// </summary>
    /// <param name="collision"></param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasEntered || roomInfo.GetSafety() || CalculateWaveCount() <= 0)
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
        spawningEnemies = true;

        int enemiesToSpawn = CalculateEnemiesToSpawn();

        pendingSpawns = enemiesToSpawn;
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            SpawnRandomEnemy(spawnPosition);
        }
        currentWave++;

        // Calculate number of enemies to spawn
        int CalculateEnemiesToSpawn()
        {
            int roomProgression = roomsCleared / 10;
            int floorProgression = ProgressFloor.GetFloorsCompleted();
            int waveProgression = currentWave;
            int totalEnemies = BASE_ENEMIES + roomProgression + floorProgression + waveProgression;
            int maxEnemies = Mathf.Min(CalculateWaveCount() * 2, MAX_ENEMY_SPAWNS);

            totalEnemies = Mathf.Min(totalEnemies, maxEnemies);
            return totalEnemies;
        }

        // Get a random position within the bounds of the room's BoxCollider2D
        Vector3 GetRandomSpawnPosition()
        {
            Bounds bounds = roomSpawnableBoundsCollider.bounds;
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            Vector3 spawnPosition = new Vector3(x, y, 0);
            return spawnPosition;
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
