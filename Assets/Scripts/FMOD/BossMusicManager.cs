using UnityEngine;
using FMODUnity;
using System;
using System.Runtime.InteropServices;


//similar to MusicManager but needed for the boss
public class BossMusicManager : MonoBehaviour
{

    //Captured music event in FMOD
    [Header("FMOD Settings")]
    [SerializeField]
    private EventReference bossMusicEvent;
    
    //name of parameter condition in FMOD 
    [SerializeField]
    private string phaseParameterName = "BossPhases";  // Your parameter name in FMOD

    //Volum Control 
    [Header("Volume Control")]
    [SerializeField, Range(0f, 1f)]
    private float volumeControl = 0.7f;
    
    //
    [Header("Debug")]
    [SerializeField]
    private BossPhase currentPhase = BossPhase.First;
    [SerializeField]
    private bool musicPlaying = false;
    
    //so other scripts can access the info here with
    public static BossMusicManager instance;
    
    
    //Get the music refrenece from the bank in FMOD
    private FMOD.Studio.EventInstance musicInstance;

    //used to get the bus for the volume Control
    private FMOD.Studio.Bus bus;
    
    // Timeline info for beat/marker data from FMODE 
    public TimelineInfo timelineInfo = null;
    private GCHandle timelineHandle;
    private FMOD.Studio.EVENT_CALLBACK beatCallback;
    
    // For phase tracking
    private BossPhase lastReportedPhase = BossPhase.First;
    private DeerBoss currentBoss;
    
