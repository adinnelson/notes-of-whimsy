using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using FMODUnity;


public class BeatHandler : MonoBehaviour
{

    //Adding beat percussion sound (working on it)
    [Header("FMOD Settings")]
    [SerializeField] private EventReference beatEndEvent;
    private MusicManager musicManager;
    private float lastCheckedBPM = 0.0f;


    //FMOD music is now 8 beats divided 
    int musicBeatIndex;
    int prevMusicIndex = 0;

    private const float REQUIRED_ACCURACY = 0.75f;
    private const int BEAT_NUM = 8;

    [SerializeField] private PlayerAttack playerAttack;

    // beats per minute
    [SerializeField] private float bpm = 120.0f;

    // spawn position
    [SerializeField] private float beatEndSpawnDistance;
    [SerializeField] private float beatStartSpawnDistance;

    [SerializeField] private float numBeatsShown;

    // beat item to spawn
    [SerializeField] private GameObject beatGraphic;

    private SpriteRenderer spriteRenderer;

    private GameManager gm;
    private PlayerActiveSpellsHandler playerActiveSpellsHandler;

    // how close the next beat is
    private float percentToNextBeat = 1.0f;

    // this index changes halfway between each beat
    private int beatIndex = 0;

    // this index changes on beat
    private int onBeatIndex = 0;

    [SerializeField]
    // update this to change how many spells the player starts with
    private int startingSpellCount = 3;

    private bool beatIndexHasChanged = true;

    // lists of left and right beat items
    private List<GameObject> leftBeatItemGraphics = new List<GameObject>();
    private List<GameObject> rightBeatItemGraphics = new List<GameObject>();

    // Unlocked beat and order variables
    private static HashSet<int> unlockedBeats = new HashSet<int>();
    public static HashSet<int> UnlockedBeats => unlockedBeats;
    // tempory until the adding and removing effects system update occurs
    // -1 is default
    private Dictionary<int, Color> beatIdToColor = new Dictionary<int, Color>
    {
        { -1, Color.white },
        { 1, new Color(1, 0, 0.2f) },
        { 2, new Color(1, 1, 0) },
        { 3, new Color(1, 0.5f, 1) },
        { 4, new Color(0, 0.5f, 1) }
    };

    // enabled when a beat is within acceptable range
    public bool ValidAttackInterval = false;
    public bool ValidDashInterval = false;



    void Awake()
    {

        spriteRenderer = transform.Find("beatBarCenter").GetComponent<SpriteRenderer>();
        musicManager = MusicManager.instance;

        gm = GameObject.FindWithTag("GameManager")?.GetComponent<GameManager>();
        playerActiveSpellsHandler = GameObject.FindWithTag("Player")?.GetComponent<PlayerActiveSpellsHandler>();

        InitializeRandomSpells();

        playerAttack = GameObject.FindWithTag("Player")?.GetComponent<PlayerAttack>();

        PopulateBeatBar();
    }

    private void InitializeRandomSpells()
    {
        // BEAT_NUM is limit of spell slots
        // TODO: added actual randomness logic
        if(unlockedBeats.Count == 0)
        {
            int spellToUnlock = Random.Range(1, BEAT_NUM + 1);
            for(int i = 1; i <= startingSpellCount; i++)
            {
                spellToUnlock = Random.Range(1, BEAT_NUM + 1);
                while (unlockedBeats.Contains(spellToUnlock))
                {
                    spellToUnlock = Random.Range(1, BEAT_NUM + 1);
                }
                unlockedBeats.Add(spellToUnlock);
                playerActiveSpellsHandler.UnlockSlot(spellToUnlock);
                // FIXME: update this if we add new amount of spells
                int spellId = Random.Range(1, 5); // Assuming there are 4 spells to choose from
                playerActiveSpellsHandler.EquipSpell(spellToUnlock, spellId);
            }

            // // 1
            // unlockedBeats.Add(spellToUnlock);
            // playerActiveSpellsHandler.UnlockSlot(spellToUnlock);
            // playerActiveSpellsHandler.EquipSpell(spellToUnlock, 1);
            // spellToUnlock = Random.Range(1, BEAT_NUM + 1);
            // // 5
            // unlockedBeats.Add(spellToUnlock);
            // playerActiveSpellsHandler.UnlockSlot(spellToUnlock);
            // playerActiveSpellsHandler.EquipSpell(spellToUnlock, 3);
            // spellToUnlock = Random.Range(1, BEAT_NUM + 1);
            // //6
            // unlockedBeats.Add(spellToUnlock);
            // playerActiveSpellsHandler.UnlockSlot(spellToUnlock);
            // playerActiveSpellsHandler.EquipSpell(spellToUnlock, 2);
        }

    }


