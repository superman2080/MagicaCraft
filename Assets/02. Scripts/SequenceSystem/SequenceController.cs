using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class SequenceController : MonoBehaviour
{
    public List<SequenceStateBase> queueStates;
    private Queue<SequenceStateBase> actionQueue = new Queue<SequenceStateBase>();
    public SequenceStateBase CurTutorialState { get; private set; }
    public Action OnFinishSequence;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var item in queueStates)
        {
            actionQueue.Enqueue(item);
        }
        CurTutorialState = actionQueue.Dequeue();
        CurTutorialState.Enter();
    }

    // Update is called once per frame
    void Update()
    {
        CurTutorialState?.Execute();
    }

    void FixedUpdate()
    {
        CurTutorialState?.FixedExecute();
    }

    private void LateUpdate()
    {
        CheckNextSequence();
    }

    private void CheckNextSequence()
    {
        if (CurTutorialState == null)
            return;
        if (CurTutorialState.FinishSequenceCondition())
            SetNextSequence();
    }

    public void SetNextSequence()
    {
        CurTutorialState?.Exit();
        if (actionQueue.TryDequeue(out SequenceStateBase state))
        {
            CurTutorialState = state;
            CurTutorialState?.Enter();
        }
        else
        {
            CurTutorialState = null;
            OnFinishSequence?.Invoke();
        }
    }
}
