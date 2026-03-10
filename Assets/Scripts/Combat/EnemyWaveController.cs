using UnityEngine;
using System.Collections.Generic;

public class EnemyWaveController : MonoBehaviour
{
    private int numberOfWaves;

    // Has the player entered the room before?
    private bool hasEntered = false;
    private int currentWave = 0;

    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private List<GameObject> enemyPrefabs = new List<GameObject>();

    private RoomInfo roomInfo;

    private const int BASE_ENEMIES_PER_WAVE = 3;
    private const int WAVE_ENEMY_MULTIPLIER = 2;

     private void Awake()
    {
        roomInfo = GetComponentInParent<RoomInfo>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyPrefabs = roomInfo.GetEnemyPrefabs();
        numberOfWaves = roomInfo.GetNumberOfWaves();
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasEntered || roomInfo.GetSafety())
            return;

        // Check if all enemies in the current wave have been defeated
        if (currentWave >= 1 && spawnedEnemies.Count == 0)
        {
            if (currentWave < numberOfWaves)
            {
                SpawnEnemyWave();
            }
            else
            {
                roomInfo.UnlockRoom();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
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

        int enemyCount = BASE_ENEMIES_PER_WAVE + (currentWave * WAVE_ENEMY_MULTIPLIER);

        for (int i = 0; i < enemyCount; i++)
        {
            // TODO: Replace with actual floor bounds (with padding) instead of random range
            Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
            SpawnRandomEnemy(spawnPosition);
        }
    }

    void SpawnRandomEnemy(Vector3 spawnPosition)
    {
        if (enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("No enemy prefabs assigned to this room!");
            return;
        }

        // Randomly select an enemy prefab from the list
        int randomIndex = Random.Range(0, enemyPrefabs.Count);
        GameObject enemyPrefab = enemyPrefabs[randomIndex];

        // Instantiate the enemy and add it to the spawnedEnemies list
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, transform);
        spawnedEnemies.Add(enemy);
    }

    public void OnEnemyDeath(GameObject enemy)
    {
        spawnedEnemies.Remove(enemy);
    }
}