    //using Update to sync music and sound effect
    void Update()
    {
        //getting information about the current beat from music 
        musicBeatIndex = musicManager.timelineInfo.currentBeat;

        //Fire percussion sound on tempo if beat is unlocked 
        if (prevMusicIndex != musicBeatIndex)
        {
            prevMusicIndex = musicBeatIndex;
            // Debug.Log($"Music = {prevMusicIndex}");
            if (unlockedBeats.Contains(prevMusicIndex))
            {
                PlayBeatEndSound();
                // Debug.Log($"Music = {prevMusicIndex}");
            }
        }

        // Check for BPM changes to update the Beat bar according to music switch
        if (musicManager != null)
        {
            float currentMusicBPM = musicManager.GetCurrentBPM();
            if (currentMusicBPM > 0 && Mathf.Abs(lastCheckedBPM - currentMusicBPM) > 0.01f)
            {
                // Debug.Log(currentMusicBPM);
                lastCheckedBPM = currentMusicBPM;
                ChangeBPM(currentMusicBPM);
            }
        }

        // place beatbar at mouse
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        transform.position = new Vector3(mousePos.x, mousePos.y, transform.position.z);

        // needs to be x2 because we are fitting 8 beats into 4/4
        percentToNextBeat += Time.deltaTime * bpm / 60.0f * 2.0f;

        // fires on half beat
        if (percentToNextBeat > 0.5 && !beatIndexHasChanged)
        {
            if (gm)
            {
                gm.BeatHit = false;
            }
            //musicBeatIndex is coming from the FMOD music
            beatIndex = musicBeatIndex % BEAT_NUM - 1;

            if (playerAttack != null && unlockedBeats.Contains(beatIndex))
            {
                playerAttack.RemoveAttackLock(PlayerAttack.MISSED_ATTACK_LOCK_KEY);
            }

            beatIndexHasChanged = true;
        }

        // fires on beat
        if (percentToNextBeat > 1.0f)
        {
            //muiscBeatIndex is coming from the FMOD music 
            onBeatIndex = musicBeatIndex % BEAT_NUM;

            if (gm && !gm.BeatHit)
            {
                gm.TriggerBeat(onBeatIndex);
            }

            beatIndexHasChanged = false;
            percentToNextBeat -= 1.0f;
        }

        UpdateBeatVisuals();

        ValidAttackInterval = CheckValidAttackInterval(percentToNextBeat);
        ValidDashInterval = CheckValidDashInterval(percentToNextBeat);
    }




