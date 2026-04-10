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

    private const float REQUIRED_ACCURACY = 0.35f; // 25% of the beat duration on either side
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

        playerAttack = GameObject.FindWithTag("Player")?.GetComponent<PlayerAttack>();

        PopulateBeatBar();
    }

    void Start()
    {
        InitializeRandomSpells();
    }

    //using Update to sync music and sound effect
    void Update()
    {
        //getting information about the current beat from music
        musicBeatIndex = musicManager.timelineInfo.currentBeat;

        //Fire percussion sound on tempo if beat is unlocked
        HandleFMODBeatChange();

        // Check for BPM changes to update the Beat bar according to music switch
        SyncBPMFromMusic();

        // place beatbar at mouse
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        transform.position = new Vector3(mousePos.x, mousePos.y, transform.position.z);

        // needs to be x2 because we are fitting 8 beats into 4/4
        percentToNextBeat += Time.deltaTime * bpm / 60.0f * 2.0f;

        // fires on half beat
        UpdateBeatIndexAtHalfBeat();

        // fires on beat
        ClampTimerIfAhead();
        UpdateBeatVisuals();

        ValidAttackInterval = CheckValidAttackInterval(percentToNextBeat);
        ValidDashInterval = CheckValidDashInterval(percentToNextBeat);

        // Fires when FMOD reports a new beat. Syncs timing and triggers beat events.
        void HandleFMODBeatChange()
        {
            if (prevMusicIndex == musicBeatIndex) return;

            // Sync to FMOD's authoritative timing
            percentToNextBeat = 0.0f;
            beatIndexHasChanged = false;
            prevMusicIndex = musicBeatIndex;

            // Trigger game events
            if (gm && !gm.BeatHit)
            {
                gm.TriggerBeat(musicBeatIndex);
            }

            // Play percussion on unlocked beats
            if (unlockedBeats.Contains(musicBeatIndex))
            {
                PlayBeatEndSound();
            }
        }

        // Syncs BPM when music changes (e.g. room transitions)
        void SyncBPMFromMusic()
        {
            if (musicManager == null) return;

            float currentMusicBPM = musicManager.GetCurrentBPM();
            if (currentMusicBPM > 0.0f && Mathf.Abs(lastCheckedBPM - currentMusicBPM) > 0.01f)
            {
                lastCheckedBPM = currentMusicBPM;
                ChangeBPM(currentMusicBPM);
            }
        }

        // Updates beatIndex at 50% through beat (for attack lock removal)
        void UpdateBeatIndexAtHalfBeat()
        {
            if (percentToNextBeat > 0.5f && !beatIndexHasChanged)
            {
                if (gm)
                {
                    gm.BeatHit = false;
                }
                int beatIndex = musicBeatIndex - 1;

                if (playerAttack != null && unlockedBeats.Contains(beatIndex))
                {
                    playerAttack.RemoveAttackLock(PlayerAttack.MISSED_ATTACK_LOCK_KEY);
                }

                beatIndexHasChanged = true;
            }
        }

        // Clamp timer if local accumulation outruns FMOD (prevents overshoot)
        void ClampTimerIfAhead()
        {
            if (percentToNextBeat > 1.0f)
            {
                percentToNextBeat = 1.0f;
            }
        }
    }

    public int GetMusicBeatIndex() => musicBeatIndex;
    public int GetPrevMusicIndex() => prevMusicIndex;
    public bool GetBeatIndexHasChanged() => beatIndexHasChanged;

    public float GetPercentToNextBeat()
    {
        return percentToNextBeat;
    }

    // get whether an attack can be made based on the percentage to next beat
    public bool CheckValidAttackInterval(float percentage)
    {
        bool inWindow = percentage <= REQUIRED_ACCURACY || percentage >= 1 - REQUIRED_ACCURACY;
        if (!inWindow) return false;

        // Early hit: check the upcoming beat's slot
        if (percentage >= 0.75f)
        {
            int nextBeat = (musicBeatIndex % BEAT_NUM) + 1;
            return unlockedBeats.Contains(nextBeat);
        }
        // Late hit: check the current beat's slot
        else
        {
            return unlockedBeats.Contains(musicBeatIndex);
        }
    }

    public bool CheckValidDashInterval(float percentage)
    {
        return percentage <= REQUIRED_ACCURACY || percentage >= 1 - REQUIRED_ACCURACY;
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
        // Early hit: return upcoming beat's slot
        if (percentToNextBeat >= 0.75f)
        {
            return (musicBeatIndex % BEAT_NUM) + 1;
        }
        // Late hit: return current beat's slot
        return musicBeatIndex;
    }

    public static void ClearUnlockedBeats()
    {
        unlockedBeats.Clear();
    }

    // method to reset the Beat bar to a new bpm
    public void ChangeBPM(float newBPM)
    {
        bpm = newBPM;

        // Reset beat ID if needed
        percentToNextBeat = 1.0f;
        beatIndexHasChanged = true;
    }

    private void InitializeRandomSpells()
    {
        // BEAT_NUM is limit of spell slots
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

    public void SetBPM(float newBPM)
    {
        bpm = newBPM;
    }
    private void UpdateBeatVisuals()
    {
        float beatSpacing = (beatStartSpawnDistance - beatEndSpawnDistance) / numBeatsShown;

        float growPerc = 0.8f + Mathf.Pow(1.0f - Mathf.Abs(percentToNextBeat - 0.5f), 2.0f) * 0.2f;
        spriteRenderer.transform.localScale = new Vector3(0.1f * growPerc, 0.1f * growPerc, spriteRenderer.transform.localScale.z);

        for (int i = 0; i < BEAT_NUM; i++)
        {
            // gets the index of the beat item from left to right based on the current beat index
            int adjustedBeatIndex = (((i - musicBeatIndex)) + BEAT_NUM) % BEAT_NUM;

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

    private void PlayBeatEndSound()
    {
        if (!beatEndEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(beatEndEvent);
        }
    }
}
