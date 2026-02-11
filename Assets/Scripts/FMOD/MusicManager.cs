using UnityEngine;
using FMODUnity;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

public class MusicManager : MonoBehaviour
{

    //so other scripts can access the info here with
    //MusicManager.instance 
    public static MusicManager instance;

    //Get the music refrenece from the bank in FMOD
    [SerializeField]
    private EventReference music;

    //in the inspector to control music volume
    [SerializeField, Range(0f, 1f)]
    private float volumeControl = 0.7f;
    //used to get the bus for the volume Control
    private FMOD.Studio.Bus bus;

    //Story both beat/marker data from FMODE to use later
    public TimelineInfo timelineInfo = null;
    private GCHandle timelineHandle;

    private FMOD.Studio.EventInstance musicInstance;

    //FMOD calls when beats/markers happened
    private FMOD.Studio.EVENT_CALLBACK beatCallback;

    [StructLayout(LayoutKind.Sequential)]
    public class TimelineInfo
    {
        public int currentBeat = 0;// accorrding to the music info
        public float currentBpm = 0f;  // Add this line to get bpm from music
        //labels places in FMOD timeline
        public FMOD.StringWrapper lastMarker = new FMOD.StringWrapper();
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize timelineInfo early
            timelineInfo = new TimelineInfo();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // create sound instacne from FMOD event
        if (!music.IsNull)
        {
            musicInstance = RuntimeManager.CreateInstance(music);
            
            // Set up callback BEFORE starting
            beatCallback = new FMOD.Studio.EVENT_CALLBACK(BeatEventCallback);
            timelineHandle = GCHandle.Alloc(timelineInfo, GCHandleType.Pinned);
            musicInstance.setCallback(beatCallback, 
                FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_BEAT | 
                FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
            
            // Set user data for the callback
            musicInstance.setUserData(GCHandle.ToIntPtr(timelineHandle));
            
            musicInstance.start();
        }
        else
        {
            Debug.LogWarning("No music event assigned to MusicManager!");
        }
    }


    //Getting bus info and setting volume
    private void Start()
    {
        //get the bus info from FMOD bank
        bus = RuntimeManager.GetBus("bus:/");

        if (bus.isValid())
        {   
            //set the volume according to the inspector value
            bus.setVolume(volumeControl);
        }

    }

    // Called if the value in the inspector is updated
    private void OnValidate()
    {
        // if the bus is valid and the game is running 
        if (Application.isPlaying && bus.isValid())
        {
            bus.setVolume(volumeControl); //change the volume accordingly       }
        }   
    }


    //clear up and releases memory
    private void OnDestroy()
    {
        // Check musicInstance is valid
        if (musicInstance.isValid())
        {
            musicInstance.setUserData(IntPtr.Zero);
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicInstance.release();
        }
        
        // Check timelineHandle is allocated
        if (timelineHandle.IsAllocated)
        {
            timelineHandle.Free(); 
        }
    }


    //Display beat and marker name (chords for the music)
#if UNITY_EDITOR // Development_build
    void OnGUI() 
    {
        // FIX: Add null check for timelineInfo
        if (timelineInfo != null)
        {
            GUILayout.Box($"Current Beat = {timelineInfo.currentBeat}, Last Marker = {(string)timelineInfo.lastMarker}");
        }
        else
        {
            GUILayout.Box("TimelineInfo is null - No music playing");
        }
    }
#endif 



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
            Debug.LogError("Timeline callback error! " + result);
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
                        

                        //capturing the bpm to send it to BeatHandler for later
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


    //SetParameter to control parameters by name
    public void SetParameter(string parameterName, float value)
    {
        if (musicInstance.isValid())
        {
            musicInstance.setParameterByName(parameterName, value);
        }
    }

    //later to get the currsent bpm from the music and make it dynamice
    public float GetCurrentBPM()
    {
        if (timelineInfo != null)
            return timelineInfo.currentBpm;
        return 120f; // Default fallback
    }
}