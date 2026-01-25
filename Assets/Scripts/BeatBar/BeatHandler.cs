using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using System.Globalization;
using System.Reflection.Metadata;

public class BeatHandler : MonoBehaviour {

    // beats per minute
    [SerializeField] private float bpm;

    // spawn position
    [SerializeField] private Transform beatSpawnPoint;
    
    // end of track
    // stored as GameObject to add effects on beatItem finishing track
    [SerializeField] private GameObject endGraphic;

    // beatItem to spawn
    [SerializeField] private BeatItem beatGraphic;


    // step size beat item moves on FixedUpdate
    [SerializeField] private float stepSize = 0.1f;

    // general track positional variables
    private Vector3 beatInitialPosition;
    private Vector3 beatEndPosition;
    private float beatDistance;
    private float spawnTime;

    // pools BeatItems to prevent constant spawning and destroying of GameObjects
    private ObjectPool<BeatItem> beatPool;

    // current beats on track
    private List<BeatItem> currentVisibleBeats = new List<BeatItem>();
    
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
        beatInitialPosition = beatSpawnPoint.localPosition; 
        beatEndPosition = endGraphic.transform.localPosition;
        beatDistance = beatEndPosition.x - beatInitialPosition.x;
        spawnTime = 60f / bpm; 

        PopulateBeatBar();
    }

    float timeElapsed = 0;
    void FixedUpdate() 
    {
        // Add time track for when to spawn a new beat
        if (timeElapsed >= spawnTime)
        {
            currentVisibleBeats.Add(beatPool.Get());
            timeElapsed = 0;
            return;
        }

        timeElapsed += Time.fixedDeltaTime;

        if(currentVisibleBeats.Count == 0) return;

        // check interval of first one in list
        CheckValidAttackInterval(GetPercentRemainingFrontBeat(bpm));
    }

    // Creates a new pooled GameObject the first time (and whenever the pool needs more).
    private BeatItem CreateItem()
    {
        BeatItem beatItem = Instantiate(beatGraphic, beatSpawnPoint.position, Quaternion.identity);
        beatItem.gameObject.SetActive(false);
        
        return beatItem;
    }

    // Called when an item is taken from the pool.
    private void OnGet(BeatItem beat)
    {
        beat.Init(this, endGraphic, beatInitialPosition, stepSize);
        beat.gameObject.SetActive(true);
    }

    // Called when an item is returned to the pool.
    private void OnRelease(BeatItem beat)
    {
        beat.gameObject.SetActive(false);
    }

    // Called when the pool decides to destroy an item (e.g., above max size).
    private void OnDestroyItem(BeatItem beat)
    {
        Destroy(beat);
    }

    // returns percentage front beat item is from final destination
    public float GetPercentRemainingFrontBeat(float bpm) 
    {
        return (endGraphic.transform.position.x - currentVisibleBeats[0].transform.position.x) / beatDistance;
    }

    
    public void CheckValidAttackInterval (float percentage) 
    {                                                                        
        // if the percentage of track on BeatItem remaining is at acceptable distance for input
        if (percentage <= 0.2) 
        {
            ValidAttackInterval = true;
        } 
        else 
        {
            ValidAttackInterval = false;
        }
    }

    // called when beat item hits end of track
    public void BeatArrived(BeatItem beat)
    {
        currentVisibleBeats.RemoveAt(0);
        beatPool.Release(beat);
    }

    // loads beat bar with beat items leaving a buffer distance
    private void PopulateBeatBar(float bufferDistance = 0.0f)
    {
        float distanceToAdd = stepSize / Time.fixedDeltaTime * spawnTime;

        int i = 0;
        while(beatDistance - bufferDistance - distanceToAdd * i > 0)
        {
            BeatItem beatItem = beatPool.Get();
            beatItem.transform.position = new Vector3(beatSpawnPoint.position.x + distanceToAdd * i, endGraphic.transform.position.y, endGraphic.transform.position.z);
            currentVisibleBeats.Add(beatItem);

            i++;
        }
    }
}