    private void UpdateBeatVisuals()
    {
        float beatSpacing = (beatStartSpawnDistance - beatEndSpawnDistance) / numBeatsShown;

        float growPerc = 0.8f + Mathf.Pow(1.0f - Mathf.Abs(percentToNextBeat - 0.5f), 2.0f) * 0.2f;
        spriteRenderer.transform.localScale = new Vector3(0.1f * growPerc, 0.1f * growPerc, spriteRenderer.transform.localScale.z);

        for (int i = 0; i < BEAT_NUM; i++)
        {
            // gets the index of the beat item from left to right based on the current beat index
            int adjustedBeatIndex = (((i - onBeatIndex) % BEAT_NUM) + BEAT_NUM) % BEAT_NUM;

            GameObject curLeftGraphic = leftBeatItemGraphics[i];
            GameObject curRightGraphic = rightBeatItemGraphics[i];
            SpriteRenderer curLeftSprite = curLeftGraphic.transform.Find("visual").GetComponent<SpriteRenderer>();
            SpriteRenderer curRightSprite = curRightGraphic.transform.Find("visual").GetComponent<SpriteRenderer>();

            float newOffset = beatEndSpawnDistance + (adjustedBeatIndex + 1.0f - percentToNextBeat) * beatSpacing;
            float newSize = 1.0f - (adjustedBeatIndex - percentToNextBeat + 1.0f) / numBeatsShown;

            curLeftGraphic.transform.localPosition = new Vector3(newOffset, 0, curLeftGraphic.transform.localPosition.z);
            curLeftGraphic.transform.localScale = new Vector3(1, newSize, 1);

            curRightGraphic.transform.localPosition = new Vector3(-newOffset, 0, curRightGraphic.transform.localPosition.z);
            curRightGraphic.transform.localScale = new Vector3(-1, newSize, 1);

            if (adjustedBeatIndex >= numBeatsShown || (i % 2 != 0 && !unlockedBeats.Contains(i + 1)))
            {
                curLeftSprite.color = Color.clear;
                curRightSprite.color = Color.clear;
            }

            else
            {
                curLeftSprite.color = beatIdToColor[playerActiveSpellsHandler.GetSpellIdFromSlotId(i + 1)];
                curRightSprite.color = beatIdToColor[playerActiveSpellsHandler.GetSpellIdFromSlotId(i + 1)];
            }
        }
    }

    // instantiates beat items for the left and right lists
    private void PopulateBeatBar()
    {
        for (int i = 0; i < BEAT_NUM; i++)
        {
            GameObject newLeftGraphic = Instantiate<GameObject>(beatGraphic);
            newLeftGraphic.transform.SetParent(transform);
            leftBeatItemGraphics.Add(newLeftGraphic);

            GameObject newRightGraphic = Instantiate<GameObject>(beatGraphic);
            newRightGraphic.transform.SetParent(transform);
            rightBeatItemGraphics.Add(newRightGraphic);
        }
    }

    public float GetPercentToNextBeat()
    {
        return percentToNextBeat;
    }

    // get whether an attack can be made based on the percentage to next beat
    public bool CheckValidAttackInterval(float percentage)
    {
        return unlockedBeats.Contains(beatIndex + 1) && (percentage <= REQUIRED_ACCURACY || percentage >= 1.0f - REQUIRED_ACCURACY);

    }

    public bool CheckValidDashInterval (float percentage)
    {
        return percentage <= REQUIRED_ACCURACY || percentage >= 1.0f - REQUIRED_ACCURACY;

    }

    // adds tick id to hashset
    public void BeatUnlocked(int beatId)
    {
        unlockedBeats.Add(beatId);
    }

    public float GetBPM()
    {
        return bpm;
    }

    public int GetBeatIndex()
    {
        return beatIndex + 1;
    }

    public static void ClearUnlockedBeats()
    {
        unlockedBeats.Clear();
    }

    //Update the Beat bar bpm to reflect the changing between rooms


    // method to reset the Beat bar to a new bpm
    public void ChangeBPM(float newBPM)
    {
        bpm = newBPM;

        // Reset beat ID if needed
        percentToNextBeat = 1.0f;
        beatIndex = 0;
        onBeatIndex = 0;
        beatIndexHasChanged = true;
    }

    private void PlayBeatEndSound()
    {
        if (!beatEndEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(beatEndEvent);
        }
    }
}



    //Old code used for testing. 
    // void Update()
    // {
    //     // //For testing:
    //     // var keyboard = Keyboard.current;
    //     // if (keyboard == null) return; // No keyboard connected
    //     // if (keyboard.bKey.wasPressedThisFrame)
    //     // {
    //     //     ChangeBPM(100f);
    //     // }
    //     // if (keyboard.nKey.wasPressedThisFrame)
    //     // {
    //     //     ChangeBPM(120f);
    //     // }
    // }