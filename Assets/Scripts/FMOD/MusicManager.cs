using UnityEngine;
using FMODUnity;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;



public class MusicManager : MonoBehaviour
{
    //name of parameter condition in FMOD (used for transition condition)
    private string parameterName = "roomChanging";

    //Three room options in the inspector for testing
    public enum RoomType
    {
        smallCombat,
        bigCombat,
        shopRoom
    }
    //For testing the change of music in the inspector
    [SerializeField]
    private RoomType currentRoomType;


    //Two method for to control the
    //public static event System.Action<GameObject>
    private void OnEnable()
    {
        // event used in RoomInfo to indecate the room changed
        RoomInfo.OnEnterRoom += HandleRoomEntered;


    }
    private void OnDisable()
    {
        // preventing memory leaks
        RoomInfo.OnEnterRoom -= HandleRoomEntered;
    }

    //method to get room information
    private void HandleRoomEntered(GameObject room)
    {
        RoomInfo roomInfo = room.GetComponent<RoomInfo>();
        if (roomInfo != null)
        {
            //change the music to reflect which room you are in
            ChangeMusicForRoom(roomInfo);

        }
    }



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



    // Convert using RoomTpyes to music related to the room 
    private void ChangeMusicForRoom(RoomInfo roomInfo)
    {
        RoomTypes roomType = roomInfo.GetRoomType();
        string musicLabel = "";

        // Map RoomTypes to your music labels
        switch (roomType)
        {
            case RoomTypes.Starter:
            case RoomTypes.Generic:
                musicLabel = "smallCombat";
                break;

            case RoomTypes.Combat:
                musicLabel = "bigCombat";
                break;

            case RoomTypes.Shop:
                musicLabel = "shopRoom";
                break;

            //not needed yet
            case RoomTypes.Reward:
                // musicLabel = "smallCombat"; 
                break;

            case RoomTypes.End:
                //    // You might want special boss music
                //     musicLabel = "bigCombat"; // or create a new "boss" type
                break;
        }

        // Change the label so if OnValidate() is called music will change
        if (!string.IsNullOrEmpty(musicLabel))
        {
            SetRoomMusicByLabel(musicLabel);
        }
    }



    // Called if the room is changed 
    private void OnValidate()
    {
#if UNITY_EDITOR
        // Only try to set if we're in Play Mode and instance is valid
        if (UnityEditor.EditorApplication.isPlaying && musicInstance.isValid())
        {
            SetRoomMusicByLabel(currentRoomType.ToString());
        }

        // Handle volume in Editor
        if (bus.isValid())
        {
            bus.setVolume(volumeControl);
        }
#endif
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
            // GUILayout.Box($"Current Beat = {timelineInfo.currentBeat}, Last Marker = {(string)timelineInfo.lastMarker}");
            GUILayout.Box($" Last Marker = {(string)timelineInfo.lastMarker}, Bpm = {(timelineInfo.currentBpm)}");
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



    // Communicate with FMOD to change the change the music based on the room
    public void SetParameterWithLabel(string parameterName, string labelName)
    {
        if (musicInstance.isValid())
        {
            // method in FMOD set parameters by label
            musicInstance.setParameterByNameWithLabel(parameterName, labelName);
            Debug.Log($"Set parameter '{parameterName}' to label '{labelName}'");
        }
        else
        {
            // Debug.LogWarning($"Cannot set parameter - musicInstance is not valid!");
        }
    }

    // change the label related to a conditional parameter 
    public void SetRoomMusicByLabel(string labelName)
    {
        if (musicInstance.isValid())
        {

            SetParameterWithLabel(parameterName, labelName);
            // Debug.Log($"Music changed to: {labelName}");
        }
        else
        {
            // Debug.LogWarning($"Cannot set music - musicInstance is not valid!");
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








//For Testing: ////////////////////////////////////////////////////////
// void Update()
// {
//     var keyboard = Keyboard.current;
//     if (keyboard == null) return;

//     // cycle between rooms by pressing (s)
//     if (keyboard.sKey.wasPressedThisFrame)
//     {
//         currentRoomType = GetNextRoomType(currentRoomType);
//         SetRoomMusicByLabel(currentRoomType.ToString());
//     }
// }

// // Helper for cycle 
// private RoomType GetNextRoomType(RoomType current)
// {
//     switch (current)
//     {
//         case RoomType.smallCombat:
//             return RoomType.bigCombat;
//         case RoomType.bigCombat:
//             return RoomType.shopRoom;
//         case RoomType.shopRoom:
//             return RoomType.smallCombat;
//         default:
//             return RoomType.smallCombat;
//     }
// }


