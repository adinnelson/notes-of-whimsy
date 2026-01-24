using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using System.Globalization;

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

    // general track positional variables
    private Vector3 beatInitialPosition;
    private Vector3 beatEndPosition;
    private float beatDistance;
    private float spawnTime;
    
    // enabled when a beat is within acceptable range
    public bool validAttackInterval = false;

    // pools BeatItems to prevent constant spawning and destroying of GameObjects
    ObjectPool<BeatItem> beatPool;

    // current beats on track
    List<BeatItem> currentVisibleBeats = new List<BeatItem>();

    // step size beat item moves on FixedUpdate
    [SerializeField] private float stepSize = 0.1f;
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

        // initial beat item spawned
        currentVisibleBeats.Add(beatPool.Get());
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
            validAttackInterval = true;
        } 
        else 
        {
            validAttackInterval = false;
        }
    }

    // called when beat item hits end of track
    public void BeatArrived(BeatItem beat)
    {
        audioSource.PlayOneShot(clip);
        currentVisibleBeats.RemoveAt(0);
        beatPool.Release(beat);
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

}