    [StructLayout(LayoutKind.Sequential)]
    public class TimelineInfo
    {
        public int currentBeat = 0;
        public float currentBpm = 0f;
        public FMOD.StringWrapper lastMarker = new FMOD.StringWrapper();
    }
    
private void Awake()
{
    if (instance == null)
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
        timelineInfo = new TimelineInfo(); // create timeline immediately
        
        InitializeMusic(); // initialize the Music
    }
    else
    {
        Destroy(gameObject);
        return;
    }
}

    private void InitializeMusic()
    {
        //is the event available in fmod 
        if (!bossMusicEvent.IsNull)
        {
            musicInstance = RuntimeManager.CreateInstance(bossMusicEvent);
            
            // Setup beat/marker callbacks
            beatCallback = new FMOD.Studio.EVENT_CALLBACK(BeatEventCallback);
            timelineHandle = GCHandle.Alloc(timelineInfo, GCHandleType.Pinned);
            musicInstance.setCallback(beatCallback,
                FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_BEAT |
                FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
            
            musicInstance.setUserData(GCHandle.ToIntPtr(timelineHandle));
            
            // Start with phase 1
            SetPhase(BossPhase.First);
            
            musicInstance.start();
            musicPlaying = true;
            Debug.Log("[BossMusicManager] Boss music initialized - Starting PhaseOne");
        }
        else
        {
            Debug.LogWarning("[BossMusicManager] No boss music event assigned!");
        }
    }
    
    private void Start()
    {
        // Get master bus for volume control
        bus = RuntimeManager.GetBus("bus:/");
        if (bus.isValid())
        {
            bus.setVolume(volumeControl);
        }
        
        // Find the deer boss and subscribe to its phase changes
        FindAndSubscribeToBoss();
    }
    
    private void FindAndSubscribeToBoss()
    {
        //FindFirstObjectByType (returns the first active object)
        DeerBoss boss = FindFirstObjectByType<DeerBoss>();
        
        if (boss != null)
        {
            currentBoss = boss;
            
            // Subscribe to boss phase changes
            //From DeerBoss.cs
            boss.OnPhaseChanged += HandleBossPhaseChanged;
            
            // Get initial phase
            var phaseField = typeof(DeerBoss).GetField("currentPhase", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (phaseField != null)
            {
                currentPhase = (BossPhase)phaseField.GetValue(boss);
                SetPhase(currentPhase);
            }
        }
        else
        {
            //keep trying to find the boss
            Debug.LogWarning("[BossMusicManager] DeerBoss not found in scene, will retry");
            Invoke(nameof(FindAndSubscribeToBoss), 1.0f);
        }
    }
    

    //helper to handle the phase changes
    private void HandleBossPhaseChanged(BossPhase newPhase)
    {
        currentPhase = newPhase;
        SetPhase(newPhase);
    }
    
    public void SetPhase(BossPhase phase)
    {
        string label = GetPhaseLabel(phase);
        SetParameterWithLabel(phaseParameterName, label);
        
        string phaseName = phase.ToString();
        Debug.Log($"[BossMusicManager] Phase transition: {phaseName} → Setting FMOD parameter '{phaseParameterName}' to label '{label}'");
    }
    

    //hardcoded values (strings) in FMOD to switch music depending on the phase
    //FMOD parameter that switch the music based on the phase 
    private string GetPhaseLabel(BossPhase phase)
    {
        switch (phase)
        {
            case BossPhase.First:
                return "PhaseOne";  // Returns "PhaseOne"
            case BossPhase.Second:
                return "PhaseTwo";  // Returns "PhaseTwo"
            case BossPhase.Third:
                return "PhaseThree";  // Returns "PhaseThree"
            default:
                return "PhaseOne";
        }
    }
    
    // Manual method: phase change testing!
    public void ForcePhaseChange(int phaseNumber)
    {
        switch (phaseNumber)
        {
            case 1:
                SetPhase(BossPhase.First);
                break;
            case 2:
                SetPhase(BossPhase.Second);
                break;
            case 3:
                SetPhase(BossPhase.Third);
                break;
            default:
                Debug.LogWarning($"[BossMusicManager] Invalid phase number: {phaseNumber}");
                break;
        }
    }

    //similar to MusicManager 
    //needed fucntions:
    public void SetParameter(string parameterName, float value)
    {
        if (musicInstance.isValid())
        {
            musicInstance.setParameterByName(parameterName, value);
        }
    }
    
    public void SetParameterWithLabel(string parameterName, string labelName)
    {
        if (musicInstance.isValid())
        {
            musicInstance.setParameterByNameWithLabel(parameterName, labelName);
            Debug.Log($"[BossMusicManager] FMOD Set Parameter: {parameterName} = {labelName}");
        }
        else
        {
            Debug.LogWarning($"[BossMusicManager] Cannot set parameter - musicInstance is not valid!");
        }
    }
    
    public float GetCurrentBPM()
    {
        if (timelineInfo != null)
            return timelineInfo.currentBpm;
        return 120f;
    }
    
    public int GetCurrentBeat()
    {
        if (timelineInfo != null)
            return timelineInfo.currentBeat;
        return 0;
    }
    
    public void StopMusic()
    {
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicPlaying = false;
            Debug.Log("[BossMusicManager] Boss music stopped");
        }
    }
    
    public void ResumeMusic()
    {
        if (musicInstance.isValid() && !musicPlaying)
        {
            musicInstance.start();
            musicPlaying = true;
            Debug.Log("[BossMusicManager] Boss music resumed");
        }
    }
    
    public void SetVolume(float volume)
    {
        volumeControl = volume;
        if (bus.isValid())
        {
            bus.setVolume(volumeControl);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from boss events
        if (currentBoss != null)
        {
            currentBoss.OnPhaseChanged -= HandleBossPhaseChanged;
        }
        
        if (musicInstance.isValid())
        {
            musicInstance.setUserData(IntPtr.Zero);
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicInstance.release();
        }
        
        if (timelineHandle.IsAllocated)
        {
            timelineHandle.Free();
        }
    }


    //C# <-> C++ communication because FMOD use C# 
    //Needed to update beat/marker info
    
    [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
    static FMOD.RESULT BeatEventCallback(FMOD.Studio.EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        FMOD.Studio.EventInstance instance = new FMOD.Studio.EventInstance(instancePtr);
        
        IntPtr timelineInfoPtr;
        FMOD.RESULT result = instance.getUserData(out timelineInfoPtr);
        
        if (result != FMOD.RESULT.OK)
        {
            return result;
        }
        else if (timelineInfoPtr != IntPtr.Zero)
        {
            GCHandle timelineHandle = GCHandle.FromIntPtr(timelineInfoPtr);
            TimelineInfo timelineInfo = (TimelineInfo)timelineHandle.Target;
            
            switch (type)
            {
                case FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_BEAT:
                    {
                        var parameter = (FMOD.Studio.TIMELINE_BEAT_PROPERTIES)Marshal.PtrToStructure(
                            parameterPtr, typeof(FMOD.Studio.TIMELINE_BEAT_PROPERTIES));
                        timelineInfo.currentBeat = parameter.beat;
                        timelineInfo.currentBpm = parameter.tempo;
                    }
                    break;
                case FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER:
                    {
                        var parameter = (FMOD.Studio.TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(
                            parameterPtr, typeof(FMOD.Studio.TIMELINE_MARKER_PROPERTIES));
                        timelineInfo.lastMarker = parameter.name;
                    }
                    break;
            }
        }
        return FMOD.RESULT.OK;
    }
    

    //Gui Debuging 
    #if UNITY_EDITOR
    void OnGUI()
    {
        if (timelineInfo != null && musicPlaying)
        {
            GUILayout.BeginArea(new Rect(10, 100, 300, 100));
            GUILayout.Box($"Boss Music Status:\nPhase: {currentPhase}\nBeat: {timelineInfo.currentBeat}\nBPM: {timelineInfo.currentBpm:F0}");
            GUILayout.EndArea();
        }
    }
    #endif
}