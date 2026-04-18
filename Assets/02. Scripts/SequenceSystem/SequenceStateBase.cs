using UnityEngine;

public abstract class SequenceStateBase : MonoBehaviour, IState
{
    private SequenceController controller;

    public SequenceStateBase(SequenceController controller)
    {
        this.controller = controller;
    }

    public virtual void Enter() { }
    public virtual void Execute() { }
    public virtual void FixedExecute() { }
    public virtual void Exit() { }

    public abstract bool FinishSequenceCondition();
}
