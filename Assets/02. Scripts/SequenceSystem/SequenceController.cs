using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SequenceController : MonoBehaviour
{
    public List<SequenceStateBase> queueStates;
    private Queue<SequenceStateBase> actionQueue = new Queue<SequenceStateBase>();
    public SequenceStateBase curTutorialState { get; private set; }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var item in queueStates)
        {
            actionQueue.Enqueue(item);
        }
        curTutorialState = actionQueue.Dequeue();
        curTutorialState.Enter();
    }

    // Update is called once per frame
    void Update()
    {
        curTutorialState?.Execute();
    }

    void FixedUpdate()
    {
        curTutorialState?.FixedExecute();
    }

    public void SetNextTutorial()
    {
        curTutorialState?.Exit();
        if (actionQueue.TryDequeue(out SequenceStateBase state))
        {
            curTutorialState = state;
            curTutorialState.Enter();
        }
        else
            curTutorialState = null;
    }
}
