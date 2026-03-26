using System;
using System.Collections;
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

    public enum PlayerMoveState
    {
        Walk,
        Dash
    }

    public class PlayerIdle : StateBase
    {
        public PlayerIdle(Player caster) : base(caster)
        {
        }
    }

    public class PlayerCompositeMove : CompositeStateBase
    {
        private Player player;
        private PlayerDash dashState;

        public PlayerCompositeMove(Player caster) : base(caster)
        {
            player = caster;
            SubStateMachine.RegisterState(PlayerMoveState.Walk, new PlayerWalk(caster));
            SubStateMachine.RegisterState(PlayerMoveState.Dash, new PlayerDash(caster));
            dashState = SubStateMachine.GetState(PlayerMoveState.Dash) as PlayerDash;

            SubStateMachine.RegisterAnyStateTransition(PlayerMoveState.Dash, () => player.InputHandler.DashPressed && dashState.dashCor == null);
        }

        public class PlayerWalk : StateBase
        {
            private Player player;
            public PlayerWalk(Player caster) : base(caster)
            {
                player = caster;
            }

            public override void FixedExecute()
            {
                base.FixedExecute();
                caster.Rb2d.MovePosition(player.InputHandler.MoveInput * caster.DefaultStat.Get(DefaultStatType.MOVE_SPEED));
            }
        }

        [Serializable]
        public class DashConfig
        {
            public float dashSpeed;
        }

        public class PlayerDash: StateBase
        {
            public Coroutine dashCor;
            private Player player;

            public PlayerDash(Player caster) : base(caster)
            {
                player = caster;
            }

            public override void Enter()
            {
                base.Enter();
                dashCor = player.StartCoroutine(DashCoroutine());
            }

            private IEnumerator DashCoroutine()
            {
                yield break;
            }
        }

    }

    public class Player : Entity
    {
        [HideInInspector] public Animator animator;
        public override StateBase EntryState => throw new System.NotImplementedException();

        public PlayerInputHandler InputHandler { get; private set; }

        protected override void Start()
        {
            InputHandler = GetComponent<PlayerInputHandler>();
            animator = gameObject.GetComponent<Animator>();
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