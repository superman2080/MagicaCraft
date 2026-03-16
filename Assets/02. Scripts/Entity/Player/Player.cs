using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public enum PlayerIdleState
    {
        Idle,
    }

    public enum PlayerMoveState
    {
        Move,
        Dash,
    }
    public class PlayerIdle : StateBase<Entity>
    {
        public PlayerIdle(Player caster) : base(caster)
        {
        }
    }

    public class PlayerCompositeMove : CompositeStateBase<Entity>
    {
        public class PlayerMove : StateBase<Player>
        {
            public PlayerMove(Player caster) : base(caster)
            {
            }

            public override void Execute(Player caster)
            {
                base.Execute(caster);

            }
        }



        public PlayerCompositeMove(Player caster) : base(caster)
        {
        }


    }

    public class Player : Entity
    {
        public override StateBase<Entity> EntryState => throw new System.NotImplementedException();

        public PlayerInputHandler InputHandler { get; private set; }

        private void Awake()
        {
            InputHandler = GetComponent<PlayerInputHandler>();
        }

        protected override void Start()
        {
            base.Start();
            // 초기 상태 설정

            stateMachine.RegisterState(PlayerIdleState.Idle, new PlayerIdle(this));
        }

    }

}