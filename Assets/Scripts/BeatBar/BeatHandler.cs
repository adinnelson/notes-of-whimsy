using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using System.Globalization;

public class BeatHandler : MonoBehaviour {

    private const float REQUIRED_ACCURACY = 0.1f;
    private const float LATE_OFFSET = 0.5f; 

    [SerializeField] private PlayerAttack playerAttack;

    // beats per minute
    [SerializeField] private float bpm;

    // spawn position
    [SerializeField] private Transform beatSpawnPoint;
    
    // end of track
    // stored as GameObject to add effects on beatItem finishing track
    [SerializeField] private GameObject endGraphic;

    // beatItem to spawn
    [SerializeField] private BeatItem beatGraphic;

    // child spawner prefab
    [SerializeField] private ChildBeatSpawner childBeatSpawner;

    // step size beat item moves on FixedUpdate
    [SerializeField] private float stepSize = 0.065f;
    
    // default colour for main bpm beats
    [SerializeField] private Color defaultColour;

    // general track positional variables
    private Vector3 beatEndPosition;
    private float beatDistance;
    private float spawnTime;

    private float timeElapsed = 0.0f;

    // pools BeatItems to prevent constant spawning and destroying of GameObjects
    private ObjectPool<BeatItem> beatPool;

    // current beats on track
    private List<BeatItem> currentVisibleBeats = new List<BeatItem>();

    // list of spawners
    private List<ChildBeatSpawner> activeBeatSpawners = new List<ChildBeatSpawner>();

    // enabled when a beat is within acceptable range
    public bool ValidAttackInterval = false;

    void Awake()
    {
        beatPool = new ObjectPool<BeatItem>(
            createFunc: CreateItem,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyItem,
            collectionCheck: true,   // helps catch double-release mistakes
            defaultCapacity: 10,
            maxSize: 50
        );
    }

    void Start() 
    {
        beatEndPosition = new Vector3(endGraphic.transform.position.x + LATE_OFFSET, endGraphic.transform.position.y, endGraphic.transform.position.z);
        beatDistance = endGraphic.transform.position.x - beatSpawnPoint.position.x;
        spawnTime = 60f / bpm; 

        PopulateBeatBar();
    }

    void FixedUpdate() 
    {
        // Add time track for when to spawn a new beat
        if (timeElapsed >= spawnTime)
        {
            BeatItem beatItem = beatPool.Get();
            beatItem.Init(this, defaultColour, beatEndPosition, beatSpawnPoint.position, stepSize);

            currentVisibleBeats.Add(beatItem);
            timeElapsed = 0.0f;

            for(int i = 0;i < activeBeatSpawners.Count;i++)
            {
                activeBeatSpawners[i].StartSpawnCountdown();
            }

            return;
        }

        timeElapsed += Time.fixedDeltaTime;

        if(currentVisibleBeats.Count == 0) return;

        // check interval of first one in list
        CheckValidAttackInterval(GetPercentRemainingFrontBeat());
    }

    // Creates a new pooled GameObject the first time (and whenever the pool needs more).
    private BeatItem CreateItem()
    {
        BeatItem beatItem = Instantiate(beatGraphic, beatSpawnPoint.position, Quaternion.identity);
        beatItem.gameObject.SetActive(false);
        beatItem.transform.parent = this.transform;

        return beatItem;
    }

    // Called when an item is taken from the pool.
    private void OnGet(BeatItem beatItem)
    {
        beatItem.gameObject.SetActive(true);
    }

    // Called when an item is returned to the pool.
    private void OnRelease(BeatItem beatItem)
    {
        beatItem.gameObject.SetActive(false);
    }

    // Called when the pool decides to destroy an item (e.g., above max size).
    private void OnDestroyItem(BeatItem beatItem)
    {
        Destroy(beatItem);
    }

    // returns percentage front beat item is from final destination
    public float GetPercentRemainingFrontBeat() 
    {
        return (endGraphic.transform.position.x - currentVisibleBeats[0].transform.position.x) / beatDistance;
    }

    // sets flag if beatItem is close enough end point
    public void CheckValidAttackInterval (float percentage) 
    {                                             
        // if the percentage of track on BeatItem remaining is at acceptable distance for input
        if (percentage <= REQUIRED_ACCURACY) 
        {
            ValidAttackInterval = true;
        } 
        else 
        {
            ValidAttackInterval = false;
        }
    }

    // called when beat item hits end of track
    public void BeatArrived(BeatItem beatItem)
    {
        if(beatItem != currentVisibleBeats[0])
        {
            Debug.Log("Beats arrived out of order!");
        }

        if (playerAttack != null)
        {
            playerAttack.RemoveAttackLock(PlayerAttack.MISSED_ATTACK_LOCK_KEY);
        }

        RemoveFrontBeat();
    }

    // remove front beat from list and return to pool
    public void RemoveFrontBeat()
    {
        BeatItem beatItem = currentVisibleBeats[0];
        currentVisibleBeats.RemoveAt(0);
        beatPool.Release(beatItem);
    }

    // create child beat spawner
    public void CreateChildSpawner(Color colour, float spawnTime)
    {
        ChildBeatSpawner spawner = Instantiate(childBeatSpawner, beatSpawnPoint.position, Quaternion.identity); 
        spawner.Init(this, colour, beatSpawnPoint.position, spawnTime);

        activeBeatSpawners.Add(spawner);
    }

    // spawns a beat on bar with passed in color
    public void SpawnAdditionalBeat(Color colour)
    {
        BeatItem beatItem = beatPool.Get();
        beatItem.Init(this, colour, beatEndPosition, beatSpawnPoint.position, stepSize);

        currentVisibleBeats.Add(beatItem);
    }

    // loads beat bar with beat items leaving a buffer distance
    private void PopulateBeatBar(float bufferDistance = 0.0f)
    {
        float distanceToAdd = stepSize / Time.fixedDeltaTime * spawnTime;

        float beatItemSpeed = stepSize / Time.fixedDeltaTime;

        // spawn as many beat items that are needed given buffer passed and bar size
        for(int i = 0; beatDistance - bufferDistance - distanceToAdd * i > 0; i++)
        {
            Vector3 spawnPosition = new Vector3(beatSpawnPoint.position.x + distanceToAdd * i, endGraphic.transform.position.y, endGraphic.transform.position.z);

            BeatItem beatItem = beatPool.Get();
            beatItem.Init(this, defaultColour, beatEndPosition, spawnPosition, stepSize);

            currentVisibleBeats.Insert(0, beatItem);

            // spawns in child spawners initial beat items throughout beatbar
            for(int j = 0;j < activeBeatSpawners.Count;j++)
            {
                Vector3 additionalSpawnPosition = spawnPosition - Vector3.right * beatItemSpeed * activeBeatSpawners[j].SpawnTime;
                
                if(additionalSpawnPosition.x < beatSpawnPoint.position.x) break;

                BeatItem additionalBeatItem = beatPool.Get();
                additionalBeatItem.Init(this, activeBeatSpawners[j].Colour, beatEndPosition, additionalSpawnPosition, stepSize);

                currentVisibleBeats.Add(additionalBeatItem);
            }
        }
    }
}