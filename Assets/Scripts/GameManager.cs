using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField] private BeatHandler beatHandler;

    private List<SimpleTimer> timers = new List<SimpleTimer>();

    // enabled when a beat is within acceptable range
    public bool TickHit = false;

    public Action OnTickTriggered = null;

    // important to call separate for enemies who only react to odd ticks
    public Action OnOddTickTriggered = null;

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

    public void TriggerTick(int tickId)
    {
        TickHit = true;

        if(OnTickTriggered == null) return;

        OnTickTriggered.Invoke();

        if(tickId % 2 == 0) return;
    }

    public void AddTimer(SimpleTimer timer)
    {
        timers.Add(timer);
    }
}
