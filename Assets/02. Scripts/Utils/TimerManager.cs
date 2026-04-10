using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class Timer
{
    public Action OnTimerExecute;
    public Action OnTimerExit;
    public bool IsRunning => isRunning;
    private bool isRunning;
    public float Duration => duration;
    private float duration;

    public Timer(float duration, Action onTimerExit, Action onTimerEnter = null, Action onTimerExecute = null)
    {
        SetTimer(duration, onTimerExit, onTimerEnter, onTimerExecute);
    }

    public void Execute()
    {
        duration -= Time.deltaTime;
        if(duration > 0)
        {
            OnTimerExecute?.Invoke();
        }
        else
        {
            OnTimerExit?.Invoke();
            Reset();
        }
    }

    public void SetTimer(float duration, Action onTimerExit, Action onTimerEnter = null, Action onTimerExecute = null)
    {
        isRunning = true;

        this.duration = duration;
        onTimerEnter?.Invoke();
        OnTimerExecute = onTimerExecute;
        OnTimerExit = onTimerExit;
    }

    private void Reset()
    {
        isRunning = false;
        duration = 0;
        OnTimerExecute = null;
        OnTimerExit = null;
    }
}

public class TimerManager: Singleton<TimerManager>
{
    private List<Timer> timerList = new();
    private bool isDirty;

    public Timer SetTimer(float duration, Action onTimerExit, Action onTimerEnter = null, Action onTimerExecute = null)
    {
        Timer timer = null;
        if(TryFindInactiveTimer(out Timer remained))
        {
            remained.SetTimer(duration, onTimerExit, onTimerEnter, onTimerExecute);
            timer = remained;
        }
        else
        {
            timer = new Timer(duration, onTimerExit, onTimerEnter, onTimerExecute);
        }

        timer.OnTimerExit += () => timerList.Remove(timer);
        timerList.Add(timer);
        isDirty = true;
        return timer;
    }

    private bool TryFindInactiveTimer(out Timer timer)
    {
        timer = null;
        var target = timerList.FirstOrDefault(t => t.IsRunning == false);
        if(target != null)
        {
            timer = target;
            return true;
        }
        else return false;
    }

    private void Update()
    {
        if (isDirty)
            timerList.OrderBy((t) => t.Duration);

        foreach (var timer in timerList) 
        {
            timer.Execute();
        }
    }
}
