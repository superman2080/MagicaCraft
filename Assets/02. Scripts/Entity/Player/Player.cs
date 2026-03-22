using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public enum PlayerState
    {
        Idle,
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

        }

        protected override void InitializeStateMachine()
        {
            base.InitializeStateMachine();

            stateMachine.RegisterState(PlayerState.Idle, new PlayerIdle(this));
            stateMachine.RegisterState(PlayerState.Move, new PlayerCompositeMove(this));

            stateMachine.RegisterCondition(PlayerState.Idle, PlayerState.Move, () => InputHandler.MoveInput != Vector2.zero);
            stateMachine.RegisterCondition(PlayerState.Move, PlayerState.Idle, () => InputHandler.MoveInput == Vector2.zero);


        }
    }

}