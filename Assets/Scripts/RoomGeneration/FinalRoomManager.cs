using Unity.VisualScripting;
using UnityEngine;

public class FinalRoomManager : MonoBehaviour
{

    [SerializeField]
    private Transform bossPrefab;

    [SerializeField]
    private GameObject endPrefab;

    [SerializeField]
    private Transform bossSpawnLocation;

    private Health bossHealth;
    private DeerBoss deerBoss;
    
    private bool endPortalSpawned = false;

    void Awake()
    {
        bossHealth = bossPrefab.GetComponent<Health>();
        deerBoss = bossPrefab.GetComponent<DeerBoss>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            bossHealth.enabled = true;
            deerBoss.enabled = true;
            // Instantiate(bossPrefab, bossSpawnLocation.position, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(bossHealth != null && bossHealth.CurrentHealth <= 0)
        {
            if (!endPortalSpawned)
            {
                Instantiate(endPrefab, transform.position, Quaternion.identity);
                endPortalSpawned = true;
            }
        }
    }
}
