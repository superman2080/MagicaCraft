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
            caster.Rb2d.linearVelocity = Vector2.zero;
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

        protected override void Start()
        {
            base.Start();
            OnTakeDamage += (s, c, amount, type) => { 
                UIManager.Instance.Get<SpellEditorUI>().Log($"<color=#98C379>피해 받음 / 피해량:{amount}, 타입{type}</color>");
            };
        }
    }
}