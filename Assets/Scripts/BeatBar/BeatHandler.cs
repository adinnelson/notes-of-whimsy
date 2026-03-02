using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using UnityEngine.InputSystem;
using System.Globalization;

public class BeatHandler : MonoBehaviour {

    private const float REQUIRED_ACCURACY = 0.1f;

    [SerializeField] private PlayerAttack playerAttack;

    // beats per minute
    [SerializeField] private float bpm = 120f;

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

    private GameManager gm;
    private PlayerActiveSpellsHandler playerActiveSpellsHandler;

    // general track positional variables
    private float beatDistance;
    private float spawnTime;

    private float timeElapsed = 0.0f;

    // pools BeatItems to prevent constant spawning and destroying of GameObjects
    private ObjectPool<BeatItem> beatPool;

    // Unlocked beat and order variables
    HashSet<int> unlockedBeats = new HashSet<int>();
    int nextBeatId = 1;
    int maxBeatId = 8;

    // tempory until the adding and removing effects system update occurs
    Dictionary<int, Color> beatIdToColor = new Dictionary<int, Color>
    {
        { 1, Color.red },
        { 2, Color.yellow },
        { 3, Color.purple },
        { 4, Color.blue }
    };

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
        gm = GameObject.FindWithTag("GameManager")?.GetComponent<GameManager>();
        playerActiveSpellsHandler = GameObject.FindWithTag("Player")?.GetComponent<PlayerActiveSpellsHandler>();

        beatDistance =  beatSpawnPoint.position.x - endGraphic.transform.position.x;
        spawnTime = 60.0f / bpm;

        unlockedBeats.Add(1);
        playerActiveSpellsHandler.UnlockSlot(1);
        playerActiveSpellsHandler.EquipSpell(1, 1);
        unlockedBeats.Add(5);
        playerActiveSpellsHandler.UnlockSlot(5);
        playerActiveSpellsHandler.EquipSpell(5, 3);

        PopulateBeatBar();
    }

    void FixedUpdate()
    {
        // place beatbar at mouse
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        transform.position = new Vector3(mousePos.x, mousePos.y, transform.position.z);

        // Add time track for when to spawn a new beat
        if (timeElapsed >= spawnTime)
        {
            bool playerHasBeat = unlockedBeats.Contains(nextBeatId);

            int spelldInBeatSlot = playerActiveSpellsHandler.GetSpellIdFromSlotId(nextBeatId);

            BeatItem beatItem = beatPool.Get();
            beatItem.Init(this, spelldInBeatSlot > 0 ? beatIdToColor[spelldInBeatSlot] : Color.white, endGraphic, beatSpawnPoint.position, stepSize, nextBeatId, playerHasBeat);

            if(!playerHasBeat)
            {
                beatItem.GetComponent<SpriteRenderer>().enabled = false;
            }
            else
            {
                beatItem.GetComponent<SpriteRenderer>().enabled = true;
            }

            currentVisibleBeats.Add(beatItem);
            timeElapsed = 0.0f;

            for(int i = 0;i < activeBeatSpawners.Count;i++)
            {
                activeBeatSpawners[i].StartSpawnCountdown();
            }

            nextBeatId++;
            if(nextBeatId > maxBeatId)
            {
                nextBeatId = 1;
            }

            return;
        }

        timeElapsed += Time.fixedDeltaTime;

        if(currentVisibleBeats.Count == 0) return;

        float percentage = GetPercentRemainingFrontBeat();

        // check interval of first one in list
        CheckValidAttackInterval(percentage);

        if (!gm.BeatHit && percentage < 0.05f)
        {
            gm.TriggerBeat(currentVisibleBeats[0].BeatId);
        }
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
        return (currentVisibleBeats[0].transform.position.x - endGraphic.transform.position.x) / beatDistance;
    }

    // sets flag if beatItem is close enough end point
    public void CheckValidAttackInterval (float percentage)
    {
        // if the percentage of track on BeatItem remaining is at acceptable distance for input
        if (currentVisibleBeats[0].Unlocked && percentage <= REQUIRED_ACCURACY)
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

        if (playerAttack != null && beatItem.Unlocked)
        {
            playerAttack.RemoveAttackLock(PlayerAttack.MISSED_ATTACK_LOCK_KEY);
        }

        RemoveFrontBeat();
        gm.BeatHit = false;
    }

    // returns front beat
    // or null if no beats
    public BeatItem GetFrontBeat(bool onlyVisible = true)
    {
        if(currentVisibleBeats.Count <= 0) return null;

        if(onlyVisible)
        {
            for(int i = 0;i < currentVisibleBeats.Count;i++)
            {
                if(!currentVisibleBeats[i].Unlocked) continue;

                return currentVisibleBeats[i];
            }

            return null;
        }

        return currentVisibleBeats[0];
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

    // DEBRECATED FROM NEW TICK SYSTEM
    // spawns a beat on bar with passed in color
    public void SpawnAdditionalBeat(Color colour)
    {
        BeatItem beatItem = beatPool.Get();
        beatItem.Init(this, colour, endGraphic, beatSpawnPoint.position, stepSize, 0);

        currentVisibleBeats.Add(beatItem);
    }

    // loads beat bar with beat items leaving a buffer distance
    private void PopulateBeatBar(float bufferDistance = 0.0f)
    {
        float distanceToAdd = stepSize / Time.fixedDeltaTime * spawnTime;

        float beatItemSpeed = stepSize / Time.fixedDeltaTime;

        int beatIdCurr = (int)(beatDistance / (bufferDistance + distanceToAdd));

        int beatId = beatIdCurr;

        // spawn as many beat items that are needed given buffer passed and bar size
        for(int i = 0; beatDistance - bufferDistance - distanceToAdd * i > 0; i++)
        {
            if(!unlockedBeats.Contains(beatId))
            {
                beatId--;
                continue;
            }

            Vector3 spawnPosition = new Vector3(beatSpawnPoint.position.x - distanceToAdd * i, endGraphic.transform.position.y, endGraphic.transform.position.z);

            BeatItem beatItem = beatPool.Get();
            beatItem.Init(this, beatIdToColor[beatId], endGraphic, spawnPosition, stepSize, beatId);

            currentVisibleBeats.Insert(0, beatItem);

            // spawns in child spawners initial beat items throughout beatbar
            for(int j = 0;j > activeBeatSpawners.Count;j++)
            {
                Vector3 additionalSpawnPosition = spawnPosition - Vector3.left * beatItemSpeed * activeBeatSpawners[j].SpawnTime;

                if(additionalSpawnPosition.x < beatSpawnPoint.position.x) break;

                BeatItem additionalBeatItem = beatPool.Get();
                additionalBeatItem.Init(this, activeBeatSpawners[j].Colour, endGraphic, additionalSpawnPosition, stepSize, 0);

                currentVisibleBeats.Add(additionalBeatItem);
            }

            beatId--;
        }

        this.nextBeatId = beatIdCurr + 1;
    }

    // adds tick id to hashset
    // unlocks annd unhides any ticks on bar
    public void BeatUnlocked(int beatId)
    {
        unlockedBeats.Add(beatId);

        for (int i = 0;i < currentVisibleBeats.Count;i++)
        {
            if(currentVisibleBeats[i].BeatId != beatId) continue;

            currentVisibleBeats[i].GetComponent<SpriteRenderer>().enabled = true;
            currentVisibleBeats[i].Unlocked = true;
        }
    }

    public float GetBPM()
    {
        return bpm;
    }
}
