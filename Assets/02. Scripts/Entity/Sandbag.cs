using System;
using UnityEngine;


namespace Sandbag
{
    public enum SandbagState
    {
        Idle,
    }

    public class SandbagIdle : StateBase<Entity>
    {
        private Sandbag sandbag;
        private Vector2 originalPosition;

        public SandbagIdle(Sandbag caster) : base(caster)
        {
            sandbag = caster;
        }

        public override void Enter()
        {
            base.Enter();
            originalPosition = sandbag.transform.position;
        }

        public override void FixedExecute()
        {
            base.FixedExecute();
            Debug.Log("Sandbag is idle at position: " + sandbag.transform.position);
            caster.Rb2d.position = originalPosition;
        }
    }


    public class Sandbag : Entity
    {   
        public override Enum EntryStateKey => entryStateKey;
        [SerializeField] private SandbagState entryStateKey;

        protected override void InitializeStateMachine()
        {
            base.InitializeStateMachine();
            stateMachine.RegisterState(SandbagState.Idle, new SandbagIdle(this));
        }
    }
}