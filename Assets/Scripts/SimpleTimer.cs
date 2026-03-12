using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class SimpleTimer
{
    private float duration = 60.0f;
    private float timeRemaining;
    private bool on = false;
    private bool loop = false;

    private GameManager gameManager;

    Action onFinish;

    public void UpdateTimer(float time)
    {
        if (!on) return; 
        
        timeRemaining -= time;

        if (timeRemaining <= 0)
        {
            if (onFinish != null) 
            {
                onFinish.Invoke();
            }

            if(loop)
            {
                timeRemaining = duration;
                return;
            }

            on = false;
            gameManager.RemoveTimer(this);
        }
    }

    public void StartTimer(float duration, bool loop = false, Action onFinish = null, GameManager gameManager = null)
    {
        this.duration = duration;
        timeRemaining = this.duration;

        this.loop = loop;
        this.onFinish = onFinish;

        on = true;

        if (gameManager == null)
        {
            this.gameManager = GameObject.FindWithTag("GameManager")?.GetComponent<GameManager>();
        }
        else
        {
            this.gameManager = gameManager;
        }

        this.gameManager?.AddTimer(this);
    }

    public void Stop()
    {
        on = false;
    }

    public void Resume()
    {
        on = true;
    }

    public void Restart()
    {
        timeRemaining = duration;
        on = true;
    }
}