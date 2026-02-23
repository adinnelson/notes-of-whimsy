using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    private [SerializeField] BeatHandler beatHandler;

    private List<SimpleTimer> timers = new List<SimpleTimer>();

    // enabled when a beat is within acceptable range
    public bool BeatHit = false;

    public Action OnBeatTriggered = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void FixedUpdate()
    {

        for(int i = 0;i < timers.Count;i++)
        {
            timers[i].UpdateTimer(Time.fixedDeltaTime);
        }        
    }

    public void TriggerBeat()
    {
        BeatHit = true;

        if(OnBeatTriggered == null) return;

        OnBeatTriggered.Invoke();
    }

    public void AddTimer(SimpleTimer timer)
    {
        timers.Add(timer);
    }
}
