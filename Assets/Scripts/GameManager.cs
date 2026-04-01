using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public const float Z_RANGE = 5.0f;

    [SerializeField] private BeatHandler beatHandler;

    private List<SimpleTimer> timers = new List<SimpleTimer>();

    // enabled when a beat is within acceptable range
    public bool BeatHit = false;

    public Action OnBeatTriggered = null;

    // important to call separate for enemies who only react to odd beats
    public Action OnOddBeatTriggered = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    void FixedUpdate()
    {
        // Since the gamemanager is apart of the DontDestroyOnLoad, the reference to the beat handler gets lost when a new scene is loaded. 
        if(beatHandler == null)
        {
            beatHandler = GameObject.Find("BeatBar")?.GetComponent<BeatHandler>();
        }

        for(int i = 0;i < timers.Count;i++)
        {
            // Debug.Log("Timer Updated");
            timers[i].UpdateTimer(Time.fixedDeltaTime);
        }
    }

    public void TriggerBeat(int beatId)
    {
        BeatHit = true;

        OnBeatTriggered?.Invoke();

        if (beatId % 2 == 0)
        {
            return;
        }

        OnOddBeatTriggered?.Invoke();
    }

    public void AddTimer(SimpleTimer timer)
    {
        timers.Add(timer);
    }

    public void RemoveTimer(SimpleTimer timer)
    {
        timers.Remove(timer);
    }

    public float GetBPM()
    {
        return beatHandler.GetBPM();
    }
